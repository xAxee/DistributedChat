import { ChatMessageCreated, MessageDto } from '../models/chat.models';

export function messageFromRealtimeEvent(event: ChatMessageCreated): MessageDto {
  return {
    id: event.messageId,
    roomId: event.roomId,
    senderUserId: event.senderUserId,
    senderUsername: event.senderUsername,
    content: event.content,
    createdAt: event.createdAt,
  };
}

export function upsertAndSortMessages(
  existing: readonly MessageDto[],
  incoming: readonly MessageDto[] | MessageDto,
): MessageDto[] {
  const messagesById = new Map<string, MessageDto>();
  const incomingMessages = Array.isArray(incoming) ? incoming : [incoming];

  for (const message of existing) {
    messagesById.set(message.id, message);
  }

  for (const message of incomingMessages) {
    messagesById.set(message.id, message);
  }

  return [...messagesById.values()].sort(compareMessages);
}

export function compareMessages(first: MessageDto, second: MessageDto): number {
  const createdAtComparison = Date.parse(first.createdAt) - Date.parse(second.createdAt);
  if (createdAtComparison !== 0) {
    return createdAtComparison;
  }

  return first.id.localeCompare(second.id);
}