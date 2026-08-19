using CleanArch.Application.Common.Errors;
using LanguageExt;

namespace CleanArch.Api.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Either<ApplicationError, T> result) =>
        result.Match<IResult>(Right: value => Results.Ok(value), Left: error => error.ToProblem());

    private static IResult ToProblem(this ApplicationError error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        var title = error.Type switch
        {
            ErrorType.Validation => "Validation error",
            ErrorType.NotFound => "Resource not found",
            ErrorType.Conflict => "Conflict",
            ErrorType.Forbidden => "Forbidden",
            _ => "Application error"
        };

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: error.Message,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
