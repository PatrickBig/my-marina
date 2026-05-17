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

function SlipRow({ slip, onClick }: { slip: SlipSearchResultDto; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="w-full text-left bg-white rounded-xl border border-slate-200 p-4 hover:shadow-md hover:border-slate-400 transition-all">
      <div className="flex justify-between items-start gap-4">
        <div className="min-w-0">
          <p className="text-sm font-semibold text-slate-800 truncate">{slip.slipName}</p>
          <p className="text-xs text-slate-500 mt-0.5">
            {slip.slipType}
            {' · '}LOA {slip.maxLength}′ · Beam {slip.maxBeam}′ · Draft {slip.maxDraft}′
          </p>
          <div className="flex gap-2 mt-1.5 flex-wrap">
            {slip.hasElectric && (
              <span className="text-xs px-1.5 py-0.5 rounded-full bg-amber-50 text-amber-700">Electric</span>
            )}
            {slip.hasWater && (
              <span className="text-xs px-1.5 py-0.5 rounded-full bg-sky-50 text-sky-700">Water</span>
            )}
            {slip.instantBook && (
              <span className="text-xs px-1.5 py-0.5 rounded-full bg-blue-50 text-blue-700">Instant book</span>
            )}
            {slip.minNights != null && (
              <span className="text-xs px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-500">Min {slip.minNights}n</span>
            )}
          </div>
        </div>
        <div className="text-right shrink-0">
          <p className="text-base font-bold text-slate-800">
            {formatRate(slip.rateKind, slip.basePricePerNight, slip.listingKind, slip.leaseTerm)}
          </p>
          {slip.cleaningFee != null && (
            <p className="text-xs text-slate-400">+${slip.cleaningFee.toFixed(2)} cleaning</p>
          )}
          {slip.minCharge != null && (
            <p className="text-xs text-slate-400">min ${slip.minCharge.toFixed(2)}/night</p>
          )}
        </div>
      </div>
    </button>
  );
}

export function MarinaSlipsPage() {
  const marinaId = getMarinaIdFromPath();
  const params   = new URLSearchParams(window.location.search);

  const listingKind = (params.get('listingKind') ?? 'Transient') as ListingKind;
  const arrivesAt   = params.get('arrivesAt') ?? undefined;
  const departsAt   = params.get('departsAt') ?? undefined;
  const leaseTerm   = (params.get('leaseTerm') ?? undefined) as LeaseTerm | undefined;
  const vesselLength = params.get('vesselLength') ? Number(params.get('vesselLength')) : undefined;
  const vesselBeam   = params.get('vesselBeam')   ? Number(params.get('vesselBeam'))   : undefined;
  const vesselDraft  = params.get('vesselDraft')  ? Number(params.get('vesselDraft'))  : undefined;

  const [slips, setSlips]     = useState<SlipSearchResultDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [error, setError]     = useState<string | null>(null);

  useEffect(() => {
    if (!marinaId) { setNotFound(true); setLoading(false); return; }
    searchSlipsAtMarina(marinaId, {
      listingKind, arrivesAt, departsAt, leaseTerm,
      vesselLength, vesselBeam, vesselDraft,
    })
      .then(setSlips)
      .catch((e: unknown) => {
        const status = (e as { response?: { status?: number } })?.response?.status;
        if (status === 404) setNotFound(true);
        else setError('Could not load slips. Make sure the API is running.');
      })
      .finally(() => setLoading(false));
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [marinaId]);

  const marina     = slips[0];
  const marinaName = marina?.marinaName ?? 'Marina';
  const marinaCity = marina?.marinaCity;
  const marinaState= marina?.marinaState;
  const mapPos     = marina ? [marina.latitude, marina.longitude] as [number, number] : null;

  const backUrl = buildBackUrl(params);

  function navigateToSlip(slip: SlipSearchResultDto) {
    const qs = new URLSearchParams();
    if (arrivesAt) qs.set('arrivesAt', arrivesAt);
    if (departsAt) qs.set('departsAt', departsAt);
    window.location.href = `/slips/${slip.slipId}?${qs.toString()}`;
  }

  if (loading) return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center text-slate-400 text-sm">Loading…</div>
  );

  if (notFound) return (
    <div className="min-h-screen bg-slate-50">
      <NavBar />
      <div className="max-w-4xl mx-auto py-16 px-4 text-center">
        <p className="text-slate-500 text-sm">Marina not found or not available.</p>
        <a href="/search" className="mt-3 inline-block text-sm text-slate-600 underline">← Back to search</a>
      </div>
    </div>
  );

  return (
    <div className="min-h-screen bg-slate-50">
      <NavBar />

      <div className="bg-white border-b border-slate-200 shadow-sm">
        <div className="max-w-6xl mx-auto px-4 py-4">
          <a href={backUrl} className="text-sm text-slate-500 hover:text-slate-800 underline">← Back to marinas</a>
          <h1 className="mt-2 text-xl font-bold text-slate-800">{marinaName}</h1>
          {(marinaCity || marinaState) && (
            <p className="text-sm text-slate-500 mt-0.5">
              {marinaCity}{marinaState ? `, ${marinaState}` : ''}
            </p>
          )}
        </div>
      </div>

      <div className="max-w-6xl mx-auto px-4 py-6">
        {error && <p className="text-sm text-red-600 mb-4">{error}</p>}

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Map — marina pin only */}
          <div className="h-[420px] rounded-xl overflow-hidden border border-slate-200 shadow-sm">
            {mapPos ? (
              <MapContainer center={mapPos} zoom={14} style={{ height: '100%', width: '100%' }}>
                <TileLayer
                  attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
                  url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                />
                <Marker position={mapPos} />
              </MapContainer>
            ) : (
              <div className="h-full flex items-center justify-center text-slate-400 text-sm bg-slate-100">
                No location data
              </div>
            )}
          </div>

          {/* Slip list */}
          <div className="space-y-3 overflow-y-auto max-h-[420px] pr-1">
            {slips.length === 0 && !error && (
              <p className="text-sm text-slate-400 pt-2">
                No {listingKind === 'Lease' ? 'lease listings' : 'available slips'} match your filters at this marina.
              </p>
            )}
            {slips.map((slip) => (
              <SlipRow key={slip.slipId} slip={slip} onClick={() => navigateToSlip(slip)} />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
