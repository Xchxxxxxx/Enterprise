using EfCore.Enterprise.Application.DTOs.Auth;
using EfCore.Enterprise.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EfCore.Enterprise.Presentation.Controllers;

[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();
            var result = await _authService.LoginAsync(request, ip, userAgent);
            return Success(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Fail(ex.Message);
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request);
            return Success(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Fail(ex.Message, 401);
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var userId = GetCurrentUserId();
        await _authService.LogoutAsync(userId, request.RefreshToken);
        return Success();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _authService.ChangePasswordAsync(userId, request);
            return Success("密码修改成功");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Fail(ex.Message);
        }
    }

    private long GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null ? long.Parse(claim.Value) : 0;
    }
}

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}