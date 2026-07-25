using DistributedChat.Api.Dtos;
using DistributedChat.Application.Common.Dto;
using DistributedChat.Application.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Serilog.Context;

namespace DistributedChat.Api.Hubs;

[Authorize]
public sealed class ChatHub(
    IMessageService messageService,
    HubCurrentUserContext hubCurrentUserContext,
    LocalSignalRSendMessageRateLimiter sendMessageRateLimiter,
    ChatHubUserResolver userResolver,
    ChatConnectionLifecycleService connectionLifecycleService,
    ChatRoomSubscriptionService roomSubscriptionService
) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var user = userResolver.GetAuthenticatedUser(Context.User);
        using (LogContext.PushProperty("UserId", user.UserId))
        {
            await connectionLifecycleService.ConnectAsync(
                Context.ConnectionId,
                user.UserId,
                Context.ConnectionAborted);

            await base.OnConnectedAsync();
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var fallbackUserId = userResolver.TryGetAuthenticatedUserId(Context.User);
        using (LogContext.PushProperty("UserId", fallbackUserId))
        {
            await connectionLifecycleService.DisconnectAsync(
                Context.ConnectionId,
                fallbackUserId,
                CancellationToken.None);

            await base.OnDisconnectedAsync(exception);
        }
    }

    public async Task JoinRoom(Guid roomId)
    {
        var user = userResolver.GetAuthenticatedUser(Context.User);
        using (LogContext.PushProperty("UserId", user.UserId))
        using (LogContext.PushProperty("RoomId", roomId))
        {
            await roomSubscriptionService.JoinRoomAsync(
                Context.ConnectionId,
                user,
                roomId,
                Groups,
                Context.ConnectionAborted);
        }
    }

    public async Task LeaveRoom(Guid roomId)
    {
        var user = userResolver.GetAuthenticatedUser(Context.User);
        using (LogContext.PushProperty("UserId", user.UserId))
        using (LogContext.PushProperty("RoomId", roomId))
        {
            await roomSubscriptionService.LeaveRoomAsync(
                Context.ConnectionId,
                user,
                roomId,
                Groups,
                Context.ConnectionAborted);
        }
    }

    public async Task<MessageDto> SendMessage(SendMessageDto dto)
    {
        var user = userResolver.GetAuthenticatedUser(Context.User);
        using var currentUserScope = hubCurrentUserContext.Use(
            user.UserId,
            user.Username,
            user.Email);
        using var userScope = LogContext.PushProperty("UserId", user.UserId);
        using var roomScope = LogContext.PushProperty("RoomId", dto.RoomId);

        if (!sendMessageRateLimiter.TryAcquire(user.UserId))
        {
            throw new HubException("RateLimit.SendMessage: Too many messages. Please try again later.");
        }

        var request = new SendMessageRequest(dto.RoomId, dto.Content);
        var result = await messageService.SendMessageAsync(request);
        if (result.IsFailure)
        {
            throw HubExceptionMapper.ToHubException(result.Error!);
        }

        return result.Value;
    }
}
