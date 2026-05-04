using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.VesselRecords;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.VesselRecords;

public class CreateVesselRecordCommandHandler(AppDbContext db, IEmailService emailService)
    : ICommandHandler<CreateVesselRecordCommand, VesselRecordDto>
{
    public async Task<VesselRecordDto> HandleAsync(CreateVesselRecordCommand command, CancellationToken ct = default)
    {
        Vessel vessel;
        bool isGhost = false;

        if (command.VesselId.HasValue)
        {
            vessel = await db.Vessels.FirstOrDefaultAsync(v => v.Id == command.VesselId.Value, ct)
                ?? throw new KeyNotFoundException($"Vessel {command.VesselId} not found.");
        }
        else
        {
            // Ghost vessel — marina creates it on behalf of an unregistered owner
            if (string.IsNullOrWhiteSpace(command.ClaimEmail))
                throw new InvalidOperationException("ClaimEmail is required when creating a ghost vessel.");
            if (string.IsNullOrWhiteSpace(command.VesselName))
                throw new InvalidOperationException("VesselName is required when creating a ghost vessel.");

            // If a user with that email already exists and has a vessel, look for an existing match
            // (simple heuristic: same name + length at same marina email → use existing)
            // For MVP we always create a new ghost vessel per marina entry.
            vessel = new Vessel
            {
                OwnerUserId        = null,
                ClaimEmail         = command.ClaimEmail.Trim().ToLowerInvariant(),
                Name               = command.VesselName,
                Make               = command.VesselMake,
                Model              = command.VesselModel,
                Year               = command.VesselYear,
                Length             = command.VesselLength ?? 0,
                Beam               = command.VesselBeam ?? 0,
                Draft              = command.VesselDraft ?? 0,
                BoatType           = command.VesselBoatType ?? Domain.Enums.BoatType.Other,
                HullColor          = command.VesselHullColor,
                RegistrationNumber = command.VesselRegistrationNumber,
                RegistrationState  = command.VesselRegistrationState,
            };
            db.Vessels.Add(vessel);
            isGhost = true;
        }

        // Prevent duplicate (marina, vessel) records
        var exists = await db.MarinaVesselRecords
            .AnyAsync(r => r.MarinaId == command.MarinaId && r.VesselId == vessel.Id, ct);
        if (exists)
            throw new InvalidOperationException("A vessel record for this boat already exists at this marina.");

        var record = new MarinaVesselRecord
        {
            MarinaId                  = command.MarinaId,
            VesselId                  = vessel.Id,
            BillingAccountId          = command.BillingAccountId,
            InsuranceProvider         = command.InsuranceProvider,
            InsurancePolicyNumber     = command.InsurancePolicyNumber,
            InsuranceExpiresOn        = command.InsuranceExpiresOn,
            Notes                     = command.Notes,
        };

        db.MarinaVesselRecords.Add(record);
        await db.SaveChangesAsync(ct);

        if (isGhost)
        {
            var marinaName = await db.Marinas
                .Where(m => m.Id == command.MarinaId)
                .Select(m => m.Name)
                .FirstOrDefaultAsync(ct) ?? "the marina";

            await emailService.SendGhostVesselClaimAsync(
                toEmail:    vessel.ClaimEmail!,
                marinaName: marinaName,
                vesselName: vessel.Name,
                vesselId:   vessel.Id,
                ct:         ct);
        }

        return ToDto(record, vessel, isGhost);
    }

    internal static VesselRecordDto ToDto(MarinaVesselRecord r, Vessel v, bool isGhost) => new(
        Id:                    r.Id,
        MarinaId:              r.MarinaId,
        VesselId:              r.VesselId,
        BillingAccountId:      r.BillingAccountId,
        VesselName:            v.Name,
        VesselMake:            v.Make,
        VesselModel:           v.Model,
        VesselYear:            v.Year,
        VesselLength:          v.Length,
        VesselBoatType:        v.BoatType.ToString(),
        VesselIsGhost:         isGhost || v.OwnerUserId == null,
        InsuranceProvider:     r.InsuranceProvider,
        InsurancePolicyNumber: r.InsurancePolicyNumber,
        InsuranceExpiresOn:    r.InsuranceExpiresOn,
        InsuranceVerifiedAt:   r.InsuranceVerifiedAt,
        Notes:                 r.Notes,
        CreatedAt:             r.CreatedAt
    );
}
