using System.ComponentModel.DataAnnotations;

namespace Cortekz.VendorDocTracking.Api.Dtos;

public class CreatePurchaseOrderRequest
{
    [Required, MaxLength(50)]
    public string PoNumber { get; set; } = string.Empty;

    [Required]
    public Guid VendorId { get; set; }

    [Required, MaxLength(100)]
    public string ProjectCode { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Title { get; set; }

    [Required]
    public DateOnly IssuedOn { get; set; }

    [Required, MinLength(1)]
    public List<CreateDocumentRequirementRequest> DocumentRequirements { get; set; } = new();
}
