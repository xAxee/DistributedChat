using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DistributedChat.Api.Hubs;
using DistributedChat.Application.Common.Abstractions;

namespace DistributedChat.Api.Http;

public sealed class CurrentUser(
    IHttpContextAccessor httpContextAccessor,
    HubCurrentUserContext hubCurrentUserContext
) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => hubCurrentUserContext.Current is not null
        || Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            if (hubCurrentUserContext.Current is { } hubCurrentUser)
            {
                return hubCurrentUser.UserId;
            }

            var value = Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public string? Username => hubCurrentUserContext.Current?.Username
        ?? Principal?.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
        ?? Principal?.FindFirstValue(ClaimTypes.Name);

    public string? Email => hubCurrentUserContext.Current?.Email
        ?? Principal?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? Principal?.FindFirstValue(ClaimTypes.Email);
}
