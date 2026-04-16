using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Platform;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Platform;

public class GetPlatformUserQueryHandler(AppDbContext db)
    : IQueryHandler<GetPlatformUserQuery, PlatformUserDto?>
{
    public async Task<PlatformUserDto?> HandleAsync(
        GetPlatformUserQuery query, CancellationToken ct = default)
    {
        return await db.Users
            .Where(u => u.Id == query.UserId)
            .Select(u => new PlatformUserDto(
                u.Id,
                u.Email!,
                u.FirstName,
                u.LastName,
                u.Role,
                u.TenantId,
                u.TenantId != null
                    ? db.Tenants.Where(t => t.Id == u.TenantId).Select(t => t.Name).FirstOrDefault()
                    : null,
                u.MarinaId,
                u.MarinaId != null
                    ? db.Marinas.Where(m => m.Id == u.MarinaId).Select(m => m.Name).FirstOrDefault()
                    : null,
                u.IsActive,
                u.CreatedAt,
                u.LastLoginAt))
            .FirstOrDefaultAsync(ct);
    }
}
