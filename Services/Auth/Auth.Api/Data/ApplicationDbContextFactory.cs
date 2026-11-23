using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Auth.Api.Data;

/// <summary>Потрібна лише для команди dotnet ef migrations: під час роботи рядок підключення дає Aspire.</summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost,14333;Database=CosmeticsIdentityDb;User ID=sa;TrustServerCertificate=True")
            .Options;

        return new ApplicationDbContext(options);
    }
}
