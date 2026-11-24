namespace DigitalCertHelper.Domain;

/// <summary>
/// An anticipated failure. The user message may be displayed, so it must never contain technical
/// details; put those in the inner exception, which is only logged.
/// </summary>
public sealed class AppException(ErrorCode code, string? userMessage = null, Exception? innerException = null)
    : Exception($"Operation failed: {code}", innerException)
{
    public ErrorCode Code { get; } = code;

    public string? UserMessage { get; } = userMessage;
}
