namespace MyMarina.Application.PricingPlans;

public sealed record ListPricingPlansQuery(Guid MarinaId, Guid RequestingUserId);
public sealed record GetPricingPlanQuery(Guid PlanId, Guid MarinaId, Guid RequestingUserId);
