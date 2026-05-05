using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VyaaparNexus.Domain.Entities;

namespace VyaaparNexus.Infrastructure.Persistence.Configurations;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.MessageId).HasColumnName("message_id").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ConsumerName).HasColumnName("consumer_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id");
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at").IsRequired().HasDefaultValueSql("NOW()");
        
        builder.HasIndex(x => new { x.MessageId, x.ConsumerName }).IsUnique();
        builder.HasIndex(x => new { x.ConsumerName, x.ProcessedAt }).IsDescending(false, true);
    }
}
