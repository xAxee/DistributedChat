import { AsyncPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { MessageDto } from '../../core/models/chat.models';
import { RoomDetails, RoomMember } from '../../core/models/room.models';
import { RoomChatFacade } from './room-chat.facade';
import { RoomSettingsPopupComponent } from './room-settings-popup/room-settings-popup.component';

@Component({
  selector: 'app-room-chat-page',
  imports: [AsyncPipe, DatePipe, ReactiveFormsModule, RouterLink, RoomSettingsPopupComponent],
  providers: [RoomChatFacade],
  templateUrl: './room-chat-page.component.html',
  styleUrl: './room-chat-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomChatPageComponent implements OnInit {
  @ViewChild('messagesScroll')
  private messagesScroll?: ElementRef<HTMLDivElement>;

  private readonly authService = inject(AuthService);
  private readonly chat = inject(RoomChatFacade);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);

  protected readonly room = this.chat.room;
  protected readonly members = this.chat.members;
  protected readonly messages = this.chat.messages;
  protected readonly hasMore = this.chat.hasMore;
  protected readonly loading = this.chat.loading;
  protected readonly loadingOlder = this.chat.loadingOlder;
  protected readonly sending = this.chat.sending;
  protected readonly membershipChanging = this.chat.membershipChanging;
  protected readonly settingsSaving = this.chat.settingsSaving;
  protected readonly memberRemovingId = this.chat.memberRemovingId;
  protected readonly inviteLink = this.chat.inviteLink;
  protected readonly inviteGenerating = this.chat.inviteGenerating;
  protected readonly settingsOpen = signal(false);
  protected readonly currentUser = this.authService.currentUserSnapshot;
  protected readonly connectionState$ = this.chat.connectionState$;

  protected readonly messageForm = this.formBuilder.group({
    content: ['', [Validators.required, Validators.maxLength(2000)]],
  });

  protected readonly joinForm = this.formBuilder.group({
    password: ['', [Validators.maxLength(100)]],
  });

  ngOnInit(): void {
    this.chat.scrollToBottom$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.scrollMessagesToBottom());

    void this.chat.initialize(this.route.snapshot.paramMap.get('roomId') ?? '');
  }

  protected joinRoom(): void {
    const currentRoom = this.room();
    const password = this.joinForm.controls.password.value;
    if (currentRoom?.isPrivate && !password.trim()) {
      this.joinForm.controls.password.setErrors({ required: true });
      this.joinForm.controls.password.markAsTouched();
      return;
    }

    void this.chat.joinRoom(password || null).then((joined) => {
      if (joined) {
        this.joinForm.reset();
      }
    });
  }

  protected leaveRoom(): void {
    void this.chat.leaveRoom();
  }

  protected loadOlderMessages(): void {
    const scrollElement = this.messagesScroll?.nativeElement;
    const previousScrollHeight = scrollElement?.scrollHeight ?? 0;
    const previousScrollTop = scrollElement?.scrollTop ?? 0;

    void this.chat.loadOlderMessages().then((loaded) => {
      if (!loaded) {
        return;
      }

      this.afterMessagesRender(() => {
        if (scrollElement) {
          scrollElement.scrollTop =
            previousScrollTop + scrollElement.scrollHeight - previousScrollHeight;
        }
      });
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

    void this.chat.sendMessage(content).then((sent) => {
      if (sent) {
        this.messageForm.reset();
      }
    });
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

  protected openSettings(): void {
    this.settingsOpen.set(true);
  }

  protected closeSettings(): void {
    this.settingsOpen.set(false);
  }

  protected saveRoomName(name: string): void {
    void this.chat.renameRoom(name);
  }

  protected saveRoomPassword(password: string): void {
    void this.chat.changeRoomPassword(password).then((changed) => {
      if (changed) {
        this.closeSettings();
      }
    });
  }

  protected removeMember(member: RoomMember): void {
    if (window.confirm(`Remove ${member.username} from this room?`)) {
      void this.chat.removeMember(member.userId);
    }
  }

  protected generateInvite(): void {
    void this.chat.generateInvite();
  }

  protected copyInvite(): void {
    void this.chat.copyInvite();
  }

  protected deleteRoom(): void {
    const currentRoom = this.room();
    if (currentRoom && window.confirm(`Delete "${currentRoom.name}" and all of its messages?`)) {
      void this.chat.deleteRoom();
    }
  }

  protected userInitial(username: string): string {
    return username.trim().charAt(0).toUpperCase() || '?';
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
