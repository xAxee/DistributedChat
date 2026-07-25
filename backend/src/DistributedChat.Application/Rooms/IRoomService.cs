using DistributedChat.Application.Common.Dto;
using DistributedChat.Application.Common.Results;

namespace DistributedChat.Application.Rooms;

public interface IRoomService
{
    Task<Result<RoomDetailsDto>> CreateRoomAsync(CreateRoomRequest request);
    Task<Result<IReadOnlyCollection<RoomSummaryDto>>> GetRoomsAsync();
    Task<Result<RoomDetailsDto>> GetRoomAsync(Guid roomId);
    Task<Result> JoinRoomAsync(Guid roomId);
    Task<Result> LeaveRoomAsync(Guid roomId);
    Task<Result<IReadOnlyCollection<RoomMemberDto>>> GetRoomMembersAsync(Guid roomId);
}
