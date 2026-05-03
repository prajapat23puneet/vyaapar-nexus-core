using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VyaaparNexus.Domain.Entities;

namespace VyaaparNexus.Infrastructure.Persistence.Configurations;

public class SagaEventLogConfiguration : IEntityTypeConfiguration<SagaEventLog>
{
    public void Configure(EntityTypeBuilder<SagaEventLog> builder)
    {
        builder.ToTable("saga_event_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ServiceName).HasColumnName("service_name").HasMaxLength(80).IsRequired();
        builder.Property(x => x.PreviousState).HasColumnName("previous_state").HasMaxLength(50);
        builder.Property(x => x.CurrentState).HasColumnName("current_state").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Message).HasColumnName("message").HasColumnType("TEXT");
        builder.Property(x => x.DurationMs).HasColumnName("duration_ms");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("JSONB");
        
        builder.HasIndex(x => new { x.CorrelationId, x.CreatedAt });
        builder.HasIndex(x => new { x.OrderId, x.CreatedAt });
    }
}
