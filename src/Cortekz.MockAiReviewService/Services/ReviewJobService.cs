using Cortekz.MockAiReviewService.Configuration;
using Cortekz.MockAiReviewService.Dtos;
using Cortekz.MockAiReviewService.Models;
using Microsoft.Extensions.Options;

namespace Cortekz.MockAiReviewService.Services;

public class ReviewJobService
{
    private static readonly string[] VerdictPool = { "Clean", "IssuesFound" };

    private static readonly string[] IssuePool =
    {
        "Missing signature",
        "Illegible scan",
        "Revision date missing",
        "Certificate expired",
        "Document does not match PO scope"
    };

    private readonly ReviewJobStore _store;
    private readonly MockAiSettings _settings;

    public ReviewJobService(ReviewJobStore store, IOptions<MockAiSettings> settings)
    {
        _store = store;
        _settings = settings.Value;
    }

    public ReviewJobAcceptedResponse CreateJob(CreateReviewJobRequest request)
    {
        var job = new ReviewJob
        {
            JobId = Guid.NewGuid().ToString("N"),
            SubmissionId = request.SubmissionId,
            DocumentType = request.DocumentType,
            DocumentRef = request.DocumentRef,
            Status = ReviewJobStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            WillFail = Random.Shared.NextDouble() < _settings.FailureRate
        };

        _store.Add(job);

        return new ReviewJobAcceptedResponse { JobId = job.JobId, Status = job.Status.ToString() };
    }

    public ReviewJobStatusResponse? GetJobStatus(string jobId)
    {
        var job = _store.Get(jobId);
        if (job is null)
        {
            return null;
        }

        Advance(job);

        return new ReviewJobStatusResponse
        {
            JobId = job.JobId,
            Status = job.Status.ToString(),
            Verdict = job.Verdict,
            Confidence = job.Confidence,
            FlaggedIssues = job.FlaggedIssues
        };
    }

    private void Advance(ReviewJob job)
    {
        if (job.Status is ReviewJobStatus.Completed or ReviewJobStatus.Failed)
        {
            return;
        }

        var elapsed = DateTime.UtcNow - job.CreatedAt;
        var queuedDuration = TimeSpan.FromSeconds(_settings.QueuedDurationSeconds);
        var totalDuration = queuedDuration + TimeSpan.FromSeconds(_settings.ProcessingDurationSeconds);

        if (elapsed < queuedDuration)
        {
            return;
        }

        if (elapsed < totalDuration)
        {
            job.Status = ReviewJobStatus.Processing;
            return;
        }

        job.CompletedAt = DateTime.UtcNow;

        if (job.WillFail)
        {
            job.Status = ReviewJobStatus.Failed;
            return;
        }

        job.Status = ReviewJobStatus.Completed;
        job.Verdict = VerdictPool[Random.Shared.Next(VerdictPool.Length)];
        job.Confidence = Math.Round(0.5 + Random.Shared.NextDouble() * 0.49, 2);
        job.FlaggedIssues = job.Verdict == "IssuesFound"
            ? Enumerable.Range(0, Random.Shared.Next(1, 3))
                .Select(_ => IssuePool[Random.Shared.Next(IssuePool.Length)])
                .Distinct()
                .ToList()
            : new List<string>();
    }
}
