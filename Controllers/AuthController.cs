
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;


[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;
    public AuthController(IAuthService service) => _service = service;

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var result = await _service.LoginAsync(req);
        return Ok(ApiResponse<object>.Ok(result, result.Token.Message, 200));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest req)
        => Ok(await _service.RefreshAsync(req));

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] RegisterRequest req)
    {
        await _service.RegisterAsync(req);
        return Ok(ApiResponse<string>.Ok("User registered successfully", "Success", 200));
    }
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        await _service.ForgotPasswordAsync(req.Email);
        return Ok(ApiResponse<string>.Ok(
            "If your email exists, reset password link has been sent.",
            "Success",
            200
        ));
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var loginResponse = await _service.ResetPasswordAsync(req);

        return Ok(ApiResponse<LoginResponse>.Ok(
            loginResponse,
            "Password reset successfully",
            200
        ));
    }
    [HttpPost("verify-2fa")]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FARequest req)
    {
        var result = await _service.Verify2FAAsync(req);
        return Ok(ApiResponse<object>.Ok(result, result.Token.Message, 200));
    }

    [HttpPost("request-login-otp")]
    public async Task<IActionResult> RequestLoginOtp([FromBody] RequestLoginOtpRequest req)
    {
        var result = await _service.RequestLoginOtpAsync(req);
        return Ok(ApiResponse<object>.Ok(result, result.Token.Message, 200));
    }

    [HttpPost("verify-login-otp")]
    public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpRequest req)
    {
        var result = await _service.VerifyLoginOtpAsync(req);
        return Ok(ApiResponse<object>.Ok(result, result.Token.Message, 200));
    }



}
