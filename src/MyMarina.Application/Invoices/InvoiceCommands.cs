using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Invoices;

public sealed record CreateInvoiceCommand(
    Guid CustomerAccountId,
    DateOnly IssuedDate,
    DateOnly DueDate,
    string? Notes);

public sealed record UpdateInvoiceDraftCommand(
    Guid InvoiceId,
    DateOnly IssuedDate,
    DateOnly DueDate,
    string? Notes);

public sealed record AddLineItemCommand(
    Guid InvoiceId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    Guid? SlipAssignmentId);

public sealed record UpdateLineItemCommand(
    Guid InvoiceId,
    Guid LineItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public sealed record RemoveLineItemCommand(
    Guid InvoiceId,
    Guid LineItemId);

public sealed record SendInvoiceCommand(Guid InvoiceId);

public sealed record VoidInvoiceCommand(Guid InvoiceId);

public sealed record RecordPaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    DateOnly PaidOn,
    PaymentMethod Method,
    string? ReferenceNumber,
    string? Notes,
    Guid RecordedByUserId);

public interface ICreateInvoiceCommandHandler : ICommandHandler<CreateInvoiceCommand, Guid>;
public interface IUpdateInvoiceDraftCommandHandler : ICommandHandler<UpdateInvoiceDraftCommand>;
public interface IAddLineItemCommandHandler : ICommandHandler<AddLineItemCommand, Guid>;
public interface IUpdateLineItemCommandHandler : ICommandHandler<UpdateLineItemCommand>;
public interface IRemoveLineItemCommandHandler : ICommandHandler<RemoveLineItemCommand>;
public interface ISendInvoiceCommandHandler : ICommandHandler<SendInvoiceCommand>;
public interface IVoidInvoiceCommandHandler : ICommandHandler<VoidInvoiceCommand>;
public interface IRecordPaymentCommandHandler : ICommandHandler<RecordPaymentCommand, Guid>;
