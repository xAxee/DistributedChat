import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { ChatRealtimeService } from '../../core/chat/chat-realtime.service';
import { ErrorNotificationService } from '../../core/notifications/error-notification.service';
import { RoomsApiService } from '../../core/rooms/rooms-api.service';
import { RoomChatPageComponent } from './room-chat-page.component';

describe('RoomChatPageComponent', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({}) } },
        },
        { provide: AuthService, useValue: { currentUserSnapshot: null } },
        {
          provide: ChatRealtimeService,
          useValue: {
            connectionState$: of('disconnected'),
            messageReceived$: of(),
            userJoinedRoom$: of(),
            userLeftRoom$: of(),
            reconnected$: of(),
            leaveRoom: vi.fn(),
          },
        },
        { provide: RoomsApiService, useValue: {} },
        {
          provide: ErrorNotificationService,
          useValue: { show: vi.fn(), showMessage: vi.fn() },
        },
      ],
    });

    const component = TestBed.runInInjectionContext(() => new RoomChatPageComponent());

    expect(component).toBeTruthy();
  });
});
