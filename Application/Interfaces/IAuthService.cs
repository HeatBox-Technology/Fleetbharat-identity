
using System.Threading.Tasks;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RefreshAsync(RefreshTokenRequest request);
    Task RegisterAsync(RegisterRequest req);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(string email, string token, string newPassword);
}
