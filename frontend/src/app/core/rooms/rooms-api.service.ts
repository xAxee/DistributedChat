import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { APP_CONFIG } from '../config/app-config';
import { MessageDto } from '../models/chat.models';
import { CursorPagedResponse } from '../models/common.models';
import { CreateRoomRequest, RoomDetails, RoomMember, RoomSummary } from '../models/room.models';

@Injectable({ providedIn: 'root' })
export class RoomsApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(APP_CONFIG).apiBaseUrl;

  getRooms(): Observable<readonly RoomSummary[]> {
    return this.http.get<readonly RoomSummary[]>(`${this.apiBaseUrl}/rooms`);
  }

  createRoom(request: CreateRoomRequest): Observable<RoomDetails> {
    return this.http.post<RoomDetails>(`${this.apiBaseUrl}/rooms`, request);
  }

  getRoom(roomId: string): Observable<RoomDetails> {
    return this.http.get<RoomDetails>(`${this.apiBaseUrl}/rooms/${roomId}`);
  }

  joinRoom(roomId: string): Observable<void> {
    return this.http.post<void>(`${this.apiBaseUrl}/rooms/${roomId}/join`, null);
  }

  leaveRoom(roomId: string): Observable<void> {
    return this.http.post<void>(`${this.apiBaseUrl}/rooms/${roomId}/leave`, null);
  }

  getMembers(roomId: string): Observable<readonly RoomMember[]> {
    return this.http.get<readonly RoomMember[]>(`${this.apiBaseUrl}/rooms/${roomId}/members`);
  }

  getMessages(
    roomId: string,
    options: { before?: string | null; limit?: number } = {},
  ): Observable<CursorPagedResponse<MessageDto>> {
    let params = new HttpParams().set('limit', String(options.limit ?? 50));
    if (options.before) {
      params = params.set('before', options.before);
    }

    return this.http.get<CursorPagedResponse<MessageDto>>(
      `${this.apiBaseUrl}/rooms/${roomId}/messages`,
      { params },
    );
  }
}