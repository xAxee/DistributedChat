namespace DistributedChat.Application.Common.Dto;

public sealed record CurrentUserDto(Guid Id, string Username, string Email, DateTimeOffset CreatedAt);
