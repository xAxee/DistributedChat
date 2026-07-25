using DistributedChat.Application.Common.Dto;
using DistributedChat.Application.Common.Results;

namespace DistributedChat.Application.Rooms;

public interface IRoomService
{
    Task<Result<RoomDetailsDto>> CreateRoomAsync(CreateRoomRequest request);
    Task<Result<IReadOnlyCollection<RoomSummaryDto>>> GetRoomsAsync();
    Task<Result<RoomDetailsDto>> GetRoomAsync(Guid roomId);
    Task<Result> JoinRoomAsync(Guid roomId, JoinRoomRequest request);
    Task<Result<RoomDetailsDto>> JoinRoomByInviteAsync(string token);
    Task<Result> LeaveRoomAsync(Guid roomId);
    Task<Result<IReadOnlyCollection<RoomMemberDto>>> GetRoomMembersAsync(Guid roomId);
    Task<Result<RoomDetailsDto>> UpdateRoomAsync(Guid roomId, UpdateRoomRequest request);
    Task<Result> ChangeRoomPasswordAsync(Guid roomId, ChangeRoomPasswordRequest request);
    Task<Result> RemoveRoomMemberAsync(Guid roomId, Guid userId);
    Task<Result> DeleteRoomAsync(Guid roomId);
    Task<Result<RoomInviteDto>> GenerateInviteAsync(Guid roomId);
}
