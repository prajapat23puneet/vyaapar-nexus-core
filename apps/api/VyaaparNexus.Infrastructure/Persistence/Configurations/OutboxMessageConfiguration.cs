using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VyaaparNexus.Domain.Entities;

namespace VyaaparNexus.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(x => x.MessageType).HasColumnName("message_type").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("JSONB").IsRequired();
        builder.Property(x => x.Exchange).HasColumnName("exchange").HasMaxLength(200).IsRequired();
        builder.Property(x => x.RoutingKey).HasColumnName("routing_key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(x => x.PublishedAt).HasColumnName("published_at");
        builder.Property(x => x.RetryCount).HasColumnName("retry_count").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.LastError).HasColumnName("last_error").HasColumnType("TEXT");
        
        builder.HasIndex(x => x.CreatedAt).HasFilter("published_at IS NULL");
        builder.HasIndex(x => x.CorrelationId);
    }
}
