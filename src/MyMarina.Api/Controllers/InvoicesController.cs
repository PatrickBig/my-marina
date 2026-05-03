using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoicing;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
public class InvoicesController(
    ICommandHandler<CreateInvoiceCommand, InvoiceDto> createInvoice,
    ICommandHandler<AddLineItemCommand, InvoiceLineItemDto> addLineItem,
    ICommandHandler<RemoveLineItemCommand> removeLineItem,
    ICommandHandler<SendInvoiceCommand> sendInvoice,
    ICommandHandler<VoidInvoiceCommand> voidInvoice,
    ICommandHandler<RecordPaymentCommand, PaymentDto> recordPayment,
    IQueryHandler<GetInvoicesQuery, IReadOnlyList<InvoiceSummaryDto>> getInvoices,
    IQueryHandler<GetInvoiceQuery, InvoiceDto> getInvoice,
    IQueryHandler<GetMyInvoicesQuery, IReadOnlyList<InvoiceSummaryDto>> getMyInvoices,
    IUserContext userContext)
    : ControllerBase
{
    // ─── Marina-side endpoints ──────────────────────────────────────────────────

    // POST /marinas/{marinaId}/invoices
    [HttpPost("marinas/{marinaId:guid}/invoices")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateInvoice(Guid marinaId, [FromBody] CreateInvoiceRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId))
            return Forbid();

        var result = await createInvoice.HandleAsync(new CreateInvoiceCommand(
            MarinaId: marinaId,
            BillingAccountId: request.BillingAccountId,
            ReservationId: request.ReservationId,
            SlipAssignmentId: request.SlipAssignmentId,
            IssuedDate: request.IssuedDate,
            DueDate: request.DueDate,
            TaxAmount: request.TaxAmount,
            Notes: request.Notes,
            LineItems: request.LineItems.Select(l => new CreateInvoiceLineItemData(
                l.Description, l.Quantity, l.UnitPrice, l.SlipAssignmentId, l.ReservationId)).ToList(),
            RequestingUserId: userContext.UserId!.Value), ct);

        return CreatedAtAction(nameof(GetInvoice), new { marinaId, invoiceId = result.Id }, result);
    }

    // GET /marinas/{marinaId}/invoices
    [HttpGet("marinas/{marinaId:guid}/invoices")]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInvoices(
        Guid marinaId,
        [FromQuery] string? status,
        [FromQuery] Guid? billingAccountId,
        CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId))
            return Forbid();

        var result = await getInvoices.HandleAsync(new GetInvoicesQuery(marinaId, status, billingAccountId), ct);
        return Ok(result);
    }

    // GET /marinas/{marinaId}/invoices/{invoiceId}
    [HttpGet("marinas/{marinaId:guid}/invoices/{invoiceId:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoice(Guid marinaId, Guid invoiceId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId))
            return Forbid();

        try
        {
            var result = await getInvoice.HandleAsync(new GetInvoiceQuery(invoiceId, marinaId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    // POST /marinas/{marinaId}/invoices/{invoiceId}/line-items
    [HttpPost("marinas/{marinaId:guid}/invoices/{invoiceId:guid}/line-items")]
    [ProducesResponseType(typeof(InvoiceLineItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddLineItem(Guid marinaId, Guid invoiceId, [FromBody] AddLineItemRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId))
            return Forbid();

        try
        {
            var result = await addLineItem.HandleAsync(new AddLineItemCommand(
                InvoiceId: invoiceId,
                MarinaId: marinaId,
                Description: request.Description,
                Quantity: request.Quantity,
                UnitPrice: request.UnitPrice,
                SlipAssignmentId: request.SlipAssignmentId,
                ReservationId: request.ReservationId,
                RequestingUserId: userContext.UserId!.Value), ct);

            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    // DELETE /marinas/{marinaId}/invoices/{invoiceId}/line-items/{lineItemId}
    [HttpDelete("marinas/{marinaId:guid}/invoices/{invoiceId:guid}/line-items/{lineItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveLineItem(Guid marinaId, Guid invoiceId, Guid lineItemId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId))
            return Forbid();

        try
        {
            await removeLineItem.HandleAsync(new RemoveLineItemCommand(invoiceId, lineItemId, marinaId, userContext.UserId!.Value), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    // POST /marinas/{marinaId}/invoices/{invoiceId}/send
    [HttpPost("marinas/{marinaId:guid}/invoices/{invoiceId:guid}/send")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendInvoice(Guid marinaId, Guid invoiceId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId))
            return Forbid();

        try
        {
            await sendInvoice.HandleAsync(new SendInvoiceCommand(invoiceId, marinaId, userContext.UserId!.Value), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    // POST /marinas/{marinaId}/invoices/{invoiceId}/void
    [HttpPost("marinas/{marinaId:guid}/invoices/{invoiceId:guid}/void")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> VoidInvoice(Guid marinaId, Guid invoiceId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId))
            return Forbid();

        try
        {
            await voidInvoice.HandleAsync(new VoidInvoiceCommand(invoiceId, marinaId, userContext.UserId!.Value), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    // POST /marinas/{marinaId}/invoices/{invoiceId}/payments
    [HttpPost("marinas/{marinaId:guid}/invoices/{invoiceId:guid}/payments")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecordPayment(Guid marinaId, Guid invoiceId, [FromBody] RecordPaymentRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId))
            return Forbid();

        try
        {
            var result = await recordPayment.HandleAsync(new RecordPaymentCommand(
                InvoiceId: invoiceId,
                MarinaId: marinaId,
                Amount: request.Amount,
                PaidOn: request.PaidOn,
                Method: request.Method,
                ReferenceNumber: request.ReferenceNumber,
                Notes: request.Notes,
                RequestingUserId: userContext.UserId!.Value), ct);

            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    // ─── Boater (customer) side ─────────────────────────────────────────────────

    // GET /me/invoices
    [HttpGet("me/invoices")]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyInvoices(CancellationToken ct)
    {
        var billingAccountIds = userContext.BillingAccounts
            .Select(b => b.BillingAccountId)
            .ToList();

        var result = await getMyInvoices.HandleAsync(new GetMyInvoicesQuery(billingAccountIds), ct);
        return Ok(result);
    }
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public sealed record InvoiceLineItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    Guid? SlipAssignmentId,
    Guid? ReservationId);

public sealed record CreateInvoiceRequest(
    Guid BillingAccountId,
    Guid? ReservationId,
    Guid? SlipAssignmentId,
    DateOnly IssuedDate,
    DateOnly DueDate,
    decimal TaxAmount,
    string? Notes,
    IReadOnlyList<InvoiceLineItemRequest> LineItems);

public sealed record AddLineItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    Guid? SlipAssignmentId,
    Guid? ReservationId);

public sealed record RecordPaymentRequest(
    decimal Amount,
    DateOnly PaidOn,
    string Method,
    string? ReferenceNumber,
    string? Notes);
