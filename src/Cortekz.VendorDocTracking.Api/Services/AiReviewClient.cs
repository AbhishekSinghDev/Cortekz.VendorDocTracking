using System.Net.Http.Json;
using System.Text.Json;

namespace Cortekz.VendorDocTracking.Api.Services;

public class TransientAiReviewException : Exception
{
    public TransientAiReviewException(string message) : base(message) { }
    public TransientAiReviewException(string message, Exception inner) : base(message, inner) { }
}

public class PermanentAiReviewException : Exception
{
    public PermanentAiReviewException(string message) : base(message) { }
}

public class AiReviewSubmitResult
{
    public string ExternalJobId { get; init; } = string.Empty;
}

public class AiReviewPollResult
{
    public string Status { get; init; } = string.Empty;
    public string? Verdict { get; init; }
    public double? Confidence { get; init; }
    public List<string> FlaggedIssues { get; init; } = new();
}

public interface IAiReviewClient
{
    Task<AiReviewSubmitResult> SubmitAsync(string submissionId, string documentType, string documentRef, CancellationToken cancellationToken);
    Task<AiReviewPollResult> PollAsync(string externalJobId, CancellationToken cancellationToken);
}

public class AiReviewClient : IAiReviewClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public AiReviewClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AiReviewSubmitResult> SubmitAsync(string submissionId, string documentType, string documentRef, CancellationToken cancellationToken)
    {
        var response = await SendAsync(
            () => _httpClient.PostAsJsonAsync("ai/review-jobs", new { submissionId, documentType, documentRef }, JsonOptions, cancellationToken),
            cancellationToken);

        var body = await response.Content.ReadFromJsonAsync<SubmitJobResponse>(JsonOptions, cancellationToken)
            ?? throw new TransientAiReviewException("AI review service returned an empty submit response.");

        return new AiReviewSubmitResult { ExternalJobId = body.JobId };
    }

    public async Task<AiReviewPollResult> PollAsync(string externalJobId, CancellationToken cancellationToken)
    {
        var response = await SendAsync(
            () => _httpClient.GetAsync($"ai/review-jobs/{externalJobId}", cancellationToken),
            cancellationToken);

        var body = await response.Content.ReadFromJsonAsync<PollJobResponse>(JsonOptions, cancellationToken)
            ?? throw new TransientAiReviewException("AI review service returned an empty poll response.");

        return new AiReviewPollResult
        {
            Status = body.Status,
            Verdict = body.Verdict,
            Confidence = body.Confidence,
            FlaggedIssues = body.FlaggedIssues
        };
    }

    private static async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await send();
        }
        catch (HttpRequestException ex)
        {
            throw new TransientAiReviewException("Failed to reach the AI review service.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TransientAiReviewException("AI review service request timed out.", ex);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var statusCode = (int)response.StatusCode;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (statusCode >= 500)
        {
            throw new TransientAiReviewException($"AI review service returned {statusCode}: {body}");
        }

        throw new PermanentAiReviewException($"AI review service returned {statusCode}: {body}");
    }

    private record SubmitJobResponse(string JobId, string Status);

    private record PollJobResponse(string JobId, string Status, string? Verdict, double? Confidence, List<string> FlaggedIssues);
}
