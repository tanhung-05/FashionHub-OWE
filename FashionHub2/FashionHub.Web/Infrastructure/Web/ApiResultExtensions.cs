using FashionHub.Web.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Infrastructure.Web;

public static class ApiResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(
        this ControllerBase controller,
        ServiceResult<T> result)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(result.Value);
        }

        var error = result.Error!;
        var statusCode = error.Type switch
        {
            ServiceErrorType.Validation => StatusCodes.Status400BadRequest,
            ServiceErrorType.NotFound => StatusCodes.Status404NotFound,
            ServiceErrorType.Conflict => StatusCodes.Status409Conflict,
            ServiceErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ServiceErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Type = $"https://fashionhub.local/errors/{error.Code}",
            Title = GetTitle(error.Type),
            Status = statusCode,
            Detail = error.Message,
            Instance = controller.HttpContext.Request.Path
        };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = controller.HttpContext.TraceIdentifier;

        return new ObjectResult(problem) { StatusCode = statusCode };
    }

    private static string GetTitle(ServiceErrorType errorType) => errorType switch
    {
        ServiceErrorType.Validation => "Request validation failed",
        ServiceErrorType.NotFound => "Resource not found",
        ServiceErrorType.Conflict => "Business rule conflict",
        ServiceErrorType.Unauthorized => "Authentication required",
        ServiceErrorType.Forbidden => "Access denied",
        _ => "Request failed"
    };
}
