import { DestroyRef, Injectable, OnDestroy, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Subject, firstValueFrom, forkJoin } from 'rxjs';

import { ChatRealtimeService } from '../../core/chat/chat-realtime.service';
import {
  messageFromRealtimeEvent,
  upsertAndSortMessages,
} from '../../core/chat/message-deduplication';
import { MessageDto } from '../../core/models/chat.models';
import { RoomDetails, RoomMember } from '../../core/models/room.models';
import { ErrorNotificationService } from '../../core/notifications/error-notification.service';
import { RoomsApiService } from '../../core/rooms/rooms-api.service';
import { RoomInviteService } from './room-invite.service';

@Injectable()
export class RoomChatFacade implements OnDestroy {
  private readonly chatRealtime = inject(ChatRealtimeService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly errorNotifications = inject(ErrorNotificationService);
  private readonly inviteService = inject(RoomInviteService);
  private readonly roomsApi = inject(RoomsApiService);
  private readonly router = inject(Router);
  private readonly scrollToBottomSubject = new Subject<void>();

  private roomId = '';
  private nextCursor: string | null = null;

  readonly room = signal<RoomDetails | null>(null);
  readonly members = signal<readonly RoomMember[]>([]);
  readonly messages = signal<readonly MessageDto[]>([]);
  readonly hasMore = signal(false);
  readonly loading = signal(false);
  readonly loadingOlder = signal(false);
  readonly sending = signal(false);
  readonly membershipChanging = signal(false);
  readonly settingsSaving = signal(false);
  readonly memberRemovingId = signal<string | null>(null);
  readonly inviteLink = signal<string | null>(null);
  readonly inviteGenerating = signal(false);
  readonly connectionState$ = this.chatRealtime.connectionState$;
  readonly scrollToBottom$ = this.scrollToBottomSubject.asObservable();

  constructor() {
    this.registerRealtimeHandlers();
  }

  ngOnDestroy(): void {
    if (this.roomId) {
      void this.chatRealtime.leaveRoom(this.roomId);
    }
  }

  async initialize(roomId: string): Promise<void> {
    this.roomId = roomId;
    if (!roomId) {
      this.errorNotifications.showMessage('Invalid room identifier.');
      return;
    }

    this.loading.set(true);
    try {
      const room = await firstValueFrom(this.roomsApi.getRoom(roomId));
      this.room.set(room);
      if (room.isMember) {
        await Promise.all([this.loadMemberData(), this.joinRealtimeRoom()]);
      }
    } catch (error) {
      this.errorNotifications.show(error, 'Could not load the room.');
    } finally {
      this.loading.set(false);
    }
  }

  async joinRoom(password: string | null): Promise<boolean> {
    this.membershipChanging.set(true);
    try {
      await firstValueFrom(this.roomsApi.joinRoom(this.roomId, password));
      const room = await firstValueFrom(this.roomsApi.getRoom(this.roomId));
      this.room.set(room);
      await Promise.all([this.loadMemberData(), this.joinRealtimeRoom()]);
      return true;
    } catch (error) {
      this.errorNotifications.show(error, 'Could not join the room.');
      return false;
    } finally {
      this.membershipChanging.set(false);
    }
  }

  async leaveRoom(): Promise<void> {
    this.membershipChanging.set(true);
    try {
      await this.chatRealtime.leaveRoom(this.roomId).catch(() => undefined);
      await firstValueFrom(this.roomsApi.leaveRoom(this.roomId));
      await this.router.navigate(['/rooms']);
    } catch (error) {
      this.errorNotifications.show(error, 'Could not leave the room.');
    } finally {
      this.membershipChanging.set(false);
    }
  }

  async loadOlderMessages(): Promise<boolean> {
    const before = this.nextCursor;
    if (!before || this.loadingOlder()) {
      return false;
    }

    this.loadingOlder.set(true);
    try {
      const page = await firstValueFrom(
        this.roomsApi.getMessages(this.roomId, { before, limit: 50 }),
      );
      this.messages.update((messages) => upsertAndSortMessages(messages, page.items));
      this.nextCursor = page.nextCursor;
      this.hasMore.set(page.hasMore);
      return true;
    } catch (error) {
      this.errorNotifications.show(error, 'Could not load older messages.');
      return false;
    } finally {
      this.loadingOlder.set(false);
    }
  }

  async sendMessage(content: string): Promise<boolean> {
    this.sending.set(true);
    try {
      const message = await this.chatRealtime.sendMessage({ roomId: this.roomId, content });
      this.messages.update((messages) => upsertAndSortMessages(messages, message));
      this.requestScrollToBottom();
      return true;
    } catch (error) {
      this.errorNotifications.show(error, 'Could not send the message.');
      return false;
    } finally {
      this.sending.set(false);
    }
  }

  async renameRoom(name: string): Promise<void> {
    this.settingsSaving.set(true);
    try {
      this.room.set(await firstValueFrom(this.roomsApi.updateRoom(this.roomId, name)));
    } catch (error) {
      this.errorNotifications.show(error, 'Could not rename the room.');
    } finally {
      this.settingsSaving.set(false);
    }
  }

  async changeRoomPassword(password: string): Promise<boolean> {
    this.settingsSaving.set(true);
    try {
      await firstValueFrom(this.roomsApi.changePassword(this.roomId, password));
      return true;
    } catch (error) {
      this.errorNotifications.show(error, 'Could not change the room password.');
      return false;
    } finally {
      this.settingsSaving.set(false);
    }
  }

  async removeMember(userId: string): Promise<void> {
    this.memberRemovingId.set(userId);
    try {
      await firstValueFrom(this.roomsApi.removeMember(this.roomId, userId));
      this.members.update((members) => members.filter((member) => member.userId !== userId));
    } catch (error) {
      this.errorNotifications.show(error, 'Could not remove the member.');
    } finally {
      this.memberRemovingId.set(null);
    }
  }

  async generateInvite(): Promise<void> {
    this.inviteGenerating.set(true);
    try {
      this.inviteLink.set(await firstValueFrom(this.inviteService.generateLink(this.roomId)));
    } catch (error) {
      this.errorNotifications.show(error, 'Could not generate an invitation.');
    } finally {
      this.inviteGenerating.set(false);
    }
  }

  async copyInvite(): Promise<void> {
    const link = this.inviteLink();
    if (!link) {
      return;
    }

    try {
      await this.inviteService.copyLink(link);
    } catch (error) {
      this.errorNotifications.show(error, 'Could not copy the invitation.');
    }
  }

  async deleteRoom(): Promise<void> {
    this.settingsSaving.set(true);
    try {
      await firstValueFrom(this.roomsApi.deleteRoom(this.roomId));
      await this.router.navigate(['/rooms']);
    } catch (error) {
      this.errorNotifications.show(error, 'Could not delete the room.');
    } finally {
      this.settingsSaving.set(false);
    }
  }

  private async loadMemberData(): Promise<void> {
    try {
      const { members, messages } = await firstValueFrom(
        forkJoin({
          members: this.roomsApi.getMembers(this.roomId),
          messages: this.roomsApi.getMessages(this.roomId, { limit: 50 }),
        }),
      );

      this.members.set(members);
      this.messages.set(upsertAndSortMessages([], messages.items));
      this.nextCursor = messages.nextCursor;
      this.hasMore.set(messages.hasMore);
      this.requestScrollToBottom();
    } catch (error) {
      this.errorNotifications.show(error, 'Could not load room data.');
    }
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
          this.requestScrollToBottom();
        }
      });

    this.chatRealtime.userJoinedRoom$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event) => {
        if (event.roomId === this.roomId) {
          void this.reloadMembers();
        }
      });

    this.chatRealtime.userLeftRoom$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event) => {
      if (event.roomId === this.roomId) {
        void this.reloadMembers();
      }
    });

    this.chatRealtime.reconnected$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => void this.synchronizeAfterReconnect());
  }

  private async reloadMembers(): Promise<void> {
    try {
      this.members.set(await firstValueFrom(this.roomsApi.getMembers(this.roomId)));
    } catch (error) {
      this.errorNotifications.show(error, 'Could not refresh room members.');
    }
  }

  private async synchronizeAfterReconnect(): Promise<void> {
    try {
      const page = await firstValueFrom(this.roomsApi.getMessages(this.roomId, { limit: 50 }));
      const hasExistingHistory = this.messages().length > 0;
      this.messages.update((messages) => upsertAndSortMessages(messages, page.items));

      if (!hasExistingHistory) {
        this.nextCursor = page.nextCursor;
        this.hasMore.set(page.hasMore);
      }
      this.requestScrollToBottom();
    } catch (error) {
      this.errorNotifications.show(error, 'Could not synchronize room messages.');
    }

    await this.reloadMembers();
  }

  private requestScrollToBottom(): void {
    this.scrollToBottomSubject.next();
  }
}
