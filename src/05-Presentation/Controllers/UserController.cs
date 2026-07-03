using EfCore.Enterprise.Application.Crud;
using EfCore.Enterprise.Application.DTOs.Auth;
using EfCore.Enterprise.Application.DTOs.User;
using EfCore.Enterprise.Application.Services;
using EfCore.Enterprise.Domain.Entities.Permission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EfCore.Enterprise.Presentation.Controllers;

[Route("api/[controller]")]
[Authorize]
public class UserController : CrudController<SysUser, UserDto, CreateUserRequest, UpdateUserRequest>
{
    private readonly UserService _userService;

    public UserController(UserService userService) : base(userService)
    {
        _userService = userService;
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _userService.ResetPasswordAsync(request);
            return Success("密码重置成功");
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    [HttpPost("assign-roles")]
    public async Task<IActionResult> AssignRoles([FromBody] AssignRolesRequest request)
    {
        try
        {
            await _userService.AssignRolesAsync(request);
            return Success("角色分配成功");
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteRequest request)
    {
        try
        {
            await _userService.BatchDeleteAsync(request.Ids);
            return Success("批量删除成功");
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    [HttpPost("batch-enable")]
    public async Task<IActionResult> BatchEnable([FromBody] BatchEnableRequest request)
    {
        try
        {
            await _userService.BatchEnableAsync(request.Ids, request.IsEnabled);
            return Success(request.IsEnabled ? "批量启用成功" : "批量禁用成功");
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    [HttpPost("{id}/unlock")]
    public async Task<IActionResult> UnlockUser(long id)
    {
        try
        {
            await _userService.UnlockUserAsync(id);
            return Success("解锁成功");
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    [HttpGet("login-logs")]
    public async Task<IActionResult> GetLoginLogs([FromQuery] LoginLogQueryRequest request)
    {
        var result = await _userService.GetLoginLogsAsync(request);
        return Success(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _userService.GetStatsAsync();
        return Success(result);
    }

    [HttpGet("check-username")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckUsername([FromQuery] string username, [FromQuery] long? excludeId)
    {
        var isUnique = await _userService.CheckUsernameUniqueAsync(username, excludeId);
        return Success(isUnique);
    }
}