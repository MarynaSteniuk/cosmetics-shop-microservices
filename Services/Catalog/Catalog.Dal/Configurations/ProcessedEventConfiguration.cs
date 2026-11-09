using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("ProcessedEvents");
        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventId).ValueGeneratedNever();
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ProcessedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
