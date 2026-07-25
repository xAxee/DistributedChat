import { TestBed } from '@angular/core/testing';
import { HubConnectionState } from '@microsoft/signalr';

import { AuthService } from '../auth/auth.service';
import {
  CHAT_HUB_CONNECTION_FACTORY,
  ChatHubConnection,
  ChatRealtimeService,
} from './chat-realtime.service';

describe('ChatRealtimeService', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { expireSession: vi.fn() } },
        { provide: CHAT_HUB_CONNECTION_FACTORY, useValue: () => new FakeChatHubConnection() },
      ],
    });

    expect(TestBed.inject(ChatRealtimeService)).toBeTruthy();
  });
});

class FakeChatHubConnection implements ChatHubConnection {
  readonly state = HubConnectionState.Disconnected;

  start(): Promise<void> {
    return Promise.resolve();
  }

  stop(): Promise<void> {
    return Promise.resolve();
  }

  invoke<T = unknown>(): Promise<T> {
    return Promise.resolve(undefined as T);
  }

  on(methodName: string, method: (...args: readonly unknown[]) => void): void {
    void methodName;
    void method;
  }

  onreconnecting(callback: (error?: Error) => void): void {
    void callback;
  }

  onreconnected(callback: (connectionId?: string) => void): void {
    void callback;
  }

  onclose(callback: (error?: Error) => void): void {
    void callback;
  }
}
