namespace DistributedChat.Application.Common.Dto;

public sealed record CursorPage<T>(IReadOnlyCollection<T> Items, string? NextCursor, bool HasMore);
