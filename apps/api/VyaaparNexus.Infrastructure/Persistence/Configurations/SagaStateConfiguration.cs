using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VyaaparNexus.Domain.Entities;

namespace VyaaparNexus.Infrastructure.Persistence.Configurations;

public class SagaStateConfiguration : IEntityTypeConfiguration<SagaState>
{
    public void Configure(EntityTypeBuilder<SagaState> builder)
    {
        builder.ToTable("saga_states");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(x => x.CurrentState).HasColumnName("current_state").HasMaxLength(50).IsRequired();
        builder.Property(x => x.InventoryReserved).HasColumnName("inventory_reserved").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.PaymentProcessed).HasColumnName("payment_processed").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ShippingDispatched).HasColumnName("shipping_dispatched").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.NotificationSent).HasColumnName("notification_sent").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.StartedAt).HasColumnName("started_at").IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.DurationMs).HasColumnName("duration_ms");
        builder.Property(x => x.LastError).HasColumnName("last_error").HasColumnType("TEXT");
        
        builder.HasIndex(x => x.CorrelationId).IsUnique();
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.CurrentState).HasFilter("current_state NOT IN ('OrderCompleted', 'OrderCancelled')");
    }
}
