namespace DistributedChat.Application.Common.Dto;

public sealed record RoomMemberDto(
    Guid RoomId,
    Guid UserId,
    string Username,
    DateTimeOffset JoinedAt,
    bool IsOnline,
    IReadOnlyCollection<string> ConnectedInstanceIds);
