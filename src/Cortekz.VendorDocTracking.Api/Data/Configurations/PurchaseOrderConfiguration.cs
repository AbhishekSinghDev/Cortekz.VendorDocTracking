using Cortekz.VendorDocTracking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortekz.VendorDocTracking.Api.Data.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders");

        builder.HasKey(po => po.Id);
        builder.Property(po => po.Id).HasColumnName("id");

        builder.Property(po => po.PoNumber).HasColumnName("po_number").HasMaxLength(50).IsRequired();
        builder.Property(po => po.VendorId).HasColumnName("vendor_id");
        builder.Property(po => po.ProjectCode).HasColumnName("project_code").HasMaxLength(100).IsRequired();
        builder.Property(po => po.Title).HasColumnName("title").HasMaxLength(300);
        builder.Property(po => po.IssuedOn).HasColumnName("issued_on");
        builder.Property(po => po.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(po => po.CreatedAt).HasColumnName("created_at");
        builder.Property(po => po.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(po => po.PoNumber).IsUnique();
        builder.HasIndex(po => po.VendorId);
        builder.HasIndex(po => po.ProjectCode);

        builder.HasOne(po => po.Vendor)
            .WithMany(v => v.PurchaseOrders)
            .HasForeignKey(po => po.VendorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
