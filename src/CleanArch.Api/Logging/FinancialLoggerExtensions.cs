namespace CleanArch.Api.Logging;

internal static partial class FinancialLoggerExtensions
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Financial operation {Operation} referenced account {AccountReference}")]
    internal static partial void FinancialOperation(
        this ILogger logger,
        string operation,
        [FinancialData] string accountReference);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Warning,
        Message = "Authentication material was rejected for subject {Subject} using token {Token}")]
    internal static partial void AuthenticationMaterialRejected(
        this ILogger logger,
        [PersonalData] string subject,
        [SecretData] string token);
}
