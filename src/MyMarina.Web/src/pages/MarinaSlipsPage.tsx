import { useEffect, useState } from 'react';
import { MapContainer, TileLayer, Marker } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { NavBar } from '@/components/NavBar';
import { searchSlipsAtMarina, type SlipSearchResultDto, type ListingKind, type LeaseTerm, type RateKind } from '@/api/api';

delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

function getMarinaIdFromPath(): string | null {
  const m = window.location.pathname.match(/^\/search\/marinas\/([^/]+)/);
  return m ? m[1] : null;
}

function formatRate(rateKind: RateKind, price: number, listingKind: ListingKind, leaseTerm?: LeaseTerm | null): string {
  if (listingKind === 'Lease') {
    const period = leaseTerm === 'Monthly' ? '/mo' : leaseTerm === 'Seasonal' ? '/season' : leaseTerm === 'Annual' ? '/yr' : '/mo';
    return rateKind === 'PerFoot' ? `$${price}/ft${period}` : `$${price.toLocaleString()}${period}`;
  }
  return rateKind === 'PerFoot' ? `$${price}/ft/night` : `$${price.toLocaleString()}/night`;
}

function buildBackUrl(params: URLSearchParams): string {
  const keep = ['listingKind', 'arrivesAt', 'departsAt', 'leaseTerm', 'vesselLength', 'vesselBeam', 'vesselDraft'];
  const out = new URLSearchParams();
  keep.forEach((k) => { const v = params.get(k); if (v) out.set(k, v); });
  const qs = out.toString();
  return qs ? `/search?${qs}` : '/search';
}

function SlipPhotoPlaceholder() {
  return (
    <div
      className="w-20 h-16 shrink-0 rounded-lg flex items-center justify-center text-white/70 text-lg"
      style={{ background: 'linear-gradient(135deg, oklch(52% 0.18 235) 0%, oklch(80% 0.10 185) 100%)' }}
    >
      ⚓
    </div>
  );
}

function FilterChip({ label, active, count, onClick }: { label: string; active: boolean; count?: number; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`text-xs px-3 py-1 rounded-full border font-medium transition-colors ${
        active
          ? 'bg-primary text-primary-foreground border-primary'
          : 'bg-background text-muted-foreground border-border hover:border-primary/50 hover:text-foreground'
      }`}
    >
      {label}{active && count !== undefined ? ` (${count})` : ''}
    </button>
  );
}

function SlipRow({ slip, onClick }: { slip: SlipSearchResultDto; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="w-full text-left bg-card rounded-xl border border-border p-3 hover:shadow-md hover:border-primary/30 transition-all">
      <div className="flex gap-3 items-start">
        <SlipPhotoPlaceholder />
        <div className="flex-1 min-w-0">
          <div className="flex justify-between items-start gap-2">
            <p className="text-sm font-semibold text-foreground truncate">{slip.slipName}</p>
            <p className="text-base font-bold text-foreground shrink-0">
              {formatRate(slip.rateKind, slip.basePricePerNight, slip.listingKind, slip.leaseTerm)}
            </p>
          </div>
          <p className="text-xs text-muted-foreground mt-0.5">
            {slip.slipType} · LOA {slip.maxLength}′ · Beam {slip.maxBeam}′ · Draft {slip.maxDraft}′
          </p>
          <div className="flex gap-1.5 mt-1.5 flex-wrap">
            {slip.instantBook && (
              <span className="text-xs px-1.5 py-0.5 rounded-full bg-primary/10 text-primary font-medium">Instant book</span>
            )}
            {slip.hasElectric && (
              <span className="text-xs px-1.5 py-0.5 rounded-full bg-amber-50 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400">Electric</span>
            )}
            {slip.hasWater && (
              <span className="text-xs px-1.5 py-0.5 rounded-full bg-sky-50 text-sky-700 dark:bg-sky-900/30 dark:text-sky-400">Water</span>
            )}
            {slip.minNights != null && (
              <span className="text-xs px-1.5 py-0.5 rounded-full bg-muted text-muted-foreground">Min {slip.minNights}n</span>
            )}
          </div>
          {(slip.cleaningFee != null || slip.minCharge != null) && (
            <p className="text-xs text-muted-foreground mt-1">
              {slip.cleaningFee != null && `+$${slip.cleaningFee.toFixed(2)} cleaning`}
              {slip.cleaningFee != null && slip.minCharge != null && ' · '}
              {slip.minCharge != null && `min $${slip.minCharge.toFixed(2)}/night`}
            </p>
          )}
        </div>
      </div>
    </button>
  );
}

