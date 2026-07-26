using DistributedChat.Api.Dtos;
using DistributedChat.Api.Http;
using DistributedChat.Application.Messages;
using DistributedChat.Application.Rooms;
using FluentValidation;

namespace DistributedChat.Api.Endpoints;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rooms")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingPolicies.Api);

        group.MapPost("", CreateRoomEndpoint);
        group.MapGet("", GetRoomsEndpoint);
        group.MapGet("/{roomId:guid}", GetRoomEndpoint);
        group.MapPost("/{roomId:guid}/join", JoinRoomEndpoint);
        group.MapPost("/invitations/{token}/join", JoinRoomByInviteEndpoint);
        group.MapPost("/{roomId:guid}/leave", LeaveRoomEndpoint);
        group.MapGet("/{roomId:guid}/members", GetRoomMembersEndpoint);
        group.MapPut("/{roomId:guid}", UpdateRoomEndpoint);
        group.MapPut("/{roomId:guid}/password", ChangeRoomPasswordEndpoint);
        group.MapDelete("/{roomId:guid}/members/{userId:guid}", RemoveRoomMemberEndpoint);
        group.MapDelete("/{roomId:guid}", DeleteRoomEndpoint);
        group.MapPost("/{roomId:guid}/invite", GenerateInviteEndpoint);
        group.MapGet("/{roomId:guid}/messages", GetRoomMessagesEndpoint);

        return app;
    }

    private static async Task<IResult> CreateRoomEndpoint(
        CreateRoomDto dto,
        IValidator<CreateRoomDto> validator,
        IRoomService roomService
    )
    {
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblemResult();
        }

        var request = new CreateRoomRequest(dto.Name!.Trim(), dto.IsPrivate, dto.Password);
        var result = await roomService.CreateRoomAsync(request);

        return result.ToCreatedResult(room => $"/api/rooms/{room.Id}");
    }

    private static async Task<IResult> GetRoomsEndpoint(IRoomService roomService)
    {
        var result = await roomService.GetRoomsAsync();

        return result.ToResult();
    }

    private static async Task<IResult> GetRoomEndpoint(
        Guid roomId,
        IRoomService roomService
    )
    {
        var result = await roomService.GetRoomAsync(roomId);

        return result.ToResult();
    }

    private static async Task<IResult> JoinRoomEndpoint(
        Guid roomId,
        JoinRoomDto? dto,
        IRoomService roomService
    )
    {
        var result = await roomService.JoinRoomAsync(roomId, new JoinRoomRequest(dto?.Password));

        return result.ToResult();
    }

    private static async Task<IResult> JoinRoomByInviteEndpoint(
        string token,
        IRoomService roomService
    )
    {
        var result = await roomService.JoinRoomByInviteAsync(token);

        return result.ToResult();
    }

    private static async Task<IResult> LeaveRoomEndpoint(
        Guid roomId,
        IRoomService roomService
    )
    {
        var result = await roomService.LeaveRoomAsync(roomId);

        return result.ToResult();
    }

    private static async Task<IResult> GetRoomMembersEndpoint(
        Guid roomId,
        IRoomService roomService
    )
    {
        var result = await roomService.GetRoomMembersAsync(roomId);

        return result.ToResult();
    }

    private static async Task<IResult> UpdateRoomEndpoint(
        Guid roomId,
        UpdateRoomDto dto,
        IValidator<UpdateRoomDto> validator,
        IRoomService roomService
    )
    {
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblemResult();
        }

        var result = await roomService.UpdateRoomAsync(roomId, new UpdateRoomRequest(dto.Name!.Trim()));
        return result.ToResult();
    }

    private static async Task<IResult> ChangeRoomPasswordEndpoint(
        Guid roomId,
        ChangeRoomPasswordDto dto,
        IValidator<ChangeRoomPasswordDto> validator,
        IRoomService roomService
    )
    {
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblemResult();
        }

        var result = await roomService.ChangeRoomPasswordAsync(
            roomId,
            new ChangeRoomPasswordRequest(dto.Password));

        return result.ToResult();
    }

    private static async Task<IResult> RemoveRoomMemberEndpoint(
        Guid roomId,
        Guid userId,
        IRoomService roomService
    )
    {
        var result = await roomService.RemoveRoomMemberAsync(roomId, userId);
        return result.ToResult();
    }

    private static async Task<IResult> DeleteRoomEndpoint(
        Guid roomId,
        IRoomService roomService
    )
    {
        var result = await roomService.DeleteRoomAsync(roomId);
        return result.ToResult();
    }

    private static async Task<IResult> GenerateInviteEndpoint(
        Guid roomId,
        IRoomService roomService
    )
    {
        var result = await roomService.GenerateInviteAsync(roomId);
        return result.ToResult();
    }

    private static async Task<IResult> GetRoomMessagesEndpoint(
        Guid roomId,
        [AsParameters] GetRoomMessagesQuery query,
        IMessageService messageService
    )
    {
        var result = await messageService.GetRoomMessagesAsync(
            roomId,
            query.Before,
            query.Limit ?? 50);

        return result.ToResult();
    }

    public sealed class GetRoomMessagesQuery
    {
        public Guid? Before { get; init; }

        public int? Limit { get; init; }
    }
}
