using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", t =>
        {
            t.HasCheckConstraint("CK_Categories_Slug", "[Slug] NOT LIKE '% %'");
            t.HasCheckConstraint("CK_Categories_SortOrder", "[SortOrder] >= 0");
        });
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(100);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasIndex(c => c.Slug).IsUnique();

        builder.HasData(
            new { Id = 1, Name = "Очищення", Slug = "cleansing", SortOrder = 1, CreatedAt = SeedDates.Initial },
            new { Id = 2, Name = "Зволоження", Slug = "moisturizing", SortOrder = 2, CreatedAt = SeedDates.Initial },
            new { Id = 3, Name = "Сироватки", Slug = "serums", SortOrder = 3, CreatedAt = SeedDates.Initial },
            new { Id = 4, Name = "Сонцезахист", Slug = "sun-protection", SortOrder = 4, CreatedAt = SeedDates.Initial },
            new { Id = 5, Name = "Проблемна шкіра", Slug = "acne-prone", SortOrder = 5, CreatedAt = SeedDates.Initial },
            new { Id = 6, Name = "Чутлива шкіра", Slug = "sensitive", SortOrder = 6, CreatedAt = SeedDates.Initial });
    }
}
