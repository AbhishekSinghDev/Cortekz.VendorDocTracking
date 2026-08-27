using Cortekz.VendorDocTracking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cortekz.VendorDocTracking.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Vendors.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        var vendorA = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = "Acme Fabrication",
            Code = "ACME",
            ContactEmail = "contact@acme-fab.example",
            CreatedAt = now,
            UpdatedAt = now
        };

        var vendorB = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = "Northline Instruments",
            Code = "NORTHLINE",
            ContactEmail = "orders@northline.example",
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Vendors.AddRange(vendorA, vendorB);

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = "PO-2026-0001",
            VendorId = vendorA.Id,
            ProjectCode = "PRJ-100",
            Title = "Compressor skid fabrication",
            IssuedOn = DateOnly.FromDateTime(now),
            Status = PurchaseOrderStatus.Issued,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.PurchaseOrders.Add(po);

        var requirements = new List<DocumentRequirement>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = po.Id,
                DocumentType = DocumentType.Datasheet,
                Title = "Compressor datasheet",
                DueDate = DateOnly.FromDateTime(now.AddDays(14)),
                IsMandatory = true,
                Status = RequirementStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = po.Id,
                DocumentType = DocumentType.Drawing,
                Title = "Skid general arrangement drawing",
                DueDate = DateOnly.FromDateTime(now.AddDays(21)),
                IsMandatory = true,
                Status = RequirementStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = po.Id,
                DocumentType = DocumentType.TestCertificate,
                Title = "Hydrostatic test certificate",
                DueDate = DateOnly.FromDateTime(now.AddDays(30)),
                IsMandatory = false,
                Status = RequirementStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        db.DocumentRequirements.AddRange(requirements);

        await db.SaveChangesAsync();
    }
}
