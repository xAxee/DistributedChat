import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ErrorNotificationService } from '../../core/notifications/error-notification.service';
import { RoomsApiService } from '../../core/rooms/rooms-api.service';

@Component({
  selector: 'app-invite-join-page',
  imports: [RouterLink],
  templateUrl: './invite-join-page.component.html',
  styleUrl: './invite-join-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InviteJoinPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly roomsApi = inject(RoomsApiService);
  private readonly errorNotifications = inject(ErrorNotificationService);

  protected readonly joining = signal(true);
  protected readonly failed = signal(false);

  ngOnInit(): void {
    const token = this.route.snapshot.paramMap.get('token');
    if (!token) {
      this.joining.set(false);
      this.failed.set(true);
      return;
    }

    this.roomsApi.joinRoomByInvite(token).subscribe({
      next: (room) => void this.router.navigate(['/rooms', room.id]),
      error: (error: unknown) => {
        this.joining.set(false);
        this.failed.set(true);
        this.errorNotifications.show(error, 'Could not use this invitation.');
      },
    });
  }
}
