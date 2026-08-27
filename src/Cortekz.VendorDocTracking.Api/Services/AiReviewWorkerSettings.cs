namespace Cortekz.VendorDocTracking.Api.Services;

public class AiReviewWorkerSettings
{
    public int PollIntervalSeconds { get; set; } = 5;
    public int MaxAttempts { get; set; } = 3;
    public int BackoffBaseSeconds { get; set; } = 5;
    public int BackoffMaxSeconds { get; set; } = 60;
    public int RequestTimeoutSeconds { get; set; } = 10;
}
