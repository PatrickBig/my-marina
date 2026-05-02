using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyMarina.Infrastructure.Persistence;

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

        return new AppDbContext(options);
    }
}
