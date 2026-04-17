using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyMarina.Application.Abstractions;

namespace MyMarina.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used exclusively by EF Core tooling (migrations, scaffolding).
/// Not used at runtime. Provides a no-op tenant context so global query filters
/// are skipped during migration generation.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=mymarina;Username=mymarina;Password=mymarina;SSL Mode=Disable",
                npgsql => npgsql
                    .MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                    .MigrationsHistoryTable("__EFMigrationsHistory", "mymarina"))
            .Options;

        return new AppDbContext(options, new DesignTimeTenantContext());
    }

    /// <summary>
    /// Platform operator context — bypasses all global query filters during migrations.
    /// </summary>
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public bool IsPlatformOperator => true;
    }
}
