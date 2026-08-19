using CleanArch.Application.Common.Errors;
using LanguageExt;

namespace CleanArch.Api.Extensions;

public static class ResultExtensions
{
    public static async Task<IResult> ToResult<T>(
        this Task<Either<ApplicationError, T>> resultTask)
    {
        var result = await resultTask;

        return result.Match<IResult>(
            Right: value => Results.Ok(value),
            Left: error => error.ToProblem());
    }

    public static IResult ToProblem(this ApplicationError error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            statusCode: statusCode,
            title: error.Type switch
            {
                ErrorType.Validation => "Validation error",
                ErrorType.NotFound => "Resource not found",
                ErrorType.Conflict => "Conflict",
                ErrorType.Forbidden => "Forbidden",
                _ => "Application error"
            },
            detail: error.Message,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code
            });
    }
}
