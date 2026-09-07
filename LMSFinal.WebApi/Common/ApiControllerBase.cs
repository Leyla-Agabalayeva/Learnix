using LMSFinal.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace LMSFinal.WebApi.Common
{

   
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected IActionResult Success<T>(T data, string? message = null) =>
            Ok(ApiResponse<T>.Ok(data, message));

        protected IActionResult Created<T>(T data, string? message = null) =>
            StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Ok(data, message));

        protected IActionResult Fail(string message, int statusCode = StatusCodes.Status400BadRequest) =>
            StatusCode(statusCode, ApiResponse.Fail(message));
    }

}
