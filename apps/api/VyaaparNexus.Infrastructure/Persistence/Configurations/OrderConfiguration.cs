using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VyaaparNexus.Domain.Entities;

namespace VyaaparNexus.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.Subtotal).HasColumnName("subtotal").HasColumnType("NUMERIC(10,2)").IsRequired();
        builder.Property(x => x.TaxAmount).HasColumnName("tax_amount").HasColumnType("NUMERIC(10,2)").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.ShippingAmount).HasColumnName("shipping_amount").HasColumnType("NUMERIC(10,2)").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("NUMERIC(10,2)").IsRequired();
        builder.Property(x => x.ShippingAddress).HasColumnName("shipping_address").HasColumnType("JSONB").IsRequired();
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.PaymentReference).HasColumnName("payment_reference").HasMaxLength(200);
        builder.Property(x => x.FailureReason).HasColumnName("failure_reason").HasColumnType("TEXT");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
        
        builder.HasIndex(x => x.CorrelationId).IsUnique();
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt).IsDescending();
        
        builder.HasOne(x => x.Customer)
               .WithMany()
               .HasForeignKey(x => x.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
