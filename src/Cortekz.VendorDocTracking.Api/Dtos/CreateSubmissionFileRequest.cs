using System.ComponentModel.DataAnnotations;

namespace Cortekz.VendorDocTracking.Api.Dtos;

public class CreateSubmissionFileRequest
{
    [Required, MaxLength(300)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string ContentType { get; set; } = string.Empty;

    [Range(0, long.MaxValue)]
    public long SizeBytes { get; set; }

    [Required, MaxLength(500)]
    public string StorageKey { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Checksum { get; set; }
}
