import { useEffect, useState } from 'react';
import { MapContainer, TileLayer, Marker } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { NavBar } from '@/components/NavBar';
import { getPublicSlipDetail, type SlipDetailDto, type PublicWindowSummaryDto } from '@/api/api';

delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

function getSlipIdFromPath(): string | null {
  const m = window.location.pathname.match(/^\/slips\/([^/]+)/);
  return m ? m[1] : null;
}

function formatDateRange(startsAt: string, endsAt: string) {
  const fmt = (d: string) => new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  return `${fmt(startsAt)} – ${fmt(endsAt)}`;
}

function WindowCard({ w, nights }: { w: PublicWindowSummaryDto; nights: number }) {
  const discount = nights >= 28 && w.monthlyDiscount
    ? w.monthlyDiscount
    : nights >= 7 && w.weeklyDiscount
    ? w.weeklyDiscount
    : 0;
  const base     = w.basePricePerNight * nights;
  const subtotal = base * (1 - discount);
  const total    = subtotal + (w.cleaningFee ?? 0);

  return (
    <div className="bg-white rounded-xl border border-slate-200 p-5">
      <div className="flex justify-between items-start">
        <div>
          <p className="text-sm font-medium text-slate-700">{formatDateRange(w.startsAt, w.endsAt)}</p>
          <div className="flex gap-2 mt-1">
            {w.instantBook && (
              <span className="text-xs px-1.5 py-0.5 rounded-full bg-blue-50 text-blue-700">Instant book</span>
            )}
            {w.minNights && (
              <span className="text-xs px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-600">
                Min {w.minNights}n
              </span>
            )}
            {w.maxNights && (
              <span className="text-xs px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-600">
                Max {w.maxNights}n
              </span>
            )}
          </div>
        </div>
        <div className="text-right">
          <p className="text-lg font-bold text-slate-800">
            ${w.basePricePerNight.toFixed(2)}
            <span className="text-xs font-normal text-slate-500">/night</span>
          </p>
          {nights > 1 && (
            <p className="text-xs text-slate-500 mt-0.5">
              {nights}n · ${total.toFixed(2)} est. total
              {discount > 0 && (
                <span className="ml-1 text-emerald-600">({(discount * 100).toFixed(0)}% off)</span>
              )}
            </p>
          )}
          {w.cleaningFee && (
            <p className="text-xs text-slate-400">+${w.cleaningFee.toFixed(2)} cleaning fee</p>
          )}
        </div>
      </div>
      <button
        disabled
        className="mt-4 w-full rounded-lg bg-slate-800 text-white py-2 text-sm font-medium opacity-40 cursor-not-allowed"
        title="Booking coming in Phase 9"
      >
        {w.instantBook ? 'Book now' : 'Request to book'} — coming soon
      </button>
    </div>
  );
}

