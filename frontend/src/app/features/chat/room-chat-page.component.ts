import { AsyncPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, forkJoin, switchMap } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { ChatRealtimeService } from '../../core/chat/chat-realtime.service';
import {
  messageFromRealtimeEvent,
  upsertAndSortMessages,
} from '../../core/chat/message-deduplication';
import { MessageDto } from '../../core/models/chat.models';
import { RoomDetails, RoomMember } from '../../core/models/room.models';
import { ErrorNotificationService } from '../../core/notifications/error-notification.service';
import { RoomsApiService } from '../../core/rooms/rooms-api.service';

@Component({
  selector: 'app-room-chat-page',
  imports: [AsyncPipe, DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './room-chat-page.component.html',
  styleUrl: './room-chat-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomChatPageComponent implements OnInit, OnDestroy {
  @ViewChild('messagesScroll')
  private messagesScroll?: ElementRef<HTMLDivElement>;

  private readonly authService = inject(AuthService);
  private readonly chatRealtime = inject(ChatRealtimeService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly roomsApi = inject(RoomsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly errorNotifications = inject(ErrorNotificationService);

  protected readonly room = signal<RoomDetails | null>(null);
  protected readonly members = signal<readonly RoomMember[]>([]);
  protected readonly messages = signal<readonly MessageDto[]>([]);
  protected readonly nextCursor = signal<string | null>(null);
  protected readonly hasMore = signal(false);
  protected readonly loading = signal(false);
  protected readonly loadingOlder = signal(false);
  protected readonly sending = signal(false);
  protected readonly membershipChanging = signal(false);
  protected readonly currentUser = this.authService.currentUserSnapshot;
  protected readonly connectionState$ = this.chatRealtime.connectionState$;

  protected readonly messageForm = this.formBuilder.group({
    content: ['', [Validators.required, Validators.maxLength(2000)]],
  });

  private roomId = '';

  ngOnInit(): void {
    this.roomId = this.route.snapshot.paramMap.get('roomId') ?? '';
    this.registerRealtimeHandlers();
    this.loadRoom();
  }

  ngOnDestroy(): void {
    const roomId = this.roomId;
    if (roomId) {
      void this.chatRealtime.leaveRoom(roomId);
    }
  }

  protected loadRoom(): void {
    if (!this.roomId) {
      this.errorNotifications.showMessage('Invalid room identifier.');

      return;
    }

    this.loading.set(true);

    this.roomsApi
      .getRoom(this.roomId)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (room) => {
          this.room.set(room);
          if (room.isMember) {
            this.loadMemberData();
            void this.joinRealtimeRoom();
          }
        },
        error: (error: unknown) =>
          this.errorNotifications.show(error, 'Could not load the room.'),
      });
  }

  protected joinRoom(): void {
    this.membershipChanging.set(true);

    this.roomsApi
      .joinRoom(this.roomId)
      .pipe(
        switchMap(() => this.roomsApi.getRoom(this.roomId)),
        finalize(() => this.membershipChanging.set(false)),
      )
      .subscribe({
        next: (room) => {
          this.room.set(room);
          this.loadMemberData();
          void this.joinRealtimeRoom();
        },
        error: (error: unknown) =>
          this.errorNotifications.show(error, 'Could not join the room.'),
      });
  }

  protected leaveRoom(): void {
    this.membershipChanging.set(true);

    void this.chatRealtime
      .leaveRoom(this.roomId)
      .catch(() => undefined)
      .then(() => {
        this.roomsApi
          .leaveRoom(this.roomId)
          .pipe(finalize(() => this.membershipChanging.set(false)))
          .subscribe({
            next: () => void this.router.navigate(['/rooms']),
            error: (error: unknown) =>
              this.errorNotifications.show(error, 'Could not leave the room.'),
          });
      });
  }

  protected loadOlderMessages(): void {
    const before = this.nextCursor();
    if (!before || this.loadingOlder()) {
      return;
    }

    const scrollElement = this.messagesScroll?.nativeElement;
    const previousScrollHeight = scrollElement?.scrollHeight ?? 0;
    const previousScrollTop = scrollElement?.scrollTop ?? 0;

    this.loadingOlder.set(true);

    this.roomsApi
      .getMessages(this.roomId, { before, limit: 50 })
      .pipe(finalize(() => this.loadingOlder.set(false)))
      .subscribe({
        next: (page) => {
          this.messages.update((messages) => upsertAndSortMessages(messages, page.items));
          this.nextCursor.set(page.nextCursor);
          this.hasMore.set(page.hasMore);
          this.afterMessagesRender(() => {
            if (scrollElement) {
              scrollElement.scrollTop =
                previousScrollTop + scrollElement.scrollHeight - previousScrollHeight;
            }
          });
        },
        error: (error: unknown) =>
          this.errorNotifications.show(error, 'Could not load older messages.'),
      });
  }

  protected sendMessage(): void {
    if (this.messageForm.invalid) {
      this.messageForm.markAllAsTouched();

      return;
    }

    const content = this.messageForm.getRawValue().content.trim();
    if (!content) {
      this.messageForm.controls.content.setErrors({ required: true });

      return;
    }

    this.sending.set(true);

    void this.chatRealtime
      .sendMessage({ roomId: this.roomId, content })
      .then((message) => {
        this.messages.update((messages) => upsertAndSortMessages(messages, message));
        this.messageForm.reset();
        this.scrollMessagesToBottom();
      })
      .catch((error: unknown) =>
        this.errorNotifications.show(error, 'Could not send the message.'),
      )
      .finally(() => this.sending.set(false));
  }

  protected handleComposerKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Enter' || event.shiftKey || event.isComposing) {
      return;
    }

    event.preventDefault();
    this.sendMessage();
  }

  protected messageError(): string {
    const control = this.messageForm.controls.content;
    if (control.hasError('required')) {
      return 'Message cannot be empty.';
    }

    return control.hasError('maxlength') ? 'Message can be at most 2000 characters.' : '';
  }

  protected isMine(message: MessageDto): boolean {
    return message.senderUserId === this.currentUser?.id;
  }

  protected isCreatedByCurrentUser(room: RoomDetails): boolean {
    return room.createdByUserId === this.currentUser?.id;
  }

  protected userInitial(username: string): string {
    return username.trim().charAt(0).toUpperCase() || '?';
  }

  private loadMemberData(): void {
    this.loading.set(true);
    forkJoin({
      members: this.roomsApi.getMembers(this.roomId),
      messages: this.roomsApi.getMessages(this.roomId, { limit: 50 }),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ members, messages }) => {
          this.members.set(members);
          this.messages.set(upsertAndSortMessages([], messages.items));
          this.nextCursor.set(messages.nextCursor);
          this.hasMore.set(messages.hasMore);
          this.scrollMessagesToBottom();
        },
        error: (error: unknown) =>
          this.errorNotifications.show(error, 'Could not load room data.'),
      });
  }

  private async joinRealtimeRoom(): Promise<void> {
    try {
      await this.chatRealtime.joinRoom(this.roomId);
    } catch (error) {
      this.errorNotifications.show(error, 'Could not connect to realtime chat.');
    }
  }

  private registerRealtimeHandlers(): void {
    this.chatRealtime.messageReceived$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event) => {
        if (event.roomId === this.roomId) {
          this.messages.update((messages) =>
            upsertAndSortMessages(messages, messageFromRealtimeEvent(event)),
          );
          this.scrollMessagesToBottom();
        }
      });

    this.chatRealtime.userJoinedRoom$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event) => {
        if (event.roomId === this.roomId) {
          this.reloadMembers();
        }
      });

    this.chatRealtime.userLeftRoom$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event) => {
      if (event.roomId === this.roomId) {
        this.reloadMembers();
      }
    });

    this.chatRealtime.reconnected$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.synchronizeAfterReconnect());
  }

  private reloadMembers(): void {
    this.roomsApi
      .getMembers(this.roomId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (members) => this.members.set(members),
        error: (error: unknown) =>
          this.errorNotifications.show(error, 'Could not refresh room members.'),
      });
  }

  private synchronizeAfterReconnect(): void {
    this.roomsApi
      .getMessages(this.roomId, { limit: 50 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) => {
          const hasExistingHistory = this.messages().length > 0;
          this.messages.update((messages) => upsertAndSortMessages(messages, page.items));

          if (!hasExistingHistory) {
            this.nextCursor.set(page.nextCursor);
            this.hasMore.set(page.hasMore);
          }
          this.scrollMessagesToBottom();
        },
        error: (error: unknown) =>
          this.errorNotifications.show(error, 'Could not synchronize room messages.'),
      });

    this.reloadMembers();
  }

  private scrollMessagesToBottom(): void {
    this.afterMessagesRender(() => {
      const scrollElement = this.messagesScroll?.nativeElement;
      if (scrollElement) {
        scrollElement.scrollTop = scrollElement.scrollHeight;
      }
    });
  }

  private afterMessagesRender(action: () => void): void {
    requestAnimationFrame(() => requestAnimationFrame(action));
  }
}
