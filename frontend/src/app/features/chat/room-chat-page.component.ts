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
  protected readonly settingsSaving = signal(false);
  protected readonly memberRemovingId = signal<string | null>(null);
  protected readonly inviteLink = signal<string | null>(null);
  protected readonly inviteGenerating = signal(false);
  protected readonly currentUser = this.authService.currentUserSnapshot;
  protected readonly connectionState$ = this.chatRealtime.connectionState$;

  protected readonly messageForm = this.formBuilder.group({
    content: ['', [Validators.required, Validators.maxLength(2000)]],
  });

  protected readonly joinForm = this.formBuilder.group({
    password: ['', [Validators.maxLength(100)]],
  });

  protected readonly roomSettingsForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
  });

  protected readonly passwordSettingsForm = this.formBuilder.group({
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
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
          this.roomSettingsForm.controls.name.setValue(room.name);
          if (room.isMember) {
            this.loadMemberData();
            void this.joinRealtimeRoom();
          }
        },
        error: (error: unknown) => this.errorNotifications.show(error, 'Could not load the room.'),
      });
  }

  protected joinRoom(): void {
    const currentRoom = this.room();
    const password = this.joinForm.controls.password.value;
    if (currentRoom?.isPrivate && !password.trim()) {
      this.joinForm.controls.password.setErrors({ required: true });
      this.joinForm.controls.password.markAsTouched();
      return;
    }

    this.membershipChanging.set(true);

    this.roomsApi
      .joinRoom(this.roomId, password || null)
      .pipe(
        switchMap(() => this.roomsApi.getRoom(this.roomId)),
        finalize(() => this.membershipChanging.set(false)),
      )
      .subscribe({
        next: (room) => {
          this.room.set(room);
          this.joinForm.reset();
          this.loadMemberData();
          void this.joinRealtimeRoom();
        },
        error: (error: unknown) => this.errorNotifications.show(error, 'Could not join the room.'),
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
      .catch((error: unknown) => this.errorNotifications.show(error, 'Could not send the message.'))
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

  protected saveRoomName(): void {
    if (this.roomSettingsForm.invalid) {
      this.roomSettingsForm.markAllAsTouched();
      return;
    }

    this.settingsSaving.set(true);
    this.roomsApi
      .updateRoom(this.roomId, this.roomSettingsForm.getRawValue().name.trim())
      .pipe(finalize(() => this.settingsSaving.set(false)))
      .subscribe({
        next: (room) => {
          this.room.set(room);
          this.roomSettingsForm.controls.name.setValue(room.name);
        },
        error: (error: unknown) =>
          this.errorNotifications.show(error, 'Could not rename the room.'),
      });
  }

  protected saveRoomPassword(): void {
    if (this.passwordSettingsForm.invalid) {
      this.passwordSettingsForm.markAllAsTouched();
      return;
    }

    this.settingsSaving.set(true);
    this.roomsApi
      .changePassword(this.roomId, this.passwordSettingsForm.getRawValue().password)
      .pipe(finalize(() => this.settingsSaving.set(false)))
      .subscribe({
        next: () => this.passwordSettingsForm.reset(),
        error: (error: unknown) =>
          this.errorNotifications.show(error, 'Could not change the room password.'),
      });
  }

  protected removeMember(member: RoomMember): void {
    if (!window.confirm(`Remove ${member.username} from this room?`)) {
      return;
    }

    this.memberRemovingId.set(member.userId);
    this.roomsApi
      .removeMember(this.roomId, member.userId)
      .pipe(finalize(() => this.memberRemovingId.set(null)))
      .subscribe({
        next: () =>
          this.members.update((members) =>
            members.filter((candidate) => candidate.userId !== member.userId),
          ),
        error: (error: unknown) =>
          this.errorNotifications.show(error, 'Could not remove the member.'),
      });
  }

  protected generateInvite(): void {
    this.inviteGenerating.set(true);
    this.roomsApi
      .generateInvite(this.roomId)
      .pipe(finalize(() => this.inviteGenerating.set(false)))
      .subscribe({
        next: (invite) => this.inviteLink.set(`${window.location.origin}/invite/${invite.token}`),
        error: (error: unknown) =>
          this.errorNotifications.show(error, 'Could not generate an invitation.'),
      });
  }

  protected copyInvite(): void {
    const link = this.inviteLink();
    if (!link) return;

    void navigator.clipboard
      .writeText(link)
      .catch((error: unknown) =>
        this.errorNotifications.show(error, 'Could not copy the invitation.'),
      );
  }

  protected deleteRoom(): void {
    const currentRoom = this.room();
    if (!currentRoom || !window.confirm(`Delete "${currentRoom.name}" and all of its messages?`)) {
      return;
    }

    this.settingsSaving.set(true);
    this.roomsApi
      .deleteRoom(this.roomId)
      .pipe(finalize(() => this.settingsSaving.set(false)))
      .subscribe({
        next: () => void this.router.navigate(['/rooms']),
        error: (error: unknown) =>
          this.errorNotifications.show(error, 'Could not delete the room.'),
      });
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
        error: (error: unknown) => this.errorNotifications.show(error, 'Could not load room data.'),
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
