using DistributedChat.Application.Common.Dto;
using DistributedChat.Application.Messages;
using DistributedChat.Domain.Messages;
using Microsoft.EntityFrameworkCore;

namespace DistributedChat.Infrastructure.Persistence.Messages;

public sealed class MessageStore(DistributedChatDbContext dbContext) : IMessageStore
{
    public async Task<MessageDto> CreateAsync(
        Message message,
        CancellationToken cancellationToken = default
    )
    {
        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.Messages
            .AsNoTracking()
            .Where(item => item.Id == message.Id)
            .Join(
                dbContext.Users.AsNoTracking(),
                item => item.SenderUserId,
                user => user.Id,
                (item, user) => new MessageDto(
                    item.Id,
                    item.RoomId,
                    item.SenderUserId,
                    user.Username,
                    item.Content,
                    item.CreatedAt))
            .SingleAsync(cancellationToken);
    }

    public Task<MessageCursor?> GetCursorAsync(
        Guid roomId,
        Guid messageId,
        CancellationToken cancellationToken = default
    )
    {
        return dbContext.Messages
            .AsNoTracking()
            .Where(message => message.RoomId == roomId && message.Id == messageId)
            .Select(message => new MessageCursor(message.Id, message.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MessageDto>> GetMessagesAsync(
        Guid roomId,
        MessageCursor? before,
        int take,
        CancellationToken cancellationToken = default
    )
    {
        var query = before is null
            ? dbContext.Messages
                .AsNoTracking()
                .Where(message => message.RoomId == roomId)
            : dbContext.Messages
                .FromSqlInterpolated(
                    $"""
                    SELECT id, room_id, sender_user_id, content, created_at
                    FROM messages
                    WHERE room_id = {roomId}
                      AND (created_at < {before.CreatedAt} OR (created_at = {before.CreatedAt} AND id < {before.Id}))
                    """)
                .AsNoTracking();

        return await query
            .OrderByDescending(message => message.CreatedAt)
            .ThenByDescending(message => message.Id)
            .Join(
                dbContext.Users.AsNoTracking(),
                message => message.SenderUserId,
                user => user.Id,
                (message, user) => new MessageDto(
                    message.Id,
                    message.RoomId,
                    message.SenderUserId,
                    user.Username,
                    message.Content,
                    message.CreatedAt))
            .Take(take)
            .ToListAsync(cancellationToken);
    }

}
