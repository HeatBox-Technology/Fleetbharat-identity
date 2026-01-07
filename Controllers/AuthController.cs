
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;


[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;
    public AuthController(IAuthService service) => _service = service;

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
        => Ok(await _service.LoginAsync(req));

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest req)
        => Ok(await _service.RefreshAsync(req));

    [HttpPost("signup")]
    public async Task<IActionResult> Signup(RegisterRequest req)
    {
        await _service.RegisterAsync(req);
        return Ok("User registered successfully");
    }


}
