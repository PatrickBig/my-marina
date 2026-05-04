namespace MyMarina.Application.Leases;

// Marina: list all inquiries for a marina (optionally filtered by slip or status)
public sealed record GetLeaseInquiriesQuery(
    Guid MarinaId,
    Guid? SlipId,
    string? Status    // null = all
);

// Boater: my own submitted inquiries
public sealed record GetMyLeaseInquiriesQuery(Guid RequestingUserId);

// Single inquiry detail (accessible to marina staff and the requesting boater)
public sealed record GetLeaseInquiryQuery(Guid InquiryId, Guid RequestingUserId, bool IsMarinaStaff);
