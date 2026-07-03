using AutoMapper;
using EfCore.Enterprise.Application.Crud;
using EfCore.Enterprise.Application.DTOs.Auth;
using EfCore.Enterprise.Application.DTOs.User;
using EfCore.Enterprise.Domain.Entities.Identity;
using EfCore.Enterprise.Domain.Entities.Permission;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Domain.Services;
using EfCore.Enterprise.Shared.DependencyInjection;
using EfCore.Enterprise.Shared.Exceptions;
using EfCore.Enterprise.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Application.Services;

[Injectable(ServiceLifetime.Scoped)]
public class UserService : CrudAppService<SysUser, UserDto, CreateUserRequest, UpdateUserRequest>
{
    private readonly ISuperRepository<SysUserRole> _userRoleRepo;
    private readonly ISuperRepository<SysRole> _roleRepo;
    private readonly ISuperRepository<LoginLog> _loginLogRepo;
    private readonly IUserDomainService _domainService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ISuperRepository<SysUser> userRepo,
        ISuperRepository<SysUserRole> userRoleRepo,
        ISuperRepository<SysRole> roleRepo,
        ISuperRepository<LoginLog> loginLogRepo,
        IUserDomainService domainService,
        IMapper mapper,
        ILogger<UserService> logger)
        : base(userRepo, mapper)
    {
        _userRoleRepo = userRoleRepo;
        _roleRepo = roleRepo;
        _loginLogRepo = loginLogRepo;
        _domainService = domainService;
        _logger = logger;
    }

    public override async Task<PagedResult<UserDto>> GetPageAsync(PagedRequest request)
    {
        var query = _repository.Query();
        var total = await query.CountAsync();
        var list = await query
            .OrderByDescending(u => u.CreatedTime)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var userIds = list.Select(u => u.Id).ToList();

        var userRoleMaps = await _userRoleRepo.Query()
            .Where(ur => userIds.Contains(ur.UserId))
            .ToListAsync();

        var allRoleIds = userRoleMaps.Select(ur => ur.RoleId).Distinct().ToList();
        var roles = await _roleRepo.Query()
            .Where(r => allRoleIds.Contains(r.Id))
            .ToListAsync();
        var roleDict = roles.ToDictionary(r => r.Id, r => r.Name);

        var userDtos = new List<UserDto>();
        foreach (var user in list)
        {
            var dto = _mapper.Map<UserDto>(user);
            var roleIds = userRoleMaps.Where(ur => ur.UserId == user.Id).Select(ur => ur.RoleId).ToList();
            dto.Roles = roleIds.Select(rid => roleDict.GetValueOrDefault(rid, "")).ToList();
            dto.RoleIds = roleIds;
            userDtos.Add(dto);
        }

        return new PagedResult<UserDto>(userDtos, total, request.PageIndex, request.PageSize);
    }

    public override async Task<UserDto?> GetByIdAsync(long id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null) return null;

        var dto = _mapper.Map<UserDto>(user);
        var roleIds = await _userRoleRepo.Query()
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId)
            .ToListAsync();
        var roles = await _roleRepo.Query()
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToListAsync();
        dto.Roles = roles;
        dto.RoleIds = roleIds;
        return dto;
    }

    public override async Task<UserDto> CreateAsync(CreateUserRequest dto)
    {
        var (hash, salt) = _domainService.HashPassword(dto.Password);
        var user = new SysUser(dto.Username, hash, salt, dto.Nickname);
        user.UpdateProfile(dto.Nickname, dto.Email, dto.Phone, dto.Avatar);

        await _domainService.CreateUserAsync(user, dto.RoleIds);

        _logger.LogInformation("创建用户 {Username}，ID={UserId}", user.Username, user.Id);
        return await GetByIdAsync(user.Id) ?? throw new InvalidOperationException("创建后获取用户失败");
    }

    public override async Task<UserDto> UpdateAsync(long id, UpdateUserRequest dto)
    {
        var user = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException();

        user.UpdateProfile(dto.Nickname, dto.Email, dto.Phone, dto.Avatar);

        if (dto.RoleIds != null)
        {
            await _domainService.UpdateUserRolesAsync(user, dto.RoleIds);
        }
        else
        {
            user.RecordUpdated(0, new List<string>());
            await _repository.UpdateAsync(user);
            await _repository.SaveChangesAsync();
        }

        _logger.LogInformation("更新用户 ID={UserId}", id);
        return await GetByIdAsync(id) ?? throw new InvalidOperationException("更新后获取用户失败");
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _repository.GetByIdAsync(request.UserId)
            ?? throw new NotFoundException("用户不存在");

        var (hash, salt) = _domainService.HashPassword(request.NewPassword);
        user.ChangePassword(hash, salt);

        await _repository.UpdateAsync(user);
        await _repository.SaveChangesAsync();
        _logger.LogWarning("管理员重置用户 {UserId} 密码", request.UserId);
    }

    public async Task AssignRolesAsync(AssignRolesRequest request)
    {
        await _domainService.AssignRolesAsync(request.UserId, request.RoleIds);
        _logger.LogInformation("分配用户 {UserId} 角色: {RoleIds}", request.UserId, string.Join(",", request.RoleIds));
    }

    public async Task BatchDeleteAsync(List<long> ids)
    {
        foreach (var id in ids)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user != null)
            {
                user.RecordDeleted(0);
                await _repository.DeleteAsync(user);
            }
        }
        await _repository.SaveChangesAsync();
        _logger.LogWarning("批量删除用户: {UserIds}", string.Join(",", ids));
    }

    public async Task BatchEnableAsync(List<long> ids, bool isEnabled)
    {
        var users = await _repository.GetByIdsAsync(ids);
        foreach (var user in users)
        {
            if (isEnabled)
                user.Enable();
            else
                user.Disable();
            await _repository.UpdateAsync(user);
        }
        await _repository.SaveChangesAsync();
        _logger.LogInformation("批量{Action}用户: {UserIds}", isEnabled ? "启用" : "禁用", string.Join(",", ids));
    }

    public async Task UnlockUserAsync(long id)
    {
        var user = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException("用户不存在");
        user.Unlock();
        await _repository.UpdateAsync(user);
        await _repository.SaveChangesAsync();
    }

    public async Task<PagedResult<LoginLogDto>> GetLoginLogsAsync(LoginLogQueryRequest request)
    {
        var query = _loginLogRepo.Query();

        if (request.UserId.HasValue)
            query = query.Where(l => l.UserId == request.UserId.Value);
        if (!string.IsNullOrWhiteSpace(request.Username))
            query = query.Where(l => l.Username != null && l.Username.Contains(request.Username));
        if (request.IsSuccess.HasValue)
            query = query.Where(l => l.IsSuccess == request.IsSuccess.Value);
        if (request.StartTime.HasValue)
            query = query.Where(l => l.LoginTime >= request.StartTime.Value);
        if (request.EndTime.HasValue)
            query = query.Where(l => l.LoginTime <= request.EndTime.Value);

        var total = await query.CountAsync();
        var list = await query
            .OrderByDescending(l => l.LoginTime)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<LoginLogDto>(_mapper.Map<List<LoginLogDto>>(list), total, request.PageIndex, request.PageSize);
    }

    public async Task<UserStatsDto> GetStatsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var todayStart = now.Date;

        var totalUsers = await _repository.Query().CountAsync();
        var activeUsers = await _repository.Query().CountAsync(u => u.IsEnabled);
        var disabledUsers = await _repository.Query().CountAsync(u => !u.IsEnabled);
        var lockedUsers = await _repository.Query().CountAsync(u => u.LockoutEnd.HasValue && u.LockoutEnd.Value > now);
        var onlineToday = await _loginLogRepo.Query()
            .Where(l => l.IsSuccess && l.LoginTime >= todayStart)
            .Select(l => l.UserId)
            .Distinct()
            .CountAsync();
        var newToday = await _repository.Query()
            .Where(u => u.CreatedTime >= todayStart)
            .CountAsync();

        var sevenDaysAgo = now.AddDays(-7);
        var dailyLogs = await _loginLogRepo.Query()
            .Where(l => l.LoginTime >= sevenDaysAgo)
            .GroupBy(l => l.LoginTime.Date)
            .Select(g => new DailyStat
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                LoginCount = g.Count(),
                NewUserCount = 0
            })
            .ToListAsync();

        var dailyNewUsers = await _repository.Query()
            .Where(u => u.CreatedTime >= sevenDaysAgo)
            .GroupBy(u => u.CreatedTime.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var dl in dailyLogs)
        {
            var match = dailyNewUsers.FirstOrDefault(n => n.Date.ToString("yyyy-MM-dd") == dl.Date);
            if (match != null) dl.NewUserCount = match.Count;
        }

        return new UserStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            DisabledUsers = disabledUsers,
            LockedUsers = lockedUsers,
            OnlineToday = onlineToday,
            NewToday = newToday,
            DailyStats = dailyLogs
        };
    }

    public async Task<bool> CheckUsernameUniqueAsync(string username, long? excludeId = null)
    {
        var query = _repository.Query().Where(u => u.Username == username);
        if (excludeId.HasValue)
            query = query.Where(u => u.Id != excludeId.Value);
        return !await query.AnyAsync();
    }
}