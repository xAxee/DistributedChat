using DistributedChat.Application.Common.Dto;
using DistributedChat.Domain.Messages;

namespace DistributedChat.Application.Messages;

public interface IMessageStore
{
    Task<MessageDto> CreateAsync(Message message, CancellationToken cancellationToken = default);

    Task<MessageCursor?> GetCursorAsync(
        Guid roomId,
        Guid messageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MessageDto>> GetMessagesAsync(
        Guid roomId,
        MessageCursor? before,
        int take,
        CancellationToken cancellationToken = default);
}

public sealed record MessageCursor(Guid Id, DateTimeOffset CreatedAt);
