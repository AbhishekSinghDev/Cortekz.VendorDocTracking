namespace Cortekz.VendorDocTracking.Api.Dtos;

public class SubmissionFileResponse
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string? Checksum { get; set; }
}
