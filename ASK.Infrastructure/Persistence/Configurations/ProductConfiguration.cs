using ASK.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ASK.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(500);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(500);
        builder.Property(p => p.Code).IsRequired().HasMaxLength(100);
        builder.Property(p => p.IntegrationCode).HasMaxLength(200);
        builder.Property(p => p.Barcode).HasMaxLength(100);
        builder.Property(p => p.SupplierProductId).HasMaxLength(50);
        builder.Property(p => p.ShortDescription).IsRequired().HasMaxLength(1000);
        builder.Property(p => p.Description).IsRequired().HasColumnType("TEXT");
        builder.Property(p => p.ImageUrl).HasMaxLength(1000);
        builder.Property(p => p.SupplierLink).HasMaxLength(1000);
        builder.Property(p => p.FeaturesJson).IsRequired().HasColumnType("JSON");
        builder.Property(p => p.SpecificationsJson).IsRequired().HasColumnType("JSON");
        builder.Property(p => p.Price).HasPrecision(18, 4);
        builder.Property(p => p.DiscountedPrice).HasPrecision(18, 4);
        builder.Property(p => p.Discount).HasPrecision(5, 2);
        builder.Property(p => p.TaxRate).HasPrecision(5, 2);
        builder.Property(p => p.Desi).HasPrecision(10, 3);
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(10);

        // Slug benzersiz index
        builder.HasIndex(p => p.Slug).IsUnique();
        // Tedarikçi kodu arama için index
        builder.HasIndex(p => p.Code);
        builder.HasIndex(p => p.Status);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
