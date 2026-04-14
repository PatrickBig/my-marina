using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Common;
using MyMarina.Application.Customers;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("customers")]
[Authorize(Roles = $"{nameof(UserRole.MarinaOwner)},{nameof(UserRole.MarinaStaff)}")]
public class CustomersController(
    ICommandHandler<CreateCustomerAccountCommand, Guid> createHandler,
    ICommandHandler<UpdateCustomerAccountCommand> updateHandler,
    ICommandHandler<DeactivateCustomerAccountCommand> deactivateHandler,
    IQueryHandler<GetCustomerAccountsQuery, IReadOnlyList<CustomerAccountDto>> getAccountsHandler,
    IQueryHandler<GetCustomerAccountQuery, CustomerAccountDto?> getAccountHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerAccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var accounts = await getAccountsHandler.HandleAsync(new GetCustomerAccountsQuery(), ct);
        return Ok(accounts);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var account = await getAccountHandler.HandleAsync(new GetCustomerAccountQuery(id), ct);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerAccountCommand command, CancellationToken ct)
    {
        var id = await createHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        try
        {
            var command = new UpdateCustomerAccountCommand(
                id, request.DisplayName, request.BillingEmail, request.BillingPhone,
                request.BillingAddress, request.EmergencyContactName,
                request.EmergencyContactPhone, request.Notes);
            await updateHandler.HandleAsync(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = nameof(UserRole.MarinaOwner))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        try
        {
            await deactivateHandler.HandleAsync(new DeactivateCustomerAccountCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

public sealed record UpdateCustomerRequest(
    string DisplayName,
    string BillingEmail,
    string? BillingPhone,
    AddressDto? BillingAddress,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes);
