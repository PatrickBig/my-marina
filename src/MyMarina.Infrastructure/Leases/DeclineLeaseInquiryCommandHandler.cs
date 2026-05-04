using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Leases;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Leases;

public class DeclineLeaseInquiryCommandHandler(AppDbContext db, UserManager<ApplicationUser> userManager)
    : ICommandHandler<DeclineLeaseInquiryCommand, LeaseInquiryDto>
{
    public async Task<LeaseInquiryDto> HandleAsync(DeclineLeaseInquiryCommand command, CancellationToken ct = default)
    {
        var inquiry = await db.SlipLeaseInquiries
            .Include(i => i.Slip)
            .FirstOrDefaultAsync(i => i.Id == command.InquiryId && i.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"Lease inquiry {command.InquiryId} not found.");

        if (inquiry.Status is LeaseInquiryStatus.Approved or LeaseInquiryStatus.Declined or LeaseInquiryStatus.Withdrawn)
            throw new InvalidOperationException($"Cannot decline an inquiry with status {inquiry.Status}.");

        inquiry.Status           = LeaseInquiryStatus.Declined;
        inquiry.DeclinedByUserId = command.DecliningUserId;
        inquiry.DeclinedAt       = DateTimeOffset.UtcNow;
        if (command.Reason is not null) inquiry.MarinaNote = command.Reason;

        await db.SaveChangesAsync(ct);
        return await LeaseInquiryMappers.ToDtoAsync(inquiry, userManager, db, ct);
    }
}
