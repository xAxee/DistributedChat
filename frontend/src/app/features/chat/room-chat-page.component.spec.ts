import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { RoomDetails } from '../../core/models/room.models';
import { RoomChatFacade } from './room-chat.facade';
import { RoomChatPageComponent } from './room-chat-page.component';

describe('RoomChatPageComponent', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    const joinRoom = vi.fn(() => Promise.resolve(true));
    const room = signal<RoomDetails | null>(null);

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({}) } },
        },
        { provide: AuthService, useValue: { currentUserSnapshot: null } },
        {
          provide: RoomChatFacade,
          useValue: {
            room,
            members: signal([]),
            messages: signal([]),
            hasMore: signal(false),
            loading: signal(false),
            loadingOlder: signal(false),
            sending: signal(false),
            membershipChanging: signal(false),
            settingsSaving: signal(false),
            memberRemovingId: signal(null),
            inviteLink: signal(null),
            inviteGenerating: signal(false),
            connectionState$: of('disconnected'),
            scrollToBottom$: of(),
            initialize: vi.fn(),
            joinRoom,
          },
        },
      ],
    });

    const component = TestBed.runInInjectionContext(() => new RoomChatPageComponent());
    const testableComponent = component as unknown as {
      joinForm: { controls: { password: { setValue(value: string): void } } };
      joinRoom(): void;
    };

    expect(component).toBeTruthy();
    room.set({
      id: 'room-id',
      name: 'Private room',
      createdByUserId: 'owner-id',
      createdAt: '2026-07-26T00:00:00Z',
      isPrivate: true,
      isMember: false,
    });
    testableComponent.joinForm.controls.password.setValue(' secret123');
    testableComponent.joinRoom();
    expect(joinRoom).toHaveBeenCalledWith(' secret123');
  });
});