export function MarinaSlipsPage() {
  const marinaId = getMarinaIdFromPath();
  const params   = new URLSearchParams(window.location.search);

  const listingKind  = (params.get('listingKind') ?? 'Transient') as ListingKind;
  const arrivesAt    = params.get('arrivesAt') ?? undefined;
  const departsAt    = params.get('departsAt') ?? undefined;
  const leaseTerm    = (params.get('leaseTerm') ?? undefined) as LeaseTerm | undefined;
  const vesselLength = params.get('vesselLength') ? Number(params.get('vesselLength')) : undefined;
  const vesselBeam   = params.get('vesselBeam')   ? Number(params.get('vesselBeam'))   : undefined;
  const vesselDraft  = params.get('vesselDraft')  ? Number(params.get('vesselDraft'))  : undefined;

  const [slips, setSlips]       = useState<SlipSearchResultDto[]>([]);
  const [loading, setLoading]   = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [error, setError]       = useState<string | null>(null);

  // Filter chips — wired to the existing API filter params
  const [chipElectric, setChipElectric] = useState(false);
  const [chipWater, setChipWater]       = useState(false);

  const [activeFilters, setActiveFilters] = useState<{ hasElectric?: boolean; hasWater?: boolean }>({});

  function applyFilters(electric: boolean, water: boolean) {
    setActiveFilters({
      hasElectric: electric || undefined,
      hasWater: water || undefined,
    });
  }

  useEffect(() => {
    if (!marinaId) { setNotFound(true); setLoading(false); return; }
    setLoading(true);
    searchSlipsAtMarina(marinaId, {
      listingKind, arrivesAt, departsAt, leaseTerm,
      vesselLength, vesselBeam, vesselDraft,
      ...activeFilters,
    })
      .then(setSlips)
      .catch((e: unknown) => {
        const status = (e as { response?: { status?: number } })?.response?.status;
        if (status === 404) setNotFound(true);
        else setError('Could not load slips. Make sure the API is running.');
      })
      .finally(() => setLoading(false));
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [marinaId, activeFilters]);

  const marina      = slips[0];
  const marinaName  = marina?.marinaName ?? 'Marina';
  const marinaCity  = marina?.marinaCity;
  const marinaState = marina?.marinaState;
  const mapPos      = marina ? [marina.latitude, marina.longitude] as [number, number] : null;
  const backUrl     = buildBackUrl(params);

  const electricCount = slips.filter(s => s.hasElectric).length;
  const waterCount    = slips.filter(s => s.hasWater).length;

  function navigateToSlip(slip: SlipSearchResultDto) {
    const qs = new URLSearchParams();
    if (arrivesAt) qs.set('arrivesAt', arrivesAt);
    if (departsAt) qs.set('departsAt', departsAt);
    window.location.href = `/slips/${slip.slipId}?${qs.toString()}`;
  }

  if (loading) return (
    <div className="min-h-screen bg-background flex items-center justify-center text-muted-foreground text-sm">Loading…</div>
  );

  if (notFound) return (
    <div className="min-h-screen bg-background">
      <NavBar />
      <div className="max-w-4xl mx-auto py-16 px-4 text-center">
        <p className="text-muted-foreground text-sm">Marina not found or not available.</p>
        <a href="/search" className="mt-3 inline-block text-sm text-primary underline">← Back to search</a>
      </div>
    </div>
  );

  return (
    <div className="flex flex-col h-screen bg-background overflow-hidden">
      <NavBar />

      {/* Header bar */}
      <div className="bg-card border-b border-border shadow-sm shrink-0">
        <div className="max-w-6xl mx-auto px-4 py-3">
          <a href={backUrl} className="text-sm text-muted-foreground hover:text-foreground underline transition-colors">← Back to marinas</a>
          <div className="flex items-baseline gap-3 mt-1">
            <h1 className="text-xl font-bold text-foreground">{marinaName}</h1>
            {(marinaCity || marinaState) && (
              <span className="text-sm text-muted-foreground">
                {marinaCity}{marinaState ? `, ${marinaState}` : ''}
              </span>
            )}
            {!loading && (
              <span className="ml-auto text-xs font-medium px-2 py-0.5 rounded-full bg-primary/10 text-primary">
                {slips.length} slip{slips.length !== 1 ? 's' : ''} fit
              </span>
            )}
          </div>
          {/* Filter chips */}
          <div className="flex gap-2 mt-2 flex-wrap">
            <FilterChip
              label="Electric" active={chipElectric} count={electricCount}
              onClick={() => { const next = !chipElectric; setChipElectric(next); applyFilters(next, chipWater); }}
            />
            <FilterChip
              label="Water" active={chipWater} count={waterCount}
              onClick={() => { const next = !chipWater; setChipWater(next); applyFilters(chipElectric, next); }}
            />
          </div>
        </div>
      </div>

      {/* Split panel */}
      <div className="flex flex-1 overflow-hidden flex-col lg:flex-row">
        {/* Slip list */}
        <div className="w-full lg:w-[420px] shrink-0 overflow-y-auto border-r border-border p-3 space-y-2 lg:h-full h-80">
          {error && <p className="text-sm text-destructive pb-2">{error}</p>}
          {slips.length === 0 && !error && !loading && (
            <p className="text-sm text-muted-foreground pt-2">
              No {listingKind === 'Lease' ? 'lease listings' : 'available slips'} match your filters at this marina.
            </p>
          )}
          {slips.map((slip) => (
            <SlipRow key={slip.slipId} slip={slip} onClick={() => navigateToSlip(slip)} />
          ))}
        </div>

        {/* Map + marina blurb */}
        <div className="relative flex-1">
          {mapPos ? (
            <MapContainer center={mapPos} zoom={14} style={{ height: '100%', width: '100%' }}>
              <TileLayer
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
              />
              <Marker position={mapPos} />
            </MapContainer>
          ) : (
            <div className="h-full flex items-center justify-center text-muted-foreground text-sm bg-muted/30">
              No location data
            </div>
          )}

        </div>
      </div>
    </div>
  );
}
