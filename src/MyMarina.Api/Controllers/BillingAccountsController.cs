using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.BillingAccounts;
using MyMarina.Application.VesselRecords;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
public class BillingAccountsController(
    ICommandHandler<CreateBillingAccountCommand, BillingAccountDto> createAccount,
    ICommandHandler<UpdateBillingAccountCommand, BillingAccountDto> updateAccount,
    IQueryHandler<GetBillingAccountsQuery, IReadOnlyList<BillingAccountDto>> getAccounts,
    IQueryHandler<GetBillingAccountQuery, BillingAccountDto> getAccount,
    ICommandHandler<InviteBillingAccountMemberCommand, BillingAccountMemberDto> inviteMember,
    ICommandHandler<AcceptBillingAccountMemberInviteCommand, BillingAccountMemberDto> acceptInvite,
    ICommandHandler<RemoveBillingAccountMemberCommand> removeMember,
    IQueryHandler<GetBillingAccountMembersQuery, IReadOnlyList<BillingAccountMemberDto>> getMembers,
    ICommandHandler<CreateVesselRecordCommand, VesselRecordDto> createVesselRecord,
    ICommandHandler<UpdateVesselRecordCommand, VesselRecordDto> updateVesselRecord,
    IQueryHandler<GetVesselRecordsQuery, IReadOnlyList<VesselRecordDto>> getVesselRecords,
    IQueryHandler<GetVesselRecordQuery, VesselRecordDto> getVesselRecord,
    IUserContext userContext)
    : ControllerBase
{
    // ---------- Billing Accounts ----------

    [HttpPost("marinas/{marinaId:guid}/billing-accounts")]
    [ProducesResponseType(typeof(BillingAccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateBillingAccount(Guid marinaId, [FromBody] CreateBillingAccountRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();

        var account = await createAccount.HandleAsync(new CreateBillingAccountCommand(
            MarinaId:               marinaId,
            RequestingUserId:       userContext.UserId!.Value,
            DisplayName:            request.DisplayName,
            BillingEmail:           request.BillingEmail,
            BillingPhone:           request.BillingPhone,
            BillingAddressStreet:   request.BillingAddressStreet,
            BillingAddressCity:     request.BillingAddressCity,
            BillingAddressState:    request.BillingAddressState,
            BillingAddressZip:      request.BillingAddressZip,
            BillingAddressCountry:  request.BillingAddressCountry,
            EmergencyContactName:   request.EmergencyContactName,
            EmergencyContactPhone:  request.EmergencyContactPhone,
            Notes:                  request.Notes), ct);

        return CreatedAtAction(nameof(GetBillingAccount), new { marinaId, accountId = account.Id }, account);
    }

    [HttpGet("marinas/{marinaId:guid}/billing-accounts")]
    [ProducesResponseType(typeof(IReadOnlyList<BillingAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBillingAccounts(Guid marinaId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        var accounts = await getAccounts.HandleAsync(new GetBillingAccountsQuery(marinaId, userContext.UserId!.Value), ct);
        return Ok(accounts);
    }

    [HttpGet("marinas/{marinaId:guid}/billing-accounts/{accountId:guid}")]
    [ProducesResponseType(typeof(BillingAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBillingAccount(Guid marinaId, Guid accountId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var account = await getAccount.HandleAsync(new GetBillingAccountQuery(accountId, marinaId, userContext.UserId!.Value), ct);
            return Ok(account);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPatch("marinas/{marinaId:guid}/billing-accounts/{accountId:guid}")]
    [ProducesResponseType(typeof(BillingAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBillingAccount(Guid marinaId, Guid accountId, [FromBody] UpdateBillingAccountRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var account = await updateAccount.HandleAsync(new UpdateBillingAccountCommand(
                Id:                     accountId,
                MarinaId:               marinaId,
                RequestingUserId:       userContext.UserId!.Value,
                DisplayName:            request.DisplayName,
                BillingEmail:           request.BillingEmail,
                BillingPhone:           request.BillingPhone,
                BillingAddressStreet:   request.BillingAddressStreet,
                BillingAddressCity:     request.BillingAddressCity,
                BillingAddressState:    request.BillingAddressState,
                BillingAddressZip:      request.BillingAddressZip,
                BillingAddressCountry:  request.BillingAddressCountry,
                EmergencyContactName:   request.EmergencyContactName,
                EmergencyContactPhone:  request.EmergencyContactPhone,
                Notes:                  request.Notes,
                IsActive:               request.IsActive), ct);
            return Ok(account);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ---------- Members ----------

    [HttpGet("marinas/{marinaId:guid}/billing-accounts/{accountId:guid}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<BillingAccountMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMembers(Guid marinaId, Guid accountId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var members = await getMembers.HandleAsync(new GetBillingAccountMembersQuery(accountId, marinaId, userContext.UserId!.Value), ct);
            return Ok(members);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("marinas/{marinaId:guid}/billing-accounts/{accountId:guid}/members/invite")]
    [ProducesResponseType(typeof(BillingAccountMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InviteMember(Guid marinaId, Guid accountId, [FromBody] InviteMemberRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();

        if (!Enum.TryParse<BillingAccountRole>(request.Role, ignoreCase: true, out var role))
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["role"] = [$"Unknown role: {request.Role}"] }));

        try
        {
            var member = await inviteMember.HandleAsync(new InviteBillingAccountMemberCommand(
                BillingAccountId:  accountId,
                MarinaId:          marinaId,
                RequestingUserId:  userContext.UserId!.Value,
                Email:             request.Email,
                Role:              role), ct);
            return CreatedAtAction(nameof(GetMembers), new { marinaId, accountId }, member);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }

    [HttpPost("marinas/{marinaId:guid}/billing-accounts/{accountId:guid}/members/{memberId:guid}/accept")]
    [ProducesResponseType(typeof(BillingAccountMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptInvite(Guid marinaId, Guid accountId, Guid memberId, CancellationToken ct)
    {
        try
        {
            var member = await acceptInvite.HandleAsync(new AcceptBillingAccountMemberInviteCommand(
                BillingAccountMemberId: memberId,
                UserId: userContext.UserId!.Value), ct);
            return Ok(member);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }

    [HttpDelete("marinas/{marinaId:guid}/billing-accounts/{accountId:guid}/members/{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid marinaId, Guid accountId, Guid memberId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            await removeMember.HandleAsync(new RemoveBillingAccountMemberCommand(
                BillingAccountMemberId: memberId,
                BillingAccountId:       accountId,
                MarinaId:               marinaId,
                RequestingUserId:       userContext.UserId!.Value), ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ---------- Vessel Records ----------

    [HttpGet("marinas/{marinaId:guid}/vessel-records")]
    [ProducesResponseType(typeof(IReadOnlyList<VesselRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetVesselRecords(Guid marinaId, [FromQuery] Guid? billingAccountId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        var records = await getVesselRecords.HandleAsync(new GetVesselRecordsQuery(marinaId, userContext.UserId!.Value, billingAccountId), ct);
        return Ok(records);
    }

    [HttpGet("marinas/{marinaId:guid}/vessel-records/{recordId:guid}")]
    [ProducesResponseType(typeof(VesselRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVesselRecord(Guid marinaId, Guid recordId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var record = await getVesselRecord.HandleAsync(new GetVesselRecordQuery(recordId, marinaId, userContext.UserId!.Value), ct);
            return Ok(record);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("marinas/{marinaId:guid}/vessel-records")]
    [ProducesResponseType(typeof(VesselRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateVesselRecord(Guid marinaId, [FromBody] CreateVesselRecordRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();

        BoatType? boatType = null;
        if (request.VesselBoatType is not null)
        {
            if (!Enum.TryParse<BoatType>(request.VesselBoatType, ignoreCase: true, out var parsed))
                return ValidationProblem(new ValidationProblemDetails(
                    new Dictionary<string, string[]> { ["vesselBoatType"] = [$"Unknown boat type: {request.VesselBoatType}"] }));
            boatType = parsed;
        }

        try
        {
            var record = await createVesselRecord.HandleAsync(new CreateVesselRecordCommand(
                MarinaId:                  marinaId,
                RequestingUserId:          userContext.UserId!.Value,
                BillingAccountId:          request.BillingAccountId,
                VesselId:                  request.VesselId,
                ClaimEmail:                request.ClaimEmail,
                VesselName:                request.VesselName,
                VesselMake:                request.VesselMake,
                VesselModel:               request.VesselModel,
                VesselYear:                request.VesselYear,
                VesselLength:              request.VesselLength,
                VesselBeam:                request.VesselBeam,
                VesselDraft:               request.VesselDraft,
                VesselBoatType:            boatType,
                VesselHullColor:           request.VesselHullColor,
                VesselRegistrationNumber:  request.VesselRegistrationNumber,
                VesselRegistrationState:   request.VesselRegistrationState,
                InsuranceProvider:         request.InsuranceProvider,
                InsurancePolicyNumber:     request.InsurancePolicyNumber,
                InsuranceExpiresOn:        request.InsuranceExpiresOn,
                Notes:                     request.Notes), ct);

            return CreatedAtAction(nameof(GetVesselRecord), new { marinaId, recordId = record.Id }, record);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpPatch("marinas/{marinaId:guid}/vessel-records/{recordId:guid}")]
    [ProducesResponseType(typeof(VesselRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVesselRecord(Guid marinaId, Guid recordId, [FromBody] UpdateVesselRecordRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var record = await updateVesselRecord.HandleAsync(new UpdateVesselRecordCommand(
                Id:                    recordId,
                MarinaId:              marinaId,
                RequestingUserId:      userContext.UserId!.Value,
                BillingAccountId:      request.BillingAccountId,
                InsuranceProvider:     request.InsuranceProvider,
                InsurancePolicyNumber: request.InsurancePolicyNumber,
                InsuranceExpiresOn:    request.InsuranceExpiresOn,
                MarkInsuranceVerified: request.MarkInsuranceVerified,
                Notes:                 request.Notes), ct);
            return Ok(record);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}

// ---------- request records ----------

public sealed record CreateBillingAccountRequest(
    string DisplayName,
    string BillingEmail,
    string? BillingPhone,
    string? BillingAddressStreet,
    string? BillingAddressCity,
    string? BillingAddressState,
    string? BillingAddressZip,
    string? BillingAddressCountry,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes
);

public sealed record UpdateBillingAccountRequest(
    string? DisplayName,
    string? BillingEmail,
    string? BillingPhone,
    string? BillingAddressStreet,
    string? BillingAddressCity,
    string? BillingAddressState,
    string? BillingAddressZip,
    string? BillingAddressCountry,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes,
    bool? IsActive
);

public sealed record InviteMemberRequest(string Email, string Role);

public sealed record CreateVesselRecordRequest(
    Guid? BillingAccountId,
    // Link to existing vessel OR provide ghost vessel details below
    Guid? VesselId,
    // Ghost vessel fields
    string? ClaimEmail,
    string? VesselName,
    string? VesselMake,
    string? VesselModel,
    int? VesselYear,
    decimal? VesselLength,
    decimal? VesselBeam,
    decimal? VesselDraft,
    string? VesselBoatType,
    string? VesselHullColor,
    string? VesselRegistrationNumber,
    string? VesselRegistrationState,
    // Insurance
    string? InsuranceProvider,
    string? InsurancePolicyNumber,
    DateOnly? InsuranceExpiresOn,
    string? Notes
);

public sealed record UpdateVesselRecordRequest(
    Guid? BillingAccountId,
    string? InsuranceProvider,
    string? InsurancePolicyNumber,
    DateOnly? InsuranceExpiresOn,
    bool? MarkInsuranceVerified,
    string? Notes
);
