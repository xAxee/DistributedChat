using DistributedChat.Application.Common.Dto;
using DistributedChat.Application.Common.Results;

namespace DistributedChat.Application.Authentication;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);

    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
}
