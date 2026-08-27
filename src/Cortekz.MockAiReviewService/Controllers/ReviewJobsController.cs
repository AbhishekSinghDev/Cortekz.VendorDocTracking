using Cortekz.MockAiReviewService.Dtos;
using Cortekz.MockAiReviewService.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cortekz.MockAiReviewService.Controllers;

[ApiController]
[Route("ai/review-jobs")]
public class ReviewJobsController : ControllerBase
{
    private readonly ReviewJobService _reviewJobService;

    public ReviewJobsController(ReviewJobService reviewJobService)
    {
        _reviewJobService = reviewJobService;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateReviewJobRequest request)
    {
        var response = _reviewJobService.CreateJob(request);
        return Accepted(response);
    }

    [HttpGet("{jobId}")]
    public IActionResult Get(string jobId)
    {
        var response = _reviewJobService.GetJobStatus(jobId);
        return response is null
            ? NotFound(new { message = $"Review job '{jobId}' does not exist." })
            : Ok(response);
    }
}
