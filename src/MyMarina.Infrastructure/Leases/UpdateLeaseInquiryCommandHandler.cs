using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Leases;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Leases;

public class UpdateLeaseInquiryCommandHandler(AppDbContext db, UserManager<ApplicationUser> userManager)
    : ICommandHandler<UpdateLeaseInquiryCommand, LeaseInquiryDto>
{
    public async Task<LeaseInquiryDto> HandleAsync(UpdateLeaseInquiryCommand command, CancellationToken ct = default)
    {
        var inquiry = await db.SlipLeaseInquiries
            .Include(i => i.Slip)
            .FirstOrDefaultAsync(i => i.Id == command.InquiryId && i.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"Lease inquiry {command.InquiryId} not found.");

        if (inquiry.Status is LeaseInquiryStatus.Approved or LeaseInquiryStatus.Declined or LeaseInquiryStatus.Withdrawn)
            throw new InvalidOperationException($"Cannot edit an inquiry with status {inquiry.Status}.");

        if (command.AgreedRateKind.HasValue)
            inquiry.AgreedRateKind = command.AgreedRateKind;

        if (command.AgreedBaseRate.HasValue) inquiry.AgreedBaseRate = command.AgreedBaseRate;
        if (command.AssignmentStartDate.HasValue) inquiry.AssignmentStartDate = command.AssignmentStartDate;
        inquiry.AssignmentEndDate = command.AssignmentEndDate;   // nullable — allow clearing
        if (command.MarinaNote is not null) inquiry.MarinaNote = command.MarinaNote;

        if (command.MarkUnderReview && inquiry.Status == LeaseInquiryStatus.Pending)
        {
            inquiry.Status      = LeaseInquiryStatus.UnderReview;
            inquiry.ReviewedByUserId = command.RequestingUserId;
            inquiry.ReviewedAt  = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return await LeaseInquiryMappers.ToDtoAsync(inquiry, userManager, db, ct);
    }
}
