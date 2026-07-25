using DistributedChat.Application.Common.Dto;
using DistributedChat.Application.Common.Results;

namespace DistributedChat.Application.Messages;

public interface IMessageService
{
    Task<Result<MessageDto>> SendMessageAsync(SendMessageRequest request);

    Task<Result<CursorPagedResponse<MessageDto>>> GetRoomMessagesAsync(
        Guid roomId,
        Guid? before,
        int limit);
}
