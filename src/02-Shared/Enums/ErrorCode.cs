namespace EfCore.Enterprise.Shared.Enums;

public enum ErrorCode
{
    Success = 0,
    UnknownError = 500,
    SystemError = 500,
    ValidationError = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    BusinessError = 1000,
    DataNotFound = 20001,
    DataConflict = 20002,
    DataLocked = 20003,
    DataArchived = 20004,
    ConcurrentConflict = 20005,
    PermissionDenied = 30001,
    TenantExpired = 40001,
    DuplicateRequest = 10009,
    IdempotencyConflict = 10010,
    RateLimitExceeded = 50001,
    CircuitBreakerOpen = 50002
}