export interface PriceResolverPlan {
  transientRateKind: string | null | undefined;
  transientAmount: number | null | undefined;
  transientMinCharge: number | null | undefined;
  leaseRateKind: string | null | undefined;
  leaseAmount: number | null | undefined;
  leaseMinCharge: number | null | undefined;
  amenityAddOns: ReadonlyArray<{
    amenity: string;
    transientAmount: number | null | undefined;
    leaseAmount: number | null | undefined;
  }>;
}

export interface ResolvedPrices {
  transient: number | null;
  lease: number | null;
}

export function resolvePrice(
  plan: PriceResolverPlan,
  slipLength: number,
  slipBeam: number,
  amenities: readonly string[],
): ResolvedPrices {
  return {
    transient: resolveRate(
      plan.transientRateKind, plan.transientAmount, plan.transientMinCharge,
      slipLength, slipBeam, plan.amenityAddOns, amenities, 'transient',
    ),
    lease: resolveRate(
      plan.leaseRateKind, plan.leaseAmount, plan.leaseMinCharge,
      slipLength, slipBeam, plan.amenityAddOns, amenities, 'lease',
    ),
  };
}

function resolveRate(
  rateKind: string | null | undefined,
  amount: number | null | undefined,
  minCharge: number | null | undefined,
  length: number,
  beam: number,
  addOns: PriceResolverPlan['amenityAddOns'],
  amenities: readonly string[],
  side: 'transient' | 'lease',
): number | null {
  if (!rateKind || amount == null) return null;

  let base: number;
  if (rateKind === 'Flat') base = amount;
  else if (rateKind === 'PerFoot') base = amount * length;
  else if (rateKind === 'PerArea') base = amount * length * beam;
  else return null;

  for (const addOn of addOns) {
    if (!amenities.includes(addOn.amenity)) continue;
    const extra = side === 'transient' ? addOn.transientAmount : addOn.leaseAmount;
    if (extra != null) base += extra;
  }

  return Math.max(base, minCharge ?? 0);
}
