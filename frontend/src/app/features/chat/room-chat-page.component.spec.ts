import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { NEVER, of } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { ChatRealtimeService } from '../../core/chat/chat-realtime.service';
import { ErrorNotificationService } from '../../core/notifications/error-notification.service';
import { RoomsApiService } from '../../core/rooms/rooms-api.service';
import { RoomChatPageComponent } from './room-chat-page.component';

describe('RoomChatPageComponent', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    const joinRoom = vi.fn(() => NEVER);

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
        { provide: RoomsApiService, useValue: { joinRoom } },
        {
          provide: ErrorNotificationService,
          useValue: { show: vi.fn(), showMessage: vi.fn() },
        },
      ],
    });

    const component = TestBed.runInInjectionContext(() => new RoomChatPageComponent());
    const testableComponent = component as unknown as {
      room: {
        set(value: {
          id: string;
          name: string;
          createdByUserId: string;
          createdAt: string;
          isPrivate: boolean;
          isMember: boolean;
        }): void;
      };
      joinForm: { controls: { password: { setValue(value: string): void } } };
      roomId: string;
      joinRoom(): void;
    };

    expect(component).toBeTruthy();
    testableComponent.room.set({
      id: 'room-id',
      name: 'Private room',
      createdByUserId: 'owner-id',
      createdAt: '2026-07-26T00:00:00Z',
      isPrivate: true,
      isMember: false,
    });
    testableComponent.roomId = 'room-id';
    testableComponent.joinForm.controls.password.setValue(' secret123');
    testableComponent.joinRoom();
    expect(joinRoom).toHaveBeenCalledWith('room-id', ' secret123');
  });
});
