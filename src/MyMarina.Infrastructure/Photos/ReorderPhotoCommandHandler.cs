using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Photos;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Photos;

public class ReorderPhotoCommandHandler(AppDbContext db) : ICommandHandler<ReorderPhotoCommand>
{
    public async Task HandleAsync(ReorderPhotoCommand command, CancellationToken ct = default)
    {
        var photo = await db.MarinaPhotos
            .FirstOrDefaultAsync(p => p.Id == command.PhotoId && p.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Photo not found.");

        var neighbour = command.Direction == ReorderDirection.Up
            ? await db.MarinaPhotos
                .Where(p => p.MarinaId == command.MarinaId && p.Kind == photo.Kind && p.SortOrder < photo.SortOrder)
                .OrderByDescending(p => p.SortOrder)
                .FirstOrDefaultAsync(ct)
            : await db.MarinaPhotos
                .Where(p => p.MarinaId == command.MarinaId && p.Kind == photo.Kind && p.SortOrder > photo.SortOrder)
                .OrderBy(p => p.SortOrder)
                .FirstOrDefaultAsync(ct);

        if (neighbour is null)
            throw new InvalidOperationException(
                command.Direction == ReorderDirection.Up
                    ? "Photo is already first in its category."
                    : "Photo is already last in its category.");

        (photo.SortOrder, neighbour.SortOrder) = (neighbour.SortOrder, photo.SortOrder);
        await db.SaveChangesAsync(ct);
    }
}
