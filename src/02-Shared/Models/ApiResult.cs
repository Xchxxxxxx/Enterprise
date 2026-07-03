using EfCore.Enterprise.Shared.Enums;

namespace EfCore.Enterprise.Shared.Models;

/// <summary>
/// 统一API响应模型，所有接口返回此格式
/// </summary>
/// <remarks>
/// Code=0表示成功，非0表示业务或系统错误。
/// </remarks>
public class ApiResult
{
    /// <summary>状态码，0=成功</summary>
    public int Code { get; set; }

    /// <summary>提示消息</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>是否成功</summary>
    public bool IsSuccess => Code == 0;

    /// <summary>
    /// 返回成功响应（无数据）
    /// </summary>
    public static ApiResult Success()
    {
        return new ApiResult { Code = 0, Message = "成功" };
    }

    /// <summary>
    /// 返回成功响应，自定义消息
    /// </summary>
    public static ApiResult Success(string message)
    {
        return new ApiResult { Code = 0, Message = message };
    }

    /// <summary>
    /// 返回业务失败响应
    /// </summary>
    /// <param name="message">错误描述</param>
    /// <param name="code">业务错误码，默认400</param>
    public static ApiResult Fail(string message, int code = 400)
    {
        return new ApiResult { Code = code, Message = message };
    }

    /// <summary>
    /// 返回业务失败响应，使用枚举错误码
    /// </summary>
    public static ApiResult Fail(ErrorCode errorCode, string message)
    {
        return new ApiResult { Code = (int)errorCode, Message = message };
    }

    /// <summary>
    /// 返回系统错误响应
    /// </summary>
    /// <param name="message">错误描述</param>
    /// <param name="code">错误码，默认500</param>
    public static ApiResult Error(string message, int code = 500)
    {
        return new ApiResult { Code = code, Message = message };
    }
}

/// <summary>
/// 带数据的统一API响应模型
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ApiResult<T> : ApiResult
{
    /// <summary>响应数据</summary>
    public T? Data { get; set; }

    /// <summary>
    /// 返回带数据的成功响应
    /// </summary>
    /// <param name="data">响应数据</param>
    /// <param name="message">提示消息</param>
    public static ApiResult<T> Success(T data, string message = "操作成功")
    {
        return new ApiResult<T> { Code = 0, Message = message, Data = data };
    }

    /// <summary>
    /// 返回带数据类型的业务失败响应
    /// </summary>
    public static new ApiResult<T> Fail(string message, int code = 400)
    {
        return new ApiResult<T> { Code = code, Message = message };
    }

    /// <summary>
    /// 返回带数据类型的业务失败响应，使用枚举错误码
    /// </summary>
    public static new ApiResult<T> Fail(ErrorCode errorCode, string message)
    {
        return new ApiResult<T> { Code = (int)errorCode, Message = message };
    }

    /// <summary>
    /// 返回带数据类型的系统错误响应
    /// </summary>
    public static new ApiResult<T> Error(string message, int code = 500)
    {
        return new ApiResult<T> { Code = code, Message = message };
    }
}