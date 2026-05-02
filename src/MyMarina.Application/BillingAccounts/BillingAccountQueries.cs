namespace MyMarina.Application.BillingAccounts;

public sealed record GetBillingAccountsQuery(Guid MarinaId, Guid RequestingUserId);

public sealed record GetBillingAccountQuery(Guid Id, Guid MarinaId, Guid RequestingUserId);

public sealed record GetBillingAccountMembersQuery(Guid BillingAccountId, Guid MarinaId, Guid RequestingUserId);
