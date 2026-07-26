import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { RoomsApiService } from '../../core/rooms/rooms-api.service';

@Injectable({ providedIn: 'root' })
export class RoomInviteService {
  private readonly document = inject(DOCUMENT);
  private readonly roomsApi = inject(RoomsApiService);

  generateLink(roomId: string): Observable<string> {
    return this.roomsApi
      .generateInvite(roomId)
      .pipe(map((invite) => `${this.document.location.origin}/invite/${invite.token}`));
  }

  copyLink(link: string): Promise<void> {
    const clipboard = this.document.defaultView?.navigator.clipboard;
    if (!clipboard) {
      return Promise.reject(new Error('Clipboard API is unavailable.'));
    }

    return clipboard.writeText(link);
  }
}
