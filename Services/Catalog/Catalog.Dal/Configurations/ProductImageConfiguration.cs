using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages", t =>
            t.HasCheckConstraint("CK_ProductImages_SortOrder", "[SortOrder] >= 0"));
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Url).IsRequired().HasMaxLength(500);
        builder.Property(i => i.AltText).HasMaxLength(200);

        builder.HasIndex(i => new { i.ProductId, i.Url }).IsUnique();
        builder.HasIndex(i => new { i.ProductId, i.SortOrder });
        builder.HasIndex(i => i.ProductId, "UX_ProductImages_MainPerProduct")
            .IsUnique()
            .HasFilter("[IsMain] = 1");
    }
}
