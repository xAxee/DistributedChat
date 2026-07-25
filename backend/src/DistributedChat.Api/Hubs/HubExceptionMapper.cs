using DistributedChat.Application.Common.Results;
using Microsoft.AspNetCore.SignalR;

namespace DistributedChat.Api.Hubs;

public static class HubExceptionMapper
{
    public static HubException ToHubException(ApplicationError error) =>
        new($"{error.Code}: {error.Message}");
}
