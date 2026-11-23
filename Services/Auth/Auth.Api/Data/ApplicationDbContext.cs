using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(u => u.FirstName).HasMaxLength(100);
            user.Property(u => u.LastName).HasMaxLength(100);
            user.Property(u => u.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            user.HasIndex(u => u.CustomerId);
        });

        builder.Entity<RefreshToken>(token =>
        {
            token.ToTable("RefreshTokens");
            token.HasKey(t => t.Id);
            token.Property(t => t.TokenHash).IsRequired().HasMaxLength(64).IsUnicode(false);
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.Property(t => t.CreatedByIp).HasMaxLength(64);
            token.Property(t => t.RevokedReason).HasMaxLength(200);
            token.Property(t => t.ReplacedByTokenHash).HasMaxLength(64).IsUnicode(false);
            token.HasIndex(t => new { t.UserId, t.ExpiresAt });
            token.HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            token.Ignore(t => t.IsActive);
            token.Ignore(t => t.IsExpired);
            token.Ignore(t => t.IsRevoked);
        });
    }
}
