using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Leases;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Leases;

public class CreateLeaseInquiryCommandHandler(AppDbContext db)
    : ICommandHandler<CreateLeaseInquiryCommand, LeaseInquiryDto>
{
    public async Task<LeaseInquiryDto> HandleAsync(CreateLeaseInquiryCommand command, CancellationToken ct = default)
    {
        var slip = await db.Slips
            .FirstOrDefaultAsync(s => s.Id == command.SlipId && s.Status == SlipStatus.Active, ct)
            ?? throw new KeyNotFoundException($"Slip {command.SlipId} not found or inactive.");

        if (slip.ResolvedLeaseBaseRate == null)
            throw new InvalidOperationException("This slip is not currently listed for lease.");

        var inquiry = new SlipLeaseInquiry
        {
            SlipId            = command.SlipId,
            MarinaId          = slip.MarinaId,
            RequestingUserId  = command.RequestingUserId,
            VesselId          = command.VesselId,
            DesiredTerm       = command.DesiredTerm,
            DesiredStartDate  = command.DesiredStartDate,
            Message           = command.Message,
            // Pre-fill agreed terms from resolved pricing rule so marina can just confirm
            AgreedRateKind    = RateKind.Flat,
            AgreedBaseRate    = slip.ResolvedLeaseBaseRate,
            Status            = LeaseInquiryStatus.Pending,
        };

        db.SlipLeaseInquiries.Add(inquiry);
        await db.SaveChangesAsync(ct);

        return await LeaseInquiryMappers.ToDtoAsync(inquiry, db, ct);
    }
}
