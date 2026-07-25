import { Injectable, InjectionToken, inject } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
} from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { AuthTokenService } from '../auth/auth-token.service';
import { APP_CONFIG } from '../config/app-config';
import {
  ChatMessageCreated,
  MessageDto,
  SendMessageRequest,
  UserRoomPresenceEvent,
} from '../models/chat.models';

export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

const AUTHENTICATION_REQUIRED_MESSAGE = 'Authentication is required.';

export interface ChatHubConnection {
  readonly state: HubConnectionState;
  start(): Promise<void>;
  stop(): Promise<void>;
  invoke<T = unknown>(methodName: string, ...args: readonly unknown[]): Promise<T>;
  on(methodName: string, newMethod: (...args: readonly unknown[]) => void): void;
  onreconnecting(callback: (error?: Error) => void): void;
  onreconnected(callback: (connectionId?: string) => void): void;
  onclose(callback: (error?: Error) => void): void;
}

export const CHAT_HUB_CONNECTION_FACTORY = new InjectionToken<() => ChatHubConnection>(
  'CHAT_HUB_CONNECTION_FACTORY',
  {
    providedIn: 'root',
    factory: () => {
      const tokenService = inject(AuthTokenService);
      const appConfig = inject(APP_CONFIG);

      return () =>
        new HubConnectionBuilder()
          .withUrl(appConfig.signalRHubUrl, {
            accessTokenFactory: () => tokenService.getToken() ?? '',
            skipNegotiation: true,
            transport: HttpTransportType.WebSockets,
          })
          .withAutomaticReconnect()
          .build() as HubConnection;
    },
  },
);

@Injectable({ providedIn: 'root' })
export class ChatRealtimeService {
  private readonly authService = inject(AuthService);
  private readonly connectionFactory = inject(CHAT_HUB_CONNECTION_FACTORY);
  private readonly connectionStateSubject = new BehaviorSubject<ConnectionStatus>('disconnected');
  private readonly messageReceivedSubject = new Subject<ChatMessageCreated>();
  private readonly userJoinedRoomSubject = new Subject<UserRoomPresenceEvent>();
  private readonly userLeftRoomSubject = new Subject<UserRoomPresenceEvent>();
  private readonly reconnectedSubject = new Subject<void>();

  private connection: ChatHubConnection | null = null;
  private activeRoomId: string | null = null;
  private startPromise: Promise<void> | null = null;

  readonly connectionState$ = this.connectionStateSubject.asObservable();
  readonly messageReceived$ = this.messageReceivedSubject.asObservable();
  readonly userJoinedRoom$ = this.userJoinedRoomSubject.asObservable();
  readonly userLeftRoom$ = this.userLeftRoomSubject.asObservable();
  readonly reconnected$ = this.reconnectedSubject.asObservable();

  get connectionStateSnapshot(): ConnectionStatus {
    return this.connectionStateSubject.value;
  }

  async joinRoom(roomId: string): Promise<void> {
    this.activeRoomId = roomId;
    try {
      await this.ensureStarted();
      await this.invokeJoinRoom(roomId);
    } catch (error) {
      if (this.activeRoomId === roomId) {
        this.activeRoomId = null;
      }

      this.handleAuthenticationError(error);
      throw error;
    }
  }

  async leaveRoom(roomId: string): Promise<void> {
    const wasActiveRoom = this.activeRoomId === roomId;
    this.activeRoomId = wasActiveRoom ? null : this.activeRoomId;

    if (wasActiveRoom && this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke('LeaveRoom', roomId);
    }
  }

  async sendMessage(request: SendMessageRequest): Promise<MessageDto> {
    await this.ensureStarted();
    const connection = this.connection;
    if (!connection) {
      throw new Error('SignalR connection has not been created.');
    }

    try {
      return await connection.invoke<MessageDto>('SendMessage', request);
    } catch (error) {
      this.handleAuthenticationError(error);
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    this.activeRoomId = null;
    if (!this.connection) {
      this.connectionStateSubject.next('disconnected');

      return;
    }

    await this.connection.stop();
    this.connectionStateSubject.next('disconnected');
  }

  private ensureStarted(): Promise<void> {
    const connection = this.getOrCreateConnection();
    if (connection.state === HubConnectionState.Connected) {
      return Promise.resolve();
    }

    if (this.startPromise) {
      return this.startPromise;
    }

    this.connectionStateSubject.next('connecting');
    this.startPromise = connection
      .start()
      .then(() => this.connectionStateSubject.next('connected'))
      .catch((error: unknown) => {
        this.connectionStateSubject.next('disconnected');
        this.handleAuthenticationError(error);
        throw error;
      })
      .finally(() => {
        this.startPromise = null;
      });

    return this.startPromise;
  }

  private getOrCreateConnection(): ChatHubConnection {
    if (!this.connection) {
      this.connection = this.connectionFactory();
      this.registerHandlers(this.connection);
    }

    return this.connection;
  }

  private registerHandlers(connection: ChatHubConnection): void {
    connection.on('MessageReceived', (event) =>
      this.messageReceivedSubject.next(event as ChatMessageCreated),
    );
    connection.on('UserJoinedRoom', (event) =>
      this.userJoinedRoomSubject.next(event as UserRoomPresenceEvent),
    );
    connection.on('UserLeftRoom', (event) =>
      this.userLeftRoomSubject.next(event as UserRoomPresenceEvent),
    );

    connection.onreconnecting(() => this.connectionStateSubject.next('reconnecting'));
    connection.onreconnected(() => void this.handleReconnected());
    connection.onclose(() => this.connectionStateSubject.next('disconnected'));
  }

  private async handleReconnected(): Promise<void> {
    const roomId = this.activeRoomId;
    if (!roomId) {
      this.connectionStateSubject.next('connected');

      return;
    }

    try {
      await this.invokeJoinRoom(roomId);
      this.connectionStateSubject.next('connected');
      this.reconnectedSubject.next();
    } catch (error) {
      if (this.activeRoomId === roomId) {
        this.activeRoomId = null;
      }

      this.connectionStateSubject.next('disconnected');
      this.handleAuthenticationError(error);
    }
  }

  private invokeJoinRoom(roomId: string): Promise<void> {
    if (!this.connection) {
      return Promise.reject(new Error('SignalR connection has not been created.'));
    }

    return this.connection.invoke('JoinRoom', roomId);
  }

  private handleAuthenticationError(error: unknown): void {
    if (error instanceof Error && error.message.includes(AUTHENTICATION_REQUIRED_MESSAGE)) {
      this.authService.expireSession();
    }
  }
}
