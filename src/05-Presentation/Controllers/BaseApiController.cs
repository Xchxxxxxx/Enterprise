using EfCore.Enterprise.Shared.Exceptions;
using EfCore.Enterprise.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace EfCore.Enterprise.Presentation.Controllers;

/// <summary>
/// API控制器基类，提供统一的 Success/Fail/Error 响应方法，确保所有API返回格式一致
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// 返回成功响应，HTTP 200
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="data">响应数据</param>
    protected IActionResult Success<T>(T data)
    {
        return Ok(ApiResult<T>.Success(data));
    }

    /// <summary>
    /// 返回无数据的成功响应，HTTP 200
    /// </summary>
    protected IActionResult Success()
    {
        return Ok(ApiResult.Success());
    }

    /// <summary>
    /// 返回业务失败响应，HTTP 200（业务错误通过code区分）
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="code">业务错误码，默认400</param>
    protected IActionResult Fail(string message, int code = (int)HttpStatusCode.BadRequest)
    {
        return Ok(ApiResult.Fail(message, code));
    }

    /// <summary>
    /// 返回系统错误响应，HTTP 200（系统错误通过code区分）
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="code">错误码，默认500</param>
    protected IActionResult Error(string message, int code = (int)HttpStatusCode.InternalServerError)
    {
        return Ok(ApiResult.Error(message, code));
    }
}