import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { App } from './app';
import { AuthService } from './core/auth/auth.service';
import { ChatRealtimeService } from './core/chat/chat-realtime.service';

describe('App', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { currentUser$: of(null), logout: vi.fn() } },
        {
          provide: ChatRealtimeService,
          useValue: { connectionState$: of('disconnected'), disconnect: vi.fn() },
        },
      ],
    });

    const component = TestBed.runInInjectionContext(() => new App());

    expect(component).toBeTruthy();
  });
});
