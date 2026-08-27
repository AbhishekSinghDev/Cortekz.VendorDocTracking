namespace Cortekz.VendorDocTracking.Api.Models;

public class PurchaseOrder
{
    public Guid Id { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateOnly IssuedOn { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Vendor Vendor { get; set; } = null!;
    public List<DocumentRequirement> DocumentRequirements { get; set; } = new();
}
