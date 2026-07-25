using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DistributedChat.Application.Rooms;
using Microsoft.AspNetCore.SignalR;

namespace DistributedChat.Api.Hubs;

public sealed class ChatHubUserResolver
{
    public ChatHubUser GetAuthenticatedUser(ClaimsPrincipal? principal)
    {
        return new ChatHubUser(
            GetAuthenticatedUserId(principal),
            GetUsername(principal),
            GetEmail(principal));
    }

    public Guid? TryGetAuthenticatedUserId(ClaimsPrincipal? principal)
    {
        try
        {
            return GetAuthenticatedUserId(principal);
        }
        catch (HubException)
        {
            return null;
        }
    }

    private static Guid GetAuthenticatedUserId(ClaimsPrincipal? principal)
    {
        var value = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(value, out var userId))
        {
            return userId;
        }

        throw HubExceptionMapper.ToHubException(RoomErrors.Unauthenticated());
    }

    private static string? GetUsername(ClaimsPrincipal? principal)
    {
        return principal?.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
            ?? principal?.FindFirstValue(ClaimTypes.Name);
    }

    private static string? GetEmail(ClaimsPrincipal? principal)
    {
        return principal?.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? principal?.FindFirstValue(ClaimTypes.Email);
    }
}
