using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class ProductDetailConfiguration : IEntityTypeConfiguration<ProductDetail>
{
    public void Configure(EntityTypeBuilder<ProductDetail> builder)
    {
        var skinTypes = string.Join(", ", SkinTypes.Values.Select(v => $"'{v}'"));
        var volumeUnits = string.Join(", ", VolumeUnits.Values.Select(v => $"'{v}'"));

        builder.ToTable("ProductDetails", t =>
        {
            t.HasCheckConstraint("CK_ProductDetails_Volume", "[Volume] > 0");
            t.HasCheckConstraint("CK_ProductDetails_VolumeUnit", $"[VolumeUnit] IN ({volumeUnits})");
            t.HasCheckConstraint("CK_ProductDetails_SkinType", $"[SkinType] IN ({skinTypes})");
        });
        builder.HasKey(d => d.ProductId);

        builder.Property(d => d.Description).HasMaxLength(2000);
        builder.Property(d => d.Ingredients);
        builder.Property(d => d.HowToUse).HasMaxLength(1000);
        builder.Property(d => d.Volume).HasColumnType("decimal(8,2)");
        builder.Property(d => d.VolumeUnit).IsRequired().HasMaxLength(5);
        builder.Property(d => d.SkinType).IsRequired().HasMaxLength(20);

        builder.HasIndex(d => d.SkinType);
    }
}
