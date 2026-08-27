using Cortekz.VendorDocTracking.Api.Models;

namespace Cortekz.VendorDocTracking.Api.Dtos;

public class PurchaseOrderResponse
{
    public Guid Id { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateOnly IssuedOn { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<DocumentRequirementResponse> DocumentRequirements { get; set; } = new();
}
