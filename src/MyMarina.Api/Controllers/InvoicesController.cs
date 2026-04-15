using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoices;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("invoices")]
[Authorize(Roles = $"{nameof(UserRole.MarinaOwner)},{nameof(UserRole.MarinaStaff)}")]
public class InvoicesController(
    ICommandHandler<CreateInvoiceCommand, Guid> createHandler,
    ICommandHandler<UpdateInvoiceDraftCommand> updateDraftHandler,
    ICommandHandler<AddLineItemCommand, Guid> addLineItemHandler,
    ICommandHandler<UpdateLineItemCommand> updateLineItemHandler,
    ICommandHandler<RemoveLineItemCommand> removeLineItemHandler,
    ICommandHandler<SendInvoiceCommand> sendHandler,
    ICommandHandler<VoidInvoiceCommand> voidHandler,
    ICommandHandler<RecordPaymentCommand, Guid> recordPaymentHandler,
    IQueryHandler<GetInvoicesQuery, IReadOnlyList<InvoiceDto>> getInvoicesHandler,
    IQueryHandler<GetInvoiceQuery, InvoiceDetailDto?> getInvoiceHandler) : ControllerBase
{
    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? customerAccountId,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] DateOnly? issuedFrom,
        [FromQuery] DateOnly? issuedTo,
        CancellationToken ct = default)
    {
        var invoices = await getInvoicesHandler.HandleAsync(
            new GetInvoicesQuery(customerAccountId, status, issuedFrom, issuedTo), ct);
        return Ok(invoices);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var invoice = await getInvoiceHandler.HandleAsync(new GetInvoiceQuery(id), ct);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    // ── Invoice lifecycle ─────────────────────────────────────────────────────

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceCommand command, CancellationToken ct)
    {
        try
        {
            var id = await createHandler.HandleAsync(command, ct);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDraft(Guid id, [FromBody] UpdateInvoiceDraftRequest request, CancellationToken ct)
    {
        try
        {
            await updateDraftHandler.HandleAsync(
                new UpdateInvoiceDraftCommand(id, request.IssuedDate, request.DueDate, request.Notes), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/send")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Send(Guid id, CancellationToken ct)
    {
        try
        {
            await sendHandler.HandleAsync(new SendInvoiceCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/void")]
    [Authorize(Roles = nameof(UserRole.MarinaOwner))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Void(Guid id, CancellationToken ct)
    {
        try
        {
            await voidHandler.HandleAsync(new VoidInvoiceCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // ── Line items ────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/line-items")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddLineItem(Guid id, [FromBody] AddLineItemRequest request, CancellationToken ct)
    {
        try
        {
            var lineItemId = await addLineItemHandler.HandleAsync(
                new AddLineItemCommand(id, request.Description, request.Quantity, request.UnitPrice, request.SlipAssignmentId),
                ct);
            return CreatedAtAction(nameof(GetById), new { id }, lineItemId);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}/line-items/{lineItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLineItem(
        Guid id, Guid lineItemId, [FromBody] UpdateLineItemRequest request, CancellationToken ct)
    {
        try
        {
            await updateLineItemHandler.HandleAsync(
                new UpdateLineItemCommand(id, lineItemId, request.Description, request.Quantity, request.UnitPrice),
                ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/line-items/{lineItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveLineItem(Guid id, Guid lineItemId, CancellationToken ct)
    {
        try
        {
            await removeLineItemHandler.HandleAsync(new RemoveLineItemCommand(id, lineItemId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // ── Payments ──────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/payments")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        try
        {
            var paymentId = await recordPaymentHandler.HandleAsync(
                new RecordPaymentCommand(id, request.Amount, request.PaidOn, request.Method,
                    request.ReferenceNumber, request.Notes, userId),
                ct);
            return CreatedAtAction(nameof(GetById), new { id }, paymentId);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message.Contains("exceeds")
                ? BadRequest(new { message = ex.Message })
                : Conflict(new { message = ex.Message });
        }
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public sealed record UpdateInvoiceDraftRequest(DateOnly IssuedDate, DateOnly DueDate, string? Notes);

public sealed record AddLineItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    Guid? SlipAssignmentId);

public sealed record UpdateLineItemRequest(string Description, decimal Quantity, decimal UnitPrice);

public sealed record RecordPaymentRequest(
    decimal Amount,
    DateOnly PaidOn,
    PaymentMethod Method,
    string? ReferenceNumber,
    string? Notes);
