using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Identity;

public class LogoutCommandHandler(AppDbContext db) : ICommandHandler<LogoutCommand>
{
    public async Task HandleAsync(LogoutCommand command, CancellationToken ct = default)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(command.RefreshToken)));
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is not null && token.RevokedAt is null)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
