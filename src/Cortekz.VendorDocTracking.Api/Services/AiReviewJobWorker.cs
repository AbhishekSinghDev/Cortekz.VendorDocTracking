using Cortekz.VendorDocTracking.Api.Data;
using Cortekz.VendorDocTracking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cortekz.VendorDocTracking.Api.Services;

public class AiReviewJobWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AiReviewWorkerSettings _settings;
    private readonly ILogger<AiReviewJobWorker> _logger;

    public AiReviewJobWorker(IServiceScopeFactory scopeFactory, IOptions<AiReviewWorkerSettings> settings, ILogger<AiReviewJobWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueJobsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI review job worker iteration failed unexpectedly.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.PollIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessDueJobsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aiReviewClient = scope.ServiceProvider.GetRequiredService<IAiReviewClient>();
        var submissionRepository = scope.ServiceProvider.GetRequiredService<SubmissionRepository>();

        var now = DateTime.UtcNow;
        var dueJobs = await db.AiReviewJobs
            .Where(j => (j.Status == AiReviewJobStatus.Pending
                      || j.Status == AiReviewJobStatus.Submitted
                      || j.Status == AiReviewJobStatus.Processing)
                     && (j.NextAttemptAt == null || j.NextAttemptAt <= now))
            .ToListAsync(stoppingToken);

        foreach (var job in dueJobs)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ProcessJobAsync(job, db, aiReviewClient, submissionRepository, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process AI review job {JobId}.", job.Id);
            }
        }
    }

    private async Task ProcessJobAsync(
        AiReviewJob job,
        AppDbContext db,
        IAiReviewClient aiReviewClient,
        SubmissionRepository submissionRepository,
        CancellationToken stoppingToken)
    {
        try
        {
            if (job.Status == AiReviewJobStatus.Pending)
            {
                await SubmitJobAsync(job, db, aiReviewClient, submissionRepository, stoppingToken);
                return;
            }

            if (job.ExternalJobId is null)
            {
                throw new PermanentAiReviewException("Job is awaiting a poll but has no external job id.");
            }

            var pollResult = await aiReviewClient.PollAsync(job.ExternalJobId, stoppingToken);

            switch (pollResult.Status)
            {
                case "Processing" when job.Status != AiReviewJobStatus.Processing:
                    job.Status = AiReviewJobStatus.Processing;
                    job.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                    break;
                case "Completed":
                case "Failed":
                    await CompleteJobAsync(job, pollResult, db, submissionRepository, stoppingToken);
                    break;
            }
        }
        catch (PermanentAiReviewException ex)
        {
            await AbandonAsync(job, db, submissionRepository, ex.Message, stoppingToken);
        }
        catch (TransientAiReviewException ex)
        {
            await ScheduleRetryAsync(job, db, submissionRepository, ex.Message, stoppingToken);
        }
    }

    private static async Task SubmitJobAsync(
        AiReviewJob job,
        AppDbContext db,
        IAiReviewClient aiReviewClient,
        SubmissionRepository submissionRepository,
        CancellationToken stoppingToken)
    {
        var submission = await submissionRepository.GetByIdAsync(job.SubmissionId)
            ?? throw new PermanentAiReviewException($"Submission '{job.SubmissionId}' no longer exists in Mongo.");

        var documentRef = submission.Files.Count > 0 ? submission.Files[0].StorageKey : job.SubmissionId;

        var submitResult = await aiReviewClient.SubmitAsync(job.SubmissionId, submission.DocumentType, documentRef, stoppingToken);

        job.ExternalJobId = submitResult.ExternalJobId;
        job.Status = AiReviewJobStatus.Submitted;
        job.UpdatedAt = DateTime.UtcNow;
        job.NextAttemptAt = DateTime.UtcNow;
        await db.SaveChangesAsync(stoppingToken);
    }

    private static async Task CompleteJobAsync(
        AiReviewJob job,
        AiReviewPollResult pollResult,
        AppDbContext db,
        SubmissionRepository submissionRepository,
        CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;

        await submissionRepository.UpdateAiReviewAsync(job.SubmissionId, new AiReviewInfo
        {
            JobId = job.ExternalJobId,
            Status = pollResult.Status,
            Verdict = pollResult.Verdict,
            Confidence = pollResult.Confidence,
            FlaggedIssues = pollResult.FlaggedIssues,
            CompletedAt = now
        });

        job.Status = pollResult.Status == "Completed" ? AiReviewJobStatus.Completed : AiReviewJobStatus.Failed;
        job.CompletedAt = now;
        job.UpdatedAt = now;
        await db.SaveChangesAsync(stoppingToken);
    }

    private async Task ScheduleRetryAsync(
        AiReviewJob job,
        AppDbContext db,
        SubmissionRepository submissionRepository,
        string error,
        CancellationToken stoppingToken)
    {
        job.AttemptCount += 1;
        job.LastError = error;
        job.UpdatedAt = DateTime.UtcNow;

        if (job.AttemptCount >= _settings.MaxAttempts)
        {
            await AbandonAsync(job, db, submissionRepository, error, stoppingToken);
            return;
        }

        var backoffSeconds = Math.Min(
            _settings.BackoffBaseSeconds * Math.Pow(2, job.AttemptCount - 1),
            _settings.BackoffMaxSeconds);

        job.NextAttemptAt = DateTime.UtcNow.AddSeconds(backoffSeconds);
        await db.SaveChangesAsync(stoppingToken);
    }

    private async Task AbandonAsync(
        AiReviewJob job,
        AppDbContext db,
        SubmissionRepository submissionRepository,
        string error,
        CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;

        job.Status = AiReviewJobStatus.Abandoned;
        job.LastError = error;
        job.CompletedAt = now;
        job.UpdatedAt = now;
        await db.SaveChangesAsync(stoppingToken);

        await submissionRepository.UpdateAiReviewAsync(job.SubmissionId, new AiReviewInfo
        {
            JobId = job.ExternalJobId,
            Status = "Failed",
            CompletedAt = now
        });

        _logger.LogWarning("AI review job {JobId} for submission {SubmissionId} abandoned after {AttemptCount} attempts: {Error}",
            job.Id, job.SubmissionId, job.AttemptCount, error);
    }
}
