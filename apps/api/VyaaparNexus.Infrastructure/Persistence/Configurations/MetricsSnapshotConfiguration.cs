using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VyaaparNexus.Domain.Entities;

namespace VyaaparNexus.Infrastructure.Persistence.Configurations;

public class MetricsSnapshotConfiguration : IEntityTypeConfiguration<MetricsSnapshot>
{
    public void Configure(EntityTypeBuilder<MetricsSnapshot> builder)
    {
        builder.ToTable("metrics_snapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ActiveSagas).HasColumnName("active_sagas").IsRequired();
        builder.Property(x => x.DeadLetterCount).HasColumnName("dead_letter_count").IsRequired();
        builder.Property(x => x.OutboxPending).HasColumnName("outbox_pending").IsRequired();
        builder.Property(x => x.OrdersPerMinute).HasColumnName("orders_per_minute").HasColumnType("NUMERIC(10,2)").IsRequired();
        builder.Property(x => x.SagaSuccessRate).HasColumnName("saga_success_rate").HasColumnType("NUMERIC(5,4)").IsRequired();
        builder.Property(x => x.P95LatencyMs).HasColumnName("p95_latency_ms").IsRequired();
        builder.Property(x => x.CpuPercent).HasColumnName("cpu_percent").HasColumnType("NUMERIC(5,2)").IsRequired();
        builder.Property(x => x.MemoryPercent).HasColumnName("memory_percent").HasColumnType("NUMERIC(5,2)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("NOW()");
        
        builder.HasIndex(x => x.CreatedAt).IsDescending();
    }
}
