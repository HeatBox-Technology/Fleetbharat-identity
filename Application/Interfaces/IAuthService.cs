
using System.Threading.Tasks;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RefreshAsync(RefreshTokenRequest request);
    Task RegisterAsync(RegisterRequest req);
}
