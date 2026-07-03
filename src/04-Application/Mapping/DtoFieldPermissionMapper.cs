using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Application.Mapping;

[AttributeUsage(AttributeTargets.Property)]
public class SensitiveFieldAttribute : Attribute
{
    public string MaskPattern { get; }
    public int ShowPrefixLength { get; }
    public int ShowSuffixLength { get; }

    public SensitiveFieldAttribute(string maskPattern = "*", int showPrefixLength = 0, int showSuffixLength = 0)
    {
        MaskPattern = maskPattern;
        ShowPrefixLength = showPrefixLength;
        ShowSuffixLength = showSuffixLength;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class FieldPermissionAttribute : Attribute
{
    public string PermissionCode { get; }
    public FieldPermissionAttribute(string permissionCode)
    {
        PermissionCode = permissionCode;
    }
}

public class DtoFieldPermissionMapper
{
    private readonly ILogger<DtoFieldPermissionMapper> _logger;

    public DtoFieldPermissionMapper(ILogger<DtoFieldPermissionMapper> logger)
    {
        _logger = logger;
    }

    public TDto ApplyFieldPermissions<TDto>(TDto dto, IEnumerable<string> userPermissions, IEnumerable<string> userFieldPermissions)
    {
        var properties = typeof(TDto).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var fieldPermission = prop.GetCustomAttribute<FieldPermissionAttribute>();
            if (fieldPermission != null && !userPermissions.Contains(fieldPermission.PermissionCode))
            {
                prop.SetValue(dto, null);
                continue;
            }

            var sensitiveAttr = prop.GetCustomAttribute<SensitiveFieldAttribute>();
            if (sensitiveAttr != null)
            {
                var value = prop.GetValue(dto)?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    prop.SetValue(dto, MaskValue(value, sensitiveAttr));
                }
            }
        }

        return dto;
    }

    public IEnumerable<TDto> ApplyFieldPermissions<TDto>(IEnumerable<TDto> dtos, IEnumerable<string> userPermissions, IEnumerable<string> userFieldPermissions)
    {
        return dtos.Select(dto => ApplyFieldPermissions(dto, userPermissions, userFieldPermissions));
    }

    private static string MaskValue(string value, SensitiveFieldAttribute attribute)
    {
        if (value.Length <= attribute.ShowPrefixLength + attribute.ShowSuffixLength)
        {
            return new string(attribute.MaskPattern[0], value.Length);
        }

        var prefix = value[..attribute.ShowPrefixLength];
        var suffix = value[^attribute.ShowSuffixLength..];
        var maskLength = value.Length - attribute.ShowPrefixLength - attribute.ShowSuffixLength;
        return prefix + new string(attribute.MaskPattern[0], maskLength) + suffix;
    }
}