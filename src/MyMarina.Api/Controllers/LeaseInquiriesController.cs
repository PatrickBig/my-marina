using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Leases;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
public class LeaseInquiriesController(
    ICommandHandler<CreateLeaseInquiryCommand, LeaseInquiryDto> create,
    ICommandHandler<UpdateLeaseInquiryCommand, LeaseInquiryDto> update,
    ICommandHandler<ApproveLeaseInquiryCommand, LeaseInquiryDto> approve,
    ICommandHandler<DeclineLeaseInquiryCommand, LeaseInquiryDto> decline,
    ICommandHandler<WithdrawLeaseInquiryCommand, LeaseInquiryDto> withdraw,
    IQueryHandler<GetLeaseInquiriesQuery, IReadOnlyList<LeaseInquiryDto>> getMarinaInquiries,
    IQueryHandler<GetMyLeaseInquiriesQuery, IReadOnlyList<LeaseInquiryDto>> getMyInquiries,
    IUserContext userContext)
    : ControllerBase
{
    // POST /slips/{slipId}/lease-inquiries — boater submits a lease inquiry
    [HttpPost("slips/{slipId:guid}/lease-inquiries")]
    [ProducesResponseType(typeof(LeaseInquiryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid slipId, [FromBody] CreateLeaseInquiryRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<LeaseTerm>(request.DesiredTerm, ignoreCase: true, out var term))
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["desiredTerm"] = [$"Unknown lease term: {request.DesiredTerm}"] }));

        try
        {
            var dto = await create.HandleAsync(new CreateLeaseInquiryCommand(
                SlipId:           slipId,
                RequestingUserId: userContext.UserId!.Value,
                VesselId:         request.VesselId,
                DesiredTerm:      term,
                DesiredStartDate: request.DesiredStartDate,
                Message:          request.Message), ct);
            return CreatedAtAction(nameof(GetMyInquiries), dto);
        }
        catch (KeyNotFoundException e) { return NotFound(new { message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
    }

    // GET /me/lease-inquiries — boater's own inquiries
    [HttpGet("me/lease-inquiries")]
    [ProducesResponseType(typeof(IReadOnlyList<LeaseInquiryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyInquiries(CancellationToken ct)
    {
        var list = await getMyInquiries.HandleAsync(new GetMyLeaseInquiriesQuery(userContext.UserId!.Value), ct);
        return Ok(list);
    }

    // GET /marinas/{marinaId}/lease-inquiries — marina staff views all inquiries
    [HttpGet("marinas/{marinaId:guid}/lease-inquiries")]
    [ProducesResponseType(typeof(IReadOnlyList<LeaseInquiryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMarinaInquiries(
        Guid marinaId,
        [FromQuery] Guid? slipId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        var list = await getMarinaInquiries.HandleAsync(new GetLeaseInquiriesQuery(marinaId, slipId, status), ct);
        return Ok(list);
    }

    // PATCH /marinas/{marinaId}/lease-inquiries/{id} — marina staff edits agreed terms
    [HttpPatch("marinas/{marinaId:guid}/lease-inquiries/{id:guid}")]
    [ProducesResponseType(typeof(LeaseInquiryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid marinaId, Guid id, [FromBody] UpdateLeaseInquiryRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();

        RateKind? agreedRateKind = null;
        if (request.AgreedRateKind is not null)
        {
            if (!Enum.TryParse<RateKind>(request.AgreedRateKind, ignoreCase: true, out var parsed))
                return ValidationProblem(new ValidationProblemDetails(
                    new Dictionary<string, string[]> { ["agreedRateKind"] = [$"Unknown rate kind: {request.AgreedRateKind}"] }));
            agreedRateKind = parsed;
        }

        try
        {
            var dto = await update.HandleAsync(new UpdateLeaseInquiryCommand(
                InquiryId:           id,
                MarinaId:            marinaId,
                RequestingUserId:    userContext.UserId!.Value,
                AgreedRateKind:      agreedRateKind,
                AgreedBaseRate:      request.AgreedBaseRate,
                AssignmentStartDate: request.AssignmentStartDate,
                AssignmentEndDate:   request.AssignmentEndDate,
                MarinaNote:          request.MarinaNote,
                MarkUnderReview:     request.MarkUnderReview), ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
    }

    // POST /marinas/{marinaId}/lease-inquiries/{id}/approve
    [HttpPost("marinas/{marinaId:guid}/lease-inquiries/{id:guid}/approve")]
    [ProducesResponseType(typeof(LeaseInquiryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid marinaId, Guid id, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        try
        {
            var dto = await approve.HandleAsync(new ApproveLeaseInquiryCommand(id, marinaId, userContext.UserId!.Value), ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
    }

    // POST /marinas/{marinaId}/lease-inquiries/{id}/decline
    [HttpPost("marinas/{marinaId:guid}/lease-inquiries/{id:guid}/decline")]
    [ProducesResponseType(typeof(LeaseInquiryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Decline(Guid marinaId, Guid id, [FromBody] DeclineLeaseInquiryRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        try
        {
            var dto = await decline.HandleAsync(new DeclineLeaseInquiryCommand(id, marinaId, userContext.UserId!.Value, request.Reason), ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
    }

    // POST /lease-inquiries/{id}/withdraw — boater withdraws their own inquiry
    [HttpPost("lease-inquiries/{id:guid}/withdraw")]
    [ProducesResponseType(typeof(LeaseInquiryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken ct)
    {
        try
        {
            var dto = await withdraw.HandleAsync(new WithdrawLeaseInquiryCommand(id, userContext.UserId!.Value), ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
    }
}

// ─── Request records ─────────────────────────────────────────────────────────

public sealed record CreateLeaseInquiryRequest(
    Guid? VesselId,
    string DesiredTerm,
    DateOnly? DesiredStartDate,
    string? Message
);

public sealed record UpdateLeaseInquiryRequest(
    string? AgreedRateKind,
    decimal? AgreedBaseRate,
    DateOnly? AssignmentStartDate,
    DateOnly? AssignmentEndDate,
    string? MarinaNote,
    bool MarkUnderReview
);

public sealed record DeclineLeaseInquiryRequest(string? Reason);
