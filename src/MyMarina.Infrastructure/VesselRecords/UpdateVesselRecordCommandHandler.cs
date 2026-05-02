using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.VesselRecords;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.VesselRecords;

public class UpdateVesselRecordCommandHandler(AppDbContext db)
    : ICommandHandler<UpdateVesselRecordCommand, VesselRecordDto>
{
    public async Task<VesselRecordDto> HandleAsync(UpdateVesselRecordCommand command, CancellationToken ct = default)
    {
        var record = await db.MarinaVesselRecords
            .Include(r => r.Vessel)
            .FirstOrDefaultAsync(r => r.Id == command.Id && r.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"VesselRecord {command.Id} not found.");

        if (command.BillingAccountId.HasValue) record.BillingAccountId = command.BillingAccountId.Value;
        if (command.InsuranceProvider is not null) record.InsuranceProvider = command.InsuranceProvider;
        if (command.InsurancePolicyNumber is not null) record.InsurancePolicyNumber = command.InsurancePolicyNumber;
        if (command.InsuranceExpiresOn.HasValue) record.InsuranceExpiresOn = command.InsuranceExpiresOn.Value;
        if (command.Notes is not null) record.Notes = command.Notes;
        if (command.MarkInsuranceVerified == true)
        {
            record.InsuranceVerifiedAt = DateTimeOffset.UtcNow;
            record.InsuranceVerifiedByUserId = command.RequestingUserId;
        }

        await db.SaveChangesAsync(ct);

        bool isGhost = record.Vessel.OwnerUserId == null;
        return CreateVesselRecordCommandHandler.ToDto(record, record.Vessel, isGhost);
    }
}
