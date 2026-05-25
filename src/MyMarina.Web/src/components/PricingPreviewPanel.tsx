import { useState } from 'react';
import { resolvePrice, type PriceResolverPlan } from '@/utils/priceResolver';

export interface PricingPreviewPanelProps {
  transientRateKind: string;
  transientAmount: string;
  transientMinCharge: string;
  leaseRateKind: string;
  leaseAmount: string;
  leaseMinCharge: string;
  amenityAddOns: ReadonlyArray<{ amenity: string; transientAmount: string; leaseAmount: string }>;
  defaultExpanded?: boolean;
}

const AMENITY_LABELS: Record<string, string> = {
  Covered:     'Covered slip',
  Electric30A: '30A electric',
  Electric50A: '50A electric',
  HasWater:    'Water hookup',
  HasPumpOut:  'Pump-out',
};

function fmt(value: number): string {
  return value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export function PricingPreviewPanel({
  transientRateKind, transientAmount, transientMinCharge,
  leaseRateKind, leaseAmount, leaseMinCharge,
  amenityAddOns,
  defaultExpanded = true,
}: PricingPreviewPanelProps) {
  const [expanded, setExpanded] = useState(defaultExpanded);
  const [length, setLength] = useState(40);
  const [beam, setBeam] = useState(14);
  const [selectedAmenities, setSelectedAmenities] = useState<Set<string>>(new Set());

  // Only show toggles for add-ons that have at least one amount configured
  const activeAddOns = amenityAddOns.filter(
    (a) => a.transientAmount !== '' || a.leaseAmount !== ''
  );

  function toggleAmenity(amenity: string) {
    setSelectedAmenities((prev) => {
      const next = new Set(prev);
      if (next.has(amenity)) next.delete(amenity); else next.add(amenity);
      return next;
    });
  }

  const plan: PriceResolverPlan = {
    transientRateKind: transientRateKind || null,
    transientAmount: transientAmount !== '' ? parseFloat(transientAmount) : null,
    transientMinCharge: transientMinCharge !== '' ? parseFloat(transientMinCharge) : null,
    leaseRateKind: leaseRateKind || null,
    leaseAmount: leaseAmount !== '' ? parseFloat(leaseAmount) : null,
    leaseMinCharge: leaseMinCharge !== '' ? parseFloat(leaseMinCharge) : null,
    amenityAddOns: amenityAddOns
      .filter((a) => a.transientAmount !== '' || a.leaseAmount !== '')
      .map((a) => ({
        amenity: a.amenity,
        transientAmount: a.transientAmount !== '' ? parseFloat(a.transientAmount) : null,
        leaseAmount: a.leaseAmount !== '' ? parseFloat(a.leaseAmount) : null,
      })),
  };

  const { transient, lease } = resolvePrice(plan, length, beam, [...selectedAmenities]);

  function rateRow(label: string, value: number | null, rateKind: string, amount: string, unit: string) {
    if (value === null) {
      return (
        <div className="flex items-baseline justify-between">
          <span className="text-sm text-muted-foreground">{label}</span>
          <span className="text-sm text-muted-foreground">—</span>
        </div>
      );
    }
    const amtNum = amount !== '' ? parseFloat(amount) : null;
    const showFormula = (rateKind === 'PerFoot' || rateKind === 'PerArea') && amtNum != null;
    const formula =
      rateKind === 'PerFoot'
        ? `$${amtNum} × ${length}ft`
        : `$${amtNum} × ${length}ft × ${beam}ft`;

    return (
      <div className="space-y-0.5">
        <div className="flex items-baseline justify-between">
          <span className="text-sm text-muted-foreground">{label}</span>
          <span className="text-sm font-semibold text-foreground">
            ${fmt(value)} <span className="font-normal text-muted-foreground text-xs">/ {unit}</span>
          </span>
        </div>
        {showFormula && (
          <p className="text-xs text-muted-foreground/70 text-right">
            {formula} = ${fmt(value)}
          </p>
        )}
      </div>
    );
  }

  return (
    <div className="rounded-xl border border-border bg-muted/20">
      <button
        type="button"
        onClick={() => setExpanded((v) => !v)}
        className="w-full flex items-center justify-between px-4 py-3 text-sm font-medium text-foreground hover:bg-muted/30 transition-colors rounded-xl"
      >
        <span>Preview rates</span>
        <span className="text-muted-foreground text-xs">{expanded ? '▲' : '▼'}</span>
      </button>

      {expanded && (
        <div className="px-4 pb-4 space-y-4 border-t border-border/50">
          {/* Sample dimensions */}
          <div className="flex items-center gap-4 pt-3">
            <span className="text-xs font-medium text-muted-foreground shrink-0">Sample slip:</span>
            <div className="flex items-center gap-2">
              <label className="text-xs text-muted-foreground">Length</label>
              <input
                type="number" min="1" max="300" step="1" value={length}
                onChange={(e) => setLength(Math.max(1, Number(e.target.value) || 1))}
                className="w-16 rounded border border-border px-2 py-1 text-xs text-center focus:outline-none focus:ring-1 focus:ring-ring"
              />
              <span className="text-xs text-muted-foreground">ft</span>
            </div>
            <div className="flex items-center gap-2">
              <label className="text-xs text-muted-foreground">Beam</label>
              <input
                type="number" min="1" max="100" step="1" value={beam}
                onChange={(e) => setBeam(Math.max(1, Number(e.target.value) || 1))}
                className="w-16 rounded border border-border px-2 py-1 text-xs text-center focus:outline-none focus:ring-1 focus:ring-ring"
              />
              <span className="text-xs text-muted-foreground">ft</span>
            </div>
          </div>

          {/* Amenity toggles */}
          {activeAddOns.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {activeAddOns.map((a) => {
                const active = selectedAmenities.has(a.amenity);
                return (
                  <button
                    key={a.amenity}
                    type="button"
                    onClick={() => toggleAmenity(a.amenity)}
                    className={`rounded-full px-2.5 py-0.5 text-xs font-medium border transition-colors ${
                      active
                        ? 'bg-foreground text-background border-foreground'
                        : 'border-border text-foreground/70 hover:bg-muted/30'
                    }`}
                  >
                    {AMENITY_LABELS[a.amenity] ?? a.amenity}
                  </button>
                );
              })}
              <span className="text-xs text-muted-foreground self-center">toggle amenities to include add-ons</span>
            </div>
          )}

          {/* Computed rates */}
          <div className="rounded-lg bg-card border border-border px-4 py-3 space-y-2">
            {rateRow('Transient', transient, transientRateKind, transientAmount, 'night')}
            {rateRow('Lease', lease, leaseRateKind, leaseAmount, 'month')}
          </div>
        </div>
      )}
    </div>
  );
}
