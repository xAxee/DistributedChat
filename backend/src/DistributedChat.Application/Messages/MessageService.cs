using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Common.Dto;
using DistributedChat.Application.Common.Results;
using DistributedChat.Application.Rooms;
using DistributedChat.Domain.Messages;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DistributedChat.Application.Messages;

public sealed partial class MessageService(
    IMessageStore messageStore,
    IRoomStore roomStore,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IChatEventPublisher chatEventPublisher,
    IValidator<SendMessageRequest> sendMessageValidator,
    ILogger<MessageService> logger
) : IMessageService
{
    private const int MinimumHistoryLimit = 1;
    private const int MaximumHistoryLimit = 100;

    public async Task<Result<MessageDto>> SendMessageAsync(SendMessageRequest request)
    {
        var validation = await sendMessageValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return Result.Failure<MessageDto>(validation.ToApplicationError());
        }

        var memberResult = await GetRoomMemberUserIdAsync(request.RoomId);
        if (memberResult.IsFailure)
        {
            return Result.Failure<MessageDto>(memberResult.Error!);
        }

        var message = Message.Create(
            Guid.NewGuid(),
            request.RoomId,
            memberResult.Value,
            request.Content!,
            timeProvider.GetUtcNow());

        var createdMessage = await messageStore.CreateAsync(message);

        var messageCreated = new ChatMessageCreated(
            Guid.NewGuid(),
            createdMessage.Id,
            createdMessage.RoomId,
            createdMessage.SenderUserId,
            createdMessage.SenderUsername,
            createdMessage.Content,
            createdMessage.CreatedAt);

        try
        {
            await chatEventPublisher.PublishMessageCreatedAsync(messageCreated);
        }
        catch (Exception exception)
        {
            LogChatEventPublishFailed(
                logger,
                exception,
                messageCreated.EventId,
                createdMessage.Id,
                createdMessage.RoomId);
        }

        return Result.Success(createdMessage);
    }

    public async Task<Result<CursorPagedResponse<MessageDto>>> GetRoomMessagesAsync(
        Guid roomId,
        Guid? before,
        int limit
    )
    {
        if (limit is < MinimumHistoryLimit or > MaximumHistoryLimit)
        {
            return Result.Failure<CursorPagedResponse<MessageDto>>(MessageErrors.InvalidLimit());
        }

        var memberResult = await GetRoomMemberUserIdAsync(roomId);
        if (memberResult.IsFailure)
        {
            return Result.Failure<CursorPagedResponse<MessageDto>>(memberResult.Error!);
        }

        MessageCursor? cursor = null;

        if (before is Guid messageId)
        {
            cursor = await messageStore.GetCursorAsync(roomId, messageId);
            if (cursor is null)
            {
                return Result.Failure<CursorPagedResponse<MessageDto>>(MessageErrors.InvalidCursor());
            }
        }

        var messages = await messageStore.GetMessagesAsync(roomId, cursor, limit + 1);
        var hasMore = messages.Count > limit;
        var items = messages.Take(limit).ToArray();

        Guid? nextCursor = hasMore ? items[^1].Id : null;

        return Result.Success(
            new CursorPagedResponse<MessageDto>(items, nextCursor, hasMore));
    }

    private async Task<Result<Guid>> GetRoomMemberUserIdAsync(Guid roomId)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not Guid userId)
        {
            return Result.Failure<Guid>(RoomErrors.Unauthenticated());
        }

        if (!await roomStore.ExistsAsync(roomId))
        {
            return Result.Failure<Guid>(RoomErrors.NotFound());
        }

        if (!await roomStore.IsMemberAsync(roomId, userId))
        {
            return Result.Failure<Guid>(RoomErrors.MembershipRequired());
        }

        return Result.Success(userId);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Chat message {MessageId} was saved, but publishing chat event {EventId} for room {RoomId} failed.")]
    private static partial void LogChatEventPublishFailed(
        ILogger logger,
        Exception exception,
        Guid eventId,
        Guid messageId,
        Guid roomId);
}
