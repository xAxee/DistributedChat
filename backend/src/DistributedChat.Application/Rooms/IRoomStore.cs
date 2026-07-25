using DistributedChat.Application.Common.Dto;
using DistributedChat.Domain.Rooms;

namespace DistributedChat.Application.Rooms;

public interface IRoomStore
{
    Task<RoomDetailsDto> CreateAsync(
        Room room,
        RoomMember creatorMembership,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RoomSummaryDto>> ListAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<Room?> GetAsync(Guid roomId, CancellationToken cancellationToken = default);

    Task<Room?> GetByInviteTokenHashAsync(
        string inviteTokenHash,
        CancellationToken cancellationToken = default);

    Task<RoomDetailsDto?> GetDetailsAsync(
        Guid roomId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid roomId, CancellationToken cancellationToken = default);

    Task<bool> IsMemberAsync(Guid roomId, Guid userId, CancellationToken cancellationToken = default);

    Task JoinAsync(
        Guid roomId,
        Guid userId,
        DateTimeOffset joinedAt,
        CancellationToken cancellationToken = default);

    Task LeaveAsync(Guid roomId, Guid userId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid roomId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RoomMemberDto>> GetMembersAsync(
        Guid roomId,
        DateTimeOffset onlineAfter,
        CancellationToken cancellationToken = default);
}
