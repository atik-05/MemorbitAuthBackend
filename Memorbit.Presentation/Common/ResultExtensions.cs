using Memorbit.Application.Authentication.Common;
using Memorbit.Application.Authentication.Models;
using Memorbit.Presentation.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Memorbit.Presentation.Common
{
    public static class ResultExtensions
    {
        public static IActionResult ToAuthActionResult(
            this Result<IssuedAuthResult> result,
            HttpResponse response,
            RefreshTokenCookieService cookieService)
        {
            if (!result.IsSuccess)
            {
                return ToProblemResultInternal(result.Error!);
            }

            cookieService.Set(
                response,
                result.Value!.RefreshTokenPlain,
                result.Value.Response.RefreshTokenExpiresAtUtc);

            return new OkObjectResult(result.Value.Response);
        }

        public static IActionResult ToActionResult<T>(this Result<T> result)
        {
            return result.IsSuccess
                ? new OkObjectResult(result.Value)
                : ToProblemResultInternal(result.Error!);
        }

        public static IActionResult ToActionResult(this Result result)
        {
            return result.IsSuccess
                ? new NoContentResult()
                : ToProblemResultInternal(result.Error!);
        }

        public static IActionResult ToProblemResult(this AuthError error)
        {
            return ToProblemResultInternal(error);
        }

        private static IActionResult ToProblemResultInternal(AuthError error)
        {
            var statusCode = ToStatusCode(error.Type);

            var problem = new ProblemDetails
            {
                Title = error.Code,
                Detail = error.Message,
                Status = statusCode
            };

            return new ObjectResult(problem) { StatusCode = statusCode };
        }

        private static int ToStatusCode(AuthErrorType type) => type switch
        {
            AuthErrorType.Validation => StatusCodes.Status400BadRequest,
            AuthErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            AuthErrorType.Forbidden => StatusCodes.Status403Forbidden,
            AuthErrorType.NotFound => StatusCodes.Status404NotFound,
            AuthErrorType.Conflict => StatusCodes.Status409Conflict,
            AuthErrorType.Locked => StatusCodes.Status423Locked,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