export function SlipDetailPage() {
  const slipId = getSlipIdFromPath();
  const [slip, setSlip] = useState<SlipDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  // Pre-fill dates from URL params if present (?arrivesAt=...&departsAt=...)
  const params = new URLSearchParams(window.location.search);
  const arrivesAt = params.get('arrivesAt') ?? new Date().toISOString().slice(0, 10);
  const departsAt = params.get('departsAt') ?? new Date(Date.now() + 86400000).toISOString().slice(0, 10);
  const nights = Math.max(
    1,
    Math.round((new Date(departsAt).getTime() - new Date(arrivesAt).getTime()) / 86400000)
  );

  useEffect(() => {
    if (!slipId) { setNotFound(true); setLoading(false); return; }
    getPublicSlipDetail(slipId)
      .then(setSlip)
      .catch(() => setNotFound(true))
      .finally(() => setLoading(false));
  }, [slipId]);

  if (loading) return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center text-slate-400 text-sm">
      Loading…
    </div>
  );

  if (notFound || !slip) return (
    <div className="min-h-screen bg-slate-50">
      <NavBar />
      <div className="max-w-4xl mx-auto py-16 px-4 text-center">
        <p className="text-slate-500 text-sm">Slip not found or not available.</p>
        <a href="/search" className="mt-3 inline-block text-sm text-slate-600 underline">← Back to search</a>
      </div>
    </div>
  );

  const hasLocation = slip.latitude != null && slip.longitude != null;

  return (
    <div className="min-h-screen bg-slate-50">
      <NavBar />
      <div className="max-w-5xl mx-auto py-8 px-4">
        <a href="/search" className="text-sm text-slate-500 hover:text-slate-800 underline">← Back to search</a>

        <div className="mt-4 grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Left: slip info + map */}
          <div className="lg:col-span-2 space-y-4">
            <div className="bg-white rounded-xl border border-slate-200 p-6">
              <h1 className="text-xl font-bold text-slate-800">{slip.name}</h1>
              <p className="text-sm text-slate-500 mt-0.5">
                {slip.marinaName}
                {slip.addressCity && ` · ${slip.addressCity}${slip.addressState ? `, ${slip.addressState}` : ''}`}
              </p>

              <dl className="mt-4 grid grid-cols-2 gap-y-3 gap-x-6 text-sm">
                <div>
                  <dt className="text-xs text-slate-400 font-medium uppercase tracking-wide">Type</dt>
                  <dd className="mt-0.5 text-slate-700">{slip.slipType}</dd>
                </div>
                <div>
                  <dt className="text-xs text-slate-400 font-medium uppercase tracking-wide">Max LOA</dt>
                  <dd className="mt-0.5 text-slate-700">{slip.maxLength}′</dd>
                </div>
                <div>
                  <dt className="text-xs text-slate-400 font-medium uppercase tracking-wide">Max Beam</dt>
                  <dd className="mt-0.5 text-slate-700">{slip.maxBeam}′</dd>
                </div>
                <div>
                  <dt className="text-xs text-slate-400 font-medium uppercase tracking-wide">Max Draft</dt>
                  <dd className="mt-0.5 text-slate-700">{slip.maxDraft}′</dd>
                </div>
                <div>
                  <dt className="text-xs text-slate-400 font-medium uppercase tracking-wide">Water</dt>
                  <dd className="mt-0.5 text-slate-700">{slip.hasWater ? 'Yes' : 'No'}</dd>
                </div>
                <div>
                  <dt className="text-xs text-slate-400 font-medium uppercase tracking-wide">Electric</dt>
                  <dd className="mt-0.5 text-slate-700">
                    {slip.hasElectric ? `${slip.electric ?? ''}A` : 'No'}
                  </dd>
                </div>
              </dl>

              {slip.marinaDescription && (
                <p className="mt-4 text-sm text-slate-600 leading-relaxed">{slip.marinaDescription}</p>
              )}
              {slip.marinaPhoneNumber && (
                <p className="mt-2 text-sm text-slate-500">
                  Phone: <a href={`tel:${slip.marinaPhoneNumber}`} className="underline">{slip.marinaPhoneNumber}</a>
                </p>
              )}
            </div>

            {hasLocation && (
              <div className="h-64 rounded-xl overflow-hidden border border-slate-200 shadow-sm">
                <MapContainer
                  center={[slip.latitude!, slip.longitude!]}
                  zoom={14}
                  style={{ height: '100%', width: '100%' }}
                  zoomControl={false}
                >
                  <TileLayer
                    attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
                    url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                  />
                  <Marker position={[slip.latitude!, slip.longitude!]} />
                </MapContainer>
              </div>
            )}
          </div>

          {/* Right: availability windows */}
          <div className="space-y-3">
            <h2 className="text-base font-semibold text-slate-800">Available windows</h2>
            {slip.openWindows.length === 0 ? (
              <p className="text-sm text-slate-400">No open listing windows right now.</p>
            ) : (
              slip.openWindows.map((w) => (
                <WindowCard key={w.id} w={w} nights={nights} />
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
