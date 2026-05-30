using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Leases;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Leases;

public class WithdrawLeaseInquiryCommandHandler(AppDbContext db)
    : ICommandHandler<WithdrawLeaseInquiryCommand, LeaseInquiryDto>
{
    public async Task<LeaseInquiryDto> HandleAsync(WithdrawLeaseInquiryCommand command, CancellationToken ct = default)
    {
        var inquiry = await db.SlipLeaseInquiries
            .Include(i => i.Slip)
            .FirstOrDefaultAsync(i => i.Id == command.InquiryId && i.RequestingUserId == command.RequestingUserId, ct)
            ?? throw new KeyNotFoundException($"Lease inquiry {command.InquiryId} not found.");

        if (inquiry.Status is LeaseInquiryStatus.Approved or LeaseInquiryStatus.Declined or LeaseInquiryStatus.Withdrawn)
            throw new InvalidOperationException($"Cannot withdraw an inquiry with status {inquiry.Status}.");

        inquiry.Status = LeaseInquiryStatus.Withdrawn;
        await db.SaveChangesAsync(ct);
        return await LeaseInquiryMappers.ToDtoAsync(inquiry, db, ct);
    }
}
