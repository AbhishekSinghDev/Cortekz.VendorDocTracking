using System.Collections.Concurrent;
using Cortekz.MockAiReviewService.Models;

namespace Cortekz.MockAiReviewService.Services;

public class ReviewJobStore
{
    private readonly ConcurrentDictionary<string, ReviewJob> _jobs = new();

    public void Add(ReviewJob job)
    {
        _jobs[job.JobId] = job;
    }

    public ReviewJob? Get(string jobId) => _jobs.GetValueOrDefault(jobId);
}
