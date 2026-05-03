using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VyaaparNexus.Domain.Entities;

namespace VyaaparNexus.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(50).IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("NUMERIC(10,2)").IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(x => x.LineTotal).HasColumnName("line_total").HasColumnType("NUMERIC(10,2)").IsRequired();
        
        // Constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_OrderItem_Quantity", "quantity > 0"));
        
        builder.HasIndex(x => x.OrderId);
        
        builder.HasOne(x => x.Order)
               .WithMany(o => o.Items)
               .HasForeignKey(x => x.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
               
        // Assume Product is conceptually connected, but don't strictly enforce cascade delete if product is soft deleted.
        // Keeping it simple as per PRD
    }
}
