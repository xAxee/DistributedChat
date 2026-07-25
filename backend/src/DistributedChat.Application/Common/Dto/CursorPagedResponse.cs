namespace DistributedChat.Application.Common.Dto;

public sealed record CursorPagedResponse<T>(
    IReadOnlyCollection<T> Items,
    Guid? NextCursor,
    bool HasMore
);
