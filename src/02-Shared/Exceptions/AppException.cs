using EfCore.Enterprise.Shared.Enums;

namespace EfCore.Enterprise.Shared.Exceptions;

public class AppException : Exception
{
    public int Code { get; set; }
    public ErrorCode ErrorCode { get; set; }

    public AppException(ErrorCode errorCode, string message = "")
        : base(string.IsNullOrEmpty(message) ? errorCode.ToString() : message)
    {
        ErrorCode = errorCode;
        Code = (int)errorCode;
    }

    public AppException(int code, string message) : base(message)
    {
        Code = code;
        ErrorCode = ErrorCode.UnknownError;
    }
}

public class BusinessException : AppException
{
    public BusinessException(string message)
        : base(ErrorCode.BusinessError, message) { }

    public BusinessException(ErrorCode errorCode, string message)
        : base(errorCode, message) { }
}

public class ValidationException : AppException
{
    public List<string> Errors { get; set; } = new();

    public ValidationException(string message)
        : base(ErrorCode.ValidationError, message) { }

    public ValidationException(List<string> errors)
        : base(ErrorCode.ValidationError, string.Join("; ", errors))
    {
        Errors = errors;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message = "资源不存在")
        : base(ErrorCode.NotFound, message) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "未授权访问")
        : base(ErrorCode.Unauthorized, message) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "禁止访问")
        : base(ErrorCode.Forbidden, message) { }
}

public class DuplicateRequestException : AppException
{
    public DuplicateRequestException(string message = "重复请求")
        : base(ErrorCode.DuplicateRequest, message) { }
}

public class DataLockedException : AppException
{
    public DataLockedException(string message = "数据已被锁定")
        : base(ErrorCode.DataLocked, message) { }
}

public class DataArchivedException : AppException
{
    public DataArchivedException(string message = "数据已归档封存")
        : base(ErrorCode.DataArchived, message) { }
}

public class ConcurrentConflictException : AppException
{
    public ConcurrentConflictException(string message = "并发冲突")
        : base(ErrorCode.ConcurrentConflict, message) { }
}