export interface MessageDto {
  id: string;
  roomId: string;
  senderUserId: string;
  senderUsername: string;
  content: string;
  createdAt: string;
}

export interface SendMessageRequest {
  roomId: string;
  content: string;
}

export interface ChatMessageCreated {
  eventId: string;
  messageId: string;
  roomId: string;
  senderUserId: string;
  senderUsername: string;
  content: string;
  createdAt: string;
}

export interface UserRoomPresenceEvent {
  eventId: string;
  roomId: string;
  userId: string;
  username?: string | null;
  connectionId: string;
  instanceId: string;
}
