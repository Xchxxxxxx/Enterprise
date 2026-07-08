using EfCore.Enterprise.Shared.Exceptions;
using EfCore.Enterprise.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace EfCore.Enterprise.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult Success<T>(T data)
    {
        return Ok(ApiResult<T>.Success(data));
    }

    protected IActionResult Success()
    {
        return Ok(ApiResult.Success());
    }

    protected IActionResult Fail(string message, int code = (int)HttpStatusCode.BadRequest)
    {
        return Ok(ApiResult.Fail(message, code));
    }

    protected IActionResult Error(string message, int code = (int)HttpStatusCode.InternalServerError)
    {
        return Ok(ApiResult.Error(message, code));
    }
}