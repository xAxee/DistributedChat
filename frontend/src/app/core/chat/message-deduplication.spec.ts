import { MessageDto } from '../models/chat.models';
import { upsertAndSortMessages } from './message-deduplication';

describe('message deduplication', () => {
  it('does not add the same MessageId twice', () => {
    const firstMessage = createMessage({ id: 'message-1' });
    const duplicateMessage = createMessage({ id: 'message-1', content: 'Duplicate' });
    const laterMessage = createMessage({
      id: 'message-2',
      createdAt: '2026-07-11T12:01:00.000Z',
    });

    const result = upsertAndSortMessages([firstMessage], [duplicateMessage, laterMessage]);

    expect(result.map((message) => message.id)).toEqual(['message-1', 'message-2']);
  });
});

function createMessage(overrides: Partial<MessageDto> = {}): MessageDto {
  return {
    id: 'message-1',
    roomId: 'room-1',
    senderUserId: 'user-1',
    senderUsername: 'alice',
    content: 'Hello',
    createdAt: '2026-07-11T12:00:00.000Z',
    ...overrides,
  };
}
