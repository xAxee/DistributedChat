import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';

import { RoomDetails } from '../../core/models/room.models';
import { ErrorNotificationService } from '../../core/notifications/error-notification.service';
import { RoomsApiService } from '../../core/rooms/rooms-api.service';

@Component({
  selector: 'app-rooms-page',
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './rooms-page.component.html',
  styleUrl: './rooms-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomsPageComponent implements OnInit {
  private readonly roomsApi = inject(RoomsApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly router = inject(Router);
  private readonly errorNotifications = inject(ErrorNotificationService);

  protected readonly rooms = signal<readonly RoomDetails[]>([]);
  protected readonly loadingRooms = signal(false);
  protected readonly creatingRoom = signal(false);
  protected readonly actionRoomId = signal<string | null>(null);
  protected readonly joinedRooms = computed(() => this.rooms().filter((room) => room.isMember));
  protected readonly availableRooms = computed(() => this.rooms().filter((room) => !room.isMember));
  protected readonly joinedRoomCount = computed(() => this.joinedRooms().length);

  protected readonly createRoomForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
    visibility: ['public'],
    password: ['', [Validators.minLength(8), Validators.maxLength(100)]],
  });

  ngOnInit(): void {
    this.loadRooms();
  }

  protected refreshDashboard(): void {
    this.loadRooms();
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

  protected createRoom(): void {
    const values = this.createRoomForm.getRawValue();
    if (values.visibility === 'private' && !values.password.trim()) {
      this.createRoomForm.controls.password.setErrors({ required: true });
    }

    if (this.createRoomForm.invalid) {
      this.createRoomForm.markAllAsTouched();
      return;
    }
    this.creatingRoom.set(true);
    this.roomsApi
      .createRoom({
        name: values.name.trim(),
        isPrivate: values.visibility === 'private',
        password: values.visibility === 'private' ? values.password : null,
      })
      .pipe(finalize(() => this.creatingRoom.set(false)))
      .subscribe({
        next: (room) => {
          this.createRoomForm.reset();
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

  protected roomNameError(): string {
    const control = this.createRoomForm.controls.name;
    if (control.hasError('required')) return 'Room name is required.';
    if (control.hasError('minlength')) return 'Room name must be at least 3 characters.';
    return control.hasError('maxlength') ? 'Room name can be at most 50 characters.' : '';
  }

  protected roomPasswordError(): string {
    const control = this.createRoomForm.controls.password;
    if (control.hasError('required')) return 'Password is required for a private room.';
    if (control.hasError('minlength')) return 'Password must be at least 8 characters.';
    return control.hasError('maxlength') ? 'Password can be at most 100 characters.' : '';
  }

  protected isPrivateRoomSelected(): boolean {
    return this.createRoomForm.controls.visibility.value === 'private';
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
