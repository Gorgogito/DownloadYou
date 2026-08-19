namespace DownloadYou.Application.Abstractions;

public sealed record MediaVerificationResult(bool IsValid, TimeSpan? ActualDuration, string? Error)
{
    public static MediaVerificationResult Valid(TimeSpan duration) => new(true, duration, null);

    public static MediaVerificationResult Invalid(string error) => new(false, null, error);
}
