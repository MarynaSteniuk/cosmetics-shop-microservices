using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).IsRequired().HasMaxLength(100);
        builder.Property(b => b.Country).HasMaxLength(60);
        builder.Property(b => b.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(b => b.Name).IsUnique();

        builder.HasData(
            new { Id = 1, Name = "CeraVe", Country = "США", CreatedAt = SeedDates.Initial },
            new { Id = 2, Name = "La Roche-Posay", Country = "Франція", CreatedAt = SeedDates.Initial },
            new { Id = 3, Name = "The Ordinary", Country = "Канада", CreatedAt = SeedDates.Initial },
            new { Id = 4, Name = "Bioderma", Country = "Франція", CreatedAt = SeedDates.Initial },
            new { Id = 5, Name = "Neutrogena", Country = "США", CreatedAt = SeedDates.Initial },
            new { Id = 6, Name = "Vichy", Country = "Франція", CreatedAt = SeedDates.Initial },
            new { Id = 7, Name = "Cetaphil", Country = "Швейцарія", CreatedAt = SeedDates.Initial },
            new { Id = 8, Name = "Avène", Country = "Франція", CreatedAt = SeedDates.Initial });
    }
}
