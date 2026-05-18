using Hangfire;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Photos;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;
using MyMarina.Infrastructure.Storage;

namespace MyMarina.Infrastructure.Photos;

public class CreateUploadTicketCommandHandler(
    AppDbContext db,
    IStorageProvider storage) : ICommandHandler<CreateUploadTicketCommand, UploadTicket>
{
    private const long MaxFileSizeBytes = 20_971_520; // 20 MB

    public async Task<UploadTicket> HandleAsync(CreateUploadTicketCommand command, CancellationToken ct = default)
    {
        if (command.FileSizeBytes > MaxFileSizeBytes)
            throw new ArgumentException($"File size {command.FileSizeBytes} exceeds the 20 MB limit.");

        if (command.ImageWidth.HasValue && command.ImageHeight.HasValue)
        {
            var (valid, error) = AspectRatioValidator.Validate(command.Kind, command.ImageWidth.Value, command.ImageHeight.Value);
            if (!valid) throw new ArgumentException(error);
        }

        // Enforce Logo/Banner uniqueness before issuing the ticket
        if (command.Kind is MarinaPhotoKind.Logo or MarinaPhotoKind.Banner)
        {
            var exists = await db.MarinaPhotos
                .AnyAsync(p => p.MarinaId == command.MarinaId && p.Kind == command.Kind, ct);
            if (exists)
                throw new InvalidOperationException(
                    $"A {command.Kind} photo already exists for this marina. Delete it before uploading a new one.");
        }

        var marina = await db.Marinas
            .IgnoreQueryFilters()
            .Where(m => m.Id == command.MarinaId)
            .Select(m => new { m.TenantId })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Marina not found.");

        var ext = command.ContentType.Split('/').Last().Replace("jpeg", "jpg");
        var key = $"{marina.TenantId}/{command.MarinaId}/marina/{command.Kind.ToString().ToLower()}/{Guid.CreateVersion7()}.{ext}";
        var ttl = TimeSpan.FromMinutes(15);

        return await storage.CreateUploadTicketAsync(key, command.ContentType, command.FileSizeBytes, ttl, ct);
    }
}
