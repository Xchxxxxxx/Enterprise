using AutoMapper;
using EfCore.Enterprise.Application.DTOs.User;
using EfCore.Enterprise.Application.Mapping;
using EfCore.Enterprise.Domain.Entities.Identity;
using EfCore.Enterprise.Domain.Entities.Permission;

namespace EfCore.Enterprise.Application.Mapping.Profiles;

public class UserProfile : BaseProfile
{
    protected override void Configure()
    {
        CreateMap<SysUser, UserDto>();

        CreateMap<CreateUserRequest, SysUser>()
            .ConstructUsing(src => new SysUser(src.Username, string.Empty, string.Empty, src.Nickname))
            .ForMember(d => d.PasswordHash, o => o.Ignore())
            .ForMember(d => d.PasswordSalt, o => o.Ignore())
            .ForMember(d => d.SecurityStamp, o => o.Ignore());

        CreateMap<UpdateUserRequest, SysUser>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Username, o => o.Ignore())
            .ForMember(d => d.PasswordHash, o => o.Ignore())
            .ForMember(d => d.PasswordSalt, o => o.Ignore())
            .ForMember(d => d.SecurityStamp, o => o.Ignore())
            .ForMember(d => d.IsEnabled, o => o.Ignore())
            .ForMember(d => d.EmailConfirmed, o => o.Ignore())
            .ForMember(d => d.PhoneConfirmed, o => o.Ignore())
            .ForMember(d => d.TwoFactorEnabled, o => o.Ignore())
            .ForMember(d => d.LastLoginTime, o => o.Ignore())
            .ForMember(d => d.LastLoginIp, o => o.Ignore())
            .ForMember(d => d.LoginFailedCount, o => o.Ignore())
            .ForMember(d => d.LockoutEnd, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));

        CreateMap<LoginLog, LoginLogDto>();
    }
}