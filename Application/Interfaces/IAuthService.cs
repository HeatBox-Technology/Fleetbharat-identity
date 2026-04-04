
using System.Threading.Tasks;

public interface IAuthService
{
    Task<LoginWithAccessResponse> LoginAsync(LoginRequest req);
    Task<LoginResponse> RefreshAsync(RefreshTokenRequest request);
    Task RegisterAsync(RegisterRequest req);
    Task ForgotPasswordAsync(string email);
    Task<LoginResponse> ResetPasswordAsync(ResetPasswordRequest req);
    Task<LoginWithAccessResponse> Verify2FAAsync(Verify2FARequest req);
    Task<LoginWithAccessResponse> VerifyLoginOtpAsync(VerifyLoginOtpRequest req);
    Task<LoginWithAccessResponse> RequestLoginOtpAsync(RequestLoginOtpRequest req);

}
