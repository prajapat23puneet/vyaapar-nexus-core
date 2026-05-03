using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VyaaparNexus.Domain.Entities;

namespace VyaaparNexus.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20).IsRequired();
        builder.Property(x => x.AddressLine1).HasColumnName("address_line1").HasMaxLength(255).IsRequired();
        builder.Property(x => x.AddressLine2).HasColumnName("address_line2").HasMaxLength(255);
        builder.Property(x => x.City).HasColumnName("city").HasMaxLength(80).IsRequired();
        builder.Property(x => x.State).HasColumnName("state").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Pincode).HasColumnName("pincode").HasMaxLength(10).IsRequired();
        builder.Property(x => x.Country).HasColumnName("country").HasMaxLength(60).IsRequired().HasDefaultValue("India");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("NOW()");
        
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
