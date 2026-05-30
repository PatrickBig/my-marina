using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Leases;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;
using MyMarina.Infrastructure.Invoicing;

namespace MyMarina.Infrastructure.Leases;

public class ApproveLeaseInquiryCommandHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IMessageBus bus)
    : ICommandHandler<ApproveLeaseInquiryCommand, LeaseInquiryDto>
{
    public async Task<LeaseInquiryDto> HandleAsync(ApproveLeaseInquiryCommand command, CancellationToken ct = default)
    {
        var inquiry = await db.SlipLeaseInquiries
            .Include(i => i.Slip)
            .FirstOrDefaultAsync(i => i.Id == command.InquiryId && i.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"Lease inquiry {command.InquiryId} not found.");

        if (inquiry.Status is LeaseInquiryStatus.Approved)
            throw new InvalidOperationException("Inquiry is already approved.");

        if (inquiry.Status is LeaseInquiryStatus.Declined or LeaseInquiryStatus.Withdrawn)
            throw new InvalidOperationException($"Cannot approve an inquiry with status {inquiry.Status}.");

        if (inquiry.AgreedBaseRate == null || inquiry.AgreedRateKind == null)
            throw new InvalidOperationException("Agreed rate must be set before approving.");

        var startDate = inquiry.AssignmentStartDate
            ?? inquiry.DesiredStartDate
            ?? DateOnly.FromDateTime(DateTime.Today);

        // Find or create a BillingAccount for the requesting user at this marina
        var existingAccount = await db.BillingAccountMembers
            .Where(m => m.UserId == inquiry.RequestingUserId)
            .Join(db.BillingAccounts, m => m.BillingAccountId, a => a.Id, (m, a) => a)
            .FirstOrDefaultAsync(a => a.MarinaId == inquiry.MarinaId && a.IsActive, ct);

        BillingAccount billingAccount;
        if (existingAccount != null)
        {
            billingAccount = existingAccount;
        }
        else
        {
            var user = await userManager.FindByIdAsync(inquiry.RequestingUserId.ToString())
                ?? throw new KeyNotFoundException("Requesting user not found.");

            billingAccount = new BillingAccount
            {
                MarinaId       = inquiry.MarinaId,
                DisplayName    = $"{user.FirstName} {user.LastName}".Trim(),
                BillingEmail   = user.Email ?? string.Empty,
                IsActive       = true,
            };
            db.BillingAccounts.Add(billingAccount);

            db.BillingAccountMembers.Add(new BillingAccountMember
            {
                BillingAccountId = billingAccount.Id,
                UserId           = inquiry.RequestingUserId,
                Role             = BillingAccountRole.Owner,
                InvitedAt        = DateTimeOffset.UtcNow,
                AcceptedAt       = DateTimeOffset.UtcNow,
            });
        }

        // Resolve vessel ID — required for SlipAssignment
        var vesselId = inquiry.VesselId
            ?? await db.Vessels
                .Where(v => v.OwnerUserId == inquiry.RequestingUserId && !v.IsArchived)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => (Guid?)v.Id)
                .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No vessel found for this boater. Ask them to add a vessel before approving.");

        // Create the SlipAssignment
        var assignment = new SlipAssignment
        {
            SlipId            = inquiry.SlipId,
            BillingAccountId  = billingAccount.Id,
            VesselId          = vesselId,
            AssignmentType    = inquiry.DesiredTerm switch
            {
                LeaseTerm.Monthly  => AssignmentType.Monthly,
                LeaseTerm.Seasonal => AssignmentType.Seasonal,
                _                  => AssignmentType.Annual,
            },
            StartDate         = startDate,
            EndDate           = inquiry.AssignmentEndDate,
            BaseRate          = inquiry.AgreedBaseRate.Value,
            AllowOwnerSubletWhenAway   = false,
            AllowHolderSublet          = false,
            OwnerSubletShareToHolder   = 0,
            HolderSubletShareToOwner   = 0,
            Notes             = inquiry.MarinaNote,
        };
        db.SlipAssignments.Add(assignment);

        // Ensure vessel is visible at this marina (create MarinaVesselRecord if needed)
        if (inquiry.VesselId.HasValue)
        {
            var hasRecord = await db.MarinaVesselRecords
                .AnyAsync(r => r.MarinaId == inquiry.MarinaId && r.VesselId == inquiry.VesselId.Value, ct);
            if (!hasRecord)
            {
                db.MarinaVesselRecords.Add(new MarinaVesselRecord
                {
                    MarinaId         = inquiry.MarinaId,
                    VesselId         = inquiry.VesselId.Value,
                    BillingAccountId = billingAccount.Id,
                });
            }
        }

        // Stamp the inquiry as approved
        inquiry.Status           = LeaseInquiryStatus.Approved;
        inquiry.ApprovedByUserId = command.ApprovingUserId;
        inquiry.ApprovedAt       = DateTimeOffset.UtcNow;
        inquiry.SlipAssignmentId = assignment.Id;
        inquiry.BillingAccountId = billingAccount.Id;
        if (inquiry.ReviewedByUserId == null)
        {
            inquiry.ReviewedByUserId = command.ApprovingUserId;
            inquiry.ReviewedAt       = DateTimeOffset.UtcNow;
        }

        // Create initial draft invoice for the first billing period
        var nextInvoiceNumber = await db.Invoices
            .Where(i => i.MarinaId == inquiry.MarinaId)
            .CountAsync(ct) + 1;

        var termLabel = inquiry.DesiredTerm switch
        {
            LeaseTerm.Monthly  => "Month",
            LeaseTerm.Seasonal => "Season",
            _                  => "Year",
        };
        var rateLabel = inquiry.AgreedRateKind == RateKind.PerFoot
            ? $"${inquiry.AgreedBaseRate!.Value}/ft/{termLabel.ToLower()}"
            : $"${inquiry.AgreedBaseRate!.Value}/{termLabel.ToLower()}";
        var lineTotal = inquiry.AgreedRateKind == RateKind.PerFoot
            ? Math.Round(inquiry.AgreedBaseRate!.Value * inquiry.Slip.MaxLength, 2)
            : inquiry.AgreedBaseRate!.Value;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var invoice = new Invoice
        {
            MarinaId         = inquiry.MarinaId,
            BillingAccountId = billingAccount.Id,
            SlipAssignmentId = assignment.Id,
            InvoiceNumber    = $"INV-{nextInvoiceNumber:D4}",
            Status           = InvoiceStatus.Draft,
            IssuedDate       = today,
            DueDate          = today.AddDays(30),
            SubTotal         = lineTotal,
            TaxAmount        = 0m,
            TotalAmount      = lineTotal,
            AmountPaid       = 0m,
            Notes            = $"First {termLabel.ToLower()} — {inquiry.Slip.Name}",
        };
        var lineItem = new InvoiceLineItem
        {
            InvoiceId        = invoice.Id,
            Description      = $"{inquiry.Slip.Name} — {termLabel} 1 ({rateLabel})",
            Quantity         = 1m,
            UnitPrice        = lineTotal,
            LineTotal        = lineTotal,
            SlipAssignmentId = assignment.Id,
        };
        invoice.LineItems = [lineItem];
        db.Invoices.Add(invoice);

        await db.SaveChangesAsync(ct);

        // Fire post-approval onboarding pipeline
        await bus.PublishAsync(new SlipLeaseApproved(
            InquiryId:       inquiry.Id,
            SlipId:          inquiry.SlipId,
            MarinaId:        inquiry.MarinaId,
            SlipAssignmentId: assignment.Id,
            BillingAccountId: billingAccount.Id,
            RequestingUserId: inquiry.RequestingUserId,
            ApprovedByUserId: command.ApprovingUserId), ct);

        return await LeaseInquiryMappers.ToDtoAsync(inquiry, db, ct);
    }
}
