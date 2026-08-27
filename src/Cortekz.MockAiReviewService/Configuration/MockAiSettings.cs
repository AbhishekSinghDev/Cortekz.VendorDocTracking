namespace Cortekz.MockAiReviewService.Configuration;

public class MockAiSettings
{
    public int QueuedDurationSeconds { get; set; } = 2;
    public int ProcessingDurationSeconds { get; set; } = 5;
    public double FailureRate { get; set; } = 0.2;
}
