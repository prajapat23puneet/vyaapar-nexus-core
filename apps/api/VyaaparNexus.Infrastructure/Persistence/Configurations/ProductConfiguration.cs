using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VyaaparNexus.Domain.Entities;

namespace VyaaparNexus.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("TEXT").IsRequired();
        builder.Property(x => x.Brand).HasColumnName("brand").HasMaxLength(100).IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("NUMERIC(10,2)").IsRequired();
        builder.Property(x => x.StockQuantity).HasColumnName("stock_quantity").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.ReorderLevel).HasColumnName("reorder_level").IsRequired().HasDefaultValue(10);
        builder.Property(x => x.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
        builder.Property(x => x.WeightGrams).HasColumnName("weight_grams").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.Tags).HasColumnName("tags").HasColumnType("TEXT[]");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("NOW()");

        // Constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_Product_UnitPrice", "unit_price > 0"));
        
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => x.IsActive).HasFilter("is_active = true");
        
        builder.HasOne(x => x.Category)
               .WithMany()
               .HasForeignKey(x => x.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
