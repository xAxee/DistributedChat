import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';

import { CreateRoomRequest, RoomDetails } from '../../core/models/room.models';
import { ErrorNotificationService } from '../../core/notifications/error-notification.service';
import { RoomsApiService } from '../../core/rooms/rooms-api.service';
import { RoomCreatePopupComponent } from './room-create-popup/room-create-popup.component';

@Component({
  selector: 'app-rooms-page',
  imports: [DatePipe, RouterLink, RoomCreatePopupComponent],
  templateUrl: './rooms-page.component.html',
  styleUrl: './rooms-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomsPageComponent implements OnInit {
  private readonly roomsApi = inject(RoomsApiService);
  private readonly router = inject(Router);
  private readonly errorNotifications = inject(ErrorNotificationService);

  protected readonly rooms = signal<readonly RoomDetails[]>([]);
  protected readonly loadingRooms = signal(false);
  protected readonly creatingRoom = signal(false);
  protected readonly createRoomOpen = signal(false);
  protected readonly actionRoomId = signal<string | null>(null);
  protected readonly joinedRooms = computed(() => this.rooms().filter((room) => room.isMember));
  protected readonly availableRooms = computed(() => this.rooms().filter((room) => !room.isMember));
  protected readonly joinedRoomCount = computed(() => this.joinedRooms().length);

  ngOnInit(): void {
    this.loadRooms();
  }

  protected refreshDashboard(): void {
    this.loadRooms();
  }

  protected openCreateRoom(): void {
    this.createRoomOpen.set(true);
  }

  protected closeCreateRoom(): void {
    this.createRoomOpen.set(false);
  }

  protected loadRooms(): void {
    this.loadingRooms.set(true);
    this.roomsApi
      .getRooms()
      .pipe(
        finalize(() => this.loadingRooms.set(false)),
        catchError((error: unknown) => {
          this.errorNotifications.show(error, 'Could not load rooms.');
          return of([] as RoomDetails[]);
        }),
      )
      .subscribe((rooms) => this.rooms.set(rooms));
  }

  protected createRoom(request: CreateRoomRequest): void {
    this.creatingRoom.set(true);
    this.roomsApi
      .createRoom(request)
      .pipe(finalize(() => this.creatingRoom.set(false)))
      .subscribe({
        next: (room) => {
          this.createRoomOpen.set(false);
          this.rooms.update((rooms) => [room, ...rooms]);
          void this.router.navigate(['/rooms', room.id]);
        },
        error: (error: unknown) =>
          this.errorNotifications.show(error, 'Could not create the room.'),
      });
  }

  protected joinRoom(room: RoomDetails): void {
    this.runRoomAction(room.id, () => this.roomsApi.joinRoom(room.id), true);
  }

  protected leaveRoom(room: RoomDetails): void {
    this.runRoomAction(room.id, () => this.roomsApi.leaveRoom(room.id), false);
  }

  private runRoomAction(
    roomId: string,
    action: () => ReturnType<RoomsApiService['joinRoom']>,
    isMember: boolean,
  ): void {
    this.actionRoomId.set(roomId);
    action()
      .pipe(finalize(() => this.actionRoomId.set(null)))
      .subscribe({
        next: () =>
          this.rooms.update((rooms) =>
            rooms.map((room) => (room.id === roomId ? { ...room, isMember } : room)),
          ),
        error: (error: unknown) =>
          this.errorNotifications.show(error, 'Could not update room membership.'),
      });
  }
}
