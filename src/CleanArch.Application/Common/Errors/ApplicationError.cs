namespace CleanArch.Application.Common.Errors;

public sealed record ApplicationError(string Code, string Message, ErrorType Type)
{
    public static ApplicationError Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static ApplicationError NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static ApplicationError Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static ApplicationError Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
    public static ApplicationError Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
