import { useEffect, useState } from 'react';
import { MapContainer, TileLayer, Marker } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { NavBar } from '@/components/NavBar';
import {
  getPublicSlipDetail, getVessels, createReservation, createLeaseInquiry,
  type SlipDetailDto, type PublicWindowSummaryDto, type VesselDto, type LeaseTerm,
} from '@/api/api';
import { useAuthStore } from '@/store/authStore';

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

function fmtDate(d: string) {
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function rateLabel(kind: string | null | undefined, rate: number | null | undefined, term?: string | null): string {
  if (!rate) return '';
  if (kind === 'PerFoot') {
    const period = term === 'Monthly' ? '/ft/mo' : term === 'Seasonal' ? '/ft/season' : term === 'Annual' ? '/ft/yr' : '/ft/night';
    return `$${rate}${period}`;
  }
  const period = term === 'Monthly' ? '/mo' : term === 'Seasonal' ? '/season' : term === 'Annual' ? '/yr' : '/night';
  return `$${rate.toFixed(2)}${period}`;
}

// ─── Transient window card ────────────────────────────────────────────────────

function TransientWindowCard({
  w, nights, arrivesAt, departsAt, vessels, isAuthenticated, onBooked,
}: {
  w: PublicWindowSummaryDto; nights: number; arrivesAt: string; departsAt: string;
  vessels: VesselDto[]; isAuthenticated: boolean; onBooked: (id: string) => void;
}) {
  const discount = nights >= 28 && w.monthlyDiscount ? w.monthlyDiscount
    : nights >= 7 && w.weeklyDiscount ? w.weeklyDiscount : 0;
  const base     = w.basePricePerNight * nights;
  const subtotal = base * (1 - discount);
  const total    = subtotal + (w.cleaningFee ?? 0);

  const [selectedVesselId, setSelectedVesselId] = useState(vessels[0]?.id ?? '');
  const [notes, setNotes] = useState('');
  const [booking, setBooking] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [booked, setBooked] = useState(false);

  async function handleBook() {
    if (!selectedVesselId) { setError('Select a vessel.'); return; }
    setError(null); setBooking(true);
    try {
      await createReservation({
        vesselId: selectedVesselId, availabilityWindowId: w.id,
        arrivesAt: new Date(arrivesAt).toISOString(), departsAt: new Date(departsAt).toISOString(),
        notes: notes || null,
      });
      setBooked(true); onBooked(w.id);
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg ?? 'Could not complete booking. Try again.');
    } finally { setBooking(false); }
  }

  return (
    <div className="bg-card rounded-xl border border-border p-5">
      <div className="flex justify-between items-start">
        <div>
          <p className="text-sm font-medium text-foreground">{fmtDate(w.startsAt)} – {fmtDate(w.endsAt)}</p>
          <div className="flex gap-2 mt-1 flex-wrap">
            {w.instantBook && <span className="text-xs px-1.5 py-0.5 rounded-full bg-blue-50 text-blue-700">Instant book</span>}
            {w.minNights && <span className="text-xs px-1.5 py-0.5 rounded-full bg-muted text-foreground/80">Min {w.minNights}n</span>}
            {w.maxNights && <span className="text-xs px-1.5 py-0.5 rounded-full bg-muted text-foreground/80">Max {w.maxNights}n</span>}
          </div>
        </div>
        <div className="text-right">
          <p className="text-lg font-bold text-foreground">{rateLabel(w.rateKind, w.basePricePerNight)}</p>
          {nights > 1 && (
            <p className="text-xs text-muted-foreground mt-0.5">
              {nights}n · ${total.toFixed(2)} est.
              {discount > 0 && <span className="ml-1 text-emerald-600">({(discount * 100).toFixed(0)}% off)</span>}
            </p>
          )}
          {w.cleaningFee && <p className="text-xs text-muted-foreground/70">+${w.cleaningFee.toFixed(2)} cleaning</p>}
        </div>
      </div>

      {booked ? (
        <div className="mt-4 rounded-lg bg-emerald-50 border border-emerald-200 p-3 text-sm text-emerald-700">
          {w.instantBook ? 'Booking confirmed!' : 'Request submitted — the marina will respond shortly.'}
          {' '}<a href="/trips" className="underline font-medium">View my trips →</a>
        </div>
      ) : !isAuthenticated ? (
        <a href={`/login?returnTo=${encodeURIComponent(window.location.pathname + window.location.search)}`}
          className="mt-4 block text-center w-full rounded-lg bg-primary text-primary-foreground py-2 text-sm font-medium hover:bg-primary/90">
          Sign in to {w.instantBook ? 'book' : 'request'}
        </a>
      ) : vessels.length === 0 ? (
        <p className="mt-4 text-xs text-muted-foreground"><a href="/boats" className="underline">Add a vessel</a> to your profile first.</p>
      ) : (
        <div className="mt-4 space-y-2">
          <select value={selectedVesselId} onChange={(e) => setSelectedVesselId(e.target.value)}
            className="w-full rounded-lg border border-border px-3 py-2 text-sm">
            {vessels.map((v) => <option key={v.id} value={v.id}>{v.name} ({v.length}′ {v.boatType})</option>)}
          </select>
          <textarea value={notes} onChange={(e) => setNotes(e.target.value)}
            placeholder="Notes for the marina (optional)" rows={2}
            className="w-full rounded-lg border border-border px-3 py-2 text-sm resize-none" />
          {error && <p className="text-xs text-red-600">{error}</p>}
          <button onClick={handleBook} disabled={booking}
            className="w-full rounded-lg bg-primary text-primary-foreground py-2 text-sm font-medium hover:bg-primary/90 disabled:opacity-50">
            {booking ? 'Submitting…' : w.instantBook ? 'Book now' : 'Request to book'}
          </button>
        </div>
      )}
    </div>
  );
}

// ─── Direct booking card (no window — uses slip's DefaultTransientRate) ───────

function DirectBookingCard({
  slipId, rateKind, baseRate, minCharge, vessels, isAuthenticated,
  arrivesAt, departsAt, nights,
}: {
  slipId: string; rateKind: string; baseRate: number; minCharge?: number | null;
  vessels: VesselDto[]; isAuthenticated: boolean;
  arrivesAt: string; departsAt: string; nights: number;
}) {
  const [selectedVesselId, setSelectedVesselId] = useState(vessels[0]?.id ?? '');
  const [notes, setNotes] = useState('');
  const [booking, setBooking] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [booked, setBooked] = useState(false);

  async function handleBook() {
    if (!selectedVesselId) { setError('Select a vessel.'); return; }
    setError(null); setBooking(true);
    try {
      await createReservation({
        vesselId: selectedVesselId, slipId,
        arrivesAt: new Date(arrivesAt).toISOString(), departsAt: new Date(departsAt).toISOString(),
        notes: notes || null,
      });
      setBooked(true);
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg ?? 'Could not complete booking. Try again.');
    } finally { setBooking(false); }
  }

  return (
    <div className="bg-card rounded-xl border border-blue-200 p-5">
      <div className="flex justify-between items-start">
        <div>
          <p className="text-sm font-semibold text-foreground">Direct booking</p>
          <p className="text-xs text-muted-foreground mt-0.5">Available anytime · Instant confirmation</p>
        </div>
        <div className="text-right">
          <p className="text-lg font-bold text-foreground">{rateLabel(rateKind, baseRate)}</p>
          {minCharge && <p className="text-xs text-muted-foreground/70">min ${minCharge.toFixed(2)}/night</p>}
          {nights > 1 && rateKind === 'Flat' && (
            <p className="text-xs text-muted-foreground mt-0.5">{nights}n · ${(baseRate * nights).toFixed(2)} est.</p>
          )}
        </div>
      </div>

      {booked ? (
        <div className="mt-4 rounded-lg bg-emerald-50 border border-emerald-200 p-3 text-sm text-emerald-700">
          Booking confirmed! <a href="/trips" className="underline font-medium">View my trips →</a>
        </div>
      ) : !isAuthenticated ? (
        <a href={`/login?returnTo=${encodeURIComponent(window.location.pathname + window.location.search)}`}
          className="mt-4 block text-center w-full rounded-lg bg-primary text-primary-foreground py-2 text-sm font-medium hover:bg-primary/90">
          Sign in to book
        </a>
      ) : vessels.length === 0 ? (
        <p className="mt-4 text-xs text-muted-foreground"><a href="/boats" className="underline">Add a vessel</a> to your profile first.</p>
      ) : (
        <div className="mt-4 space-y-2">
          <select value={selectedVesselId} onChange={(e) => setSelectedVesselId(e.target.value)}
            className="w-full rounded-lg border border-border px-3 py-2 text-sm">
            {vessels.map((v) => <option key={v.id} value={v.id}>{v.name} ({v.length}′ {v.boatType})</option>)}
          </select>
          <textarea value={notes} onChange={(e) => setNotes(e.target.value)}
            placeholder="Notes (optional)" rows={2}
            className="w-full rounded-lg border border-border px-3 py-2 text-sm resize-none" />
          {error && <p className="text-xs text-red-600">{error}</p>}
          <button onClick={handleBook} disabled={booking}
            className="w-full rounded-lg bg-primary text-primary-foreground py-2 text-sm font-medium hover:bg-primary/90 disabled:opacity-50">
            {booking ? 'Booking…' : 'Book now'}
          </button>
        </div>
      )}
    </div>
  );
}

// ─── Lease inquiry card ───────────────────────────────────────────────────────

function LeaseInquiryCard({
  slipId, rateKind, baseRate, leaseTerm, vessels, isAuthenticated,
}: {
  slipId: string; rateKind: string; baseRate: number; leaseTerm?: string | null;
  vessels: VesselDto[]; isAuthenticated: boolean;
}) {
  const [selectedVesselId, setSelectedVesselId] = useState(vessels[0]?.id ?? '');
  const [desiredTerm, setDesiredTerm] = useState<LeaseTerm>(
    (leaseTerm as LeaseTerm | null | undefined) ?? 'Annual'
  );
  const [desiredStart, setDesiredStart] = useState('');
  const [message, setMessage] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);

  async function handleSubmit() {
    setError(null); setSubmitting(true);
    try {
      await createLeaseInquiry(slipId, {
        vesselId: selectedVesselId || null,
        desiredTerm,
        desiredStartDate: desiredStart || null,
        message: message || null,
      });
      setSubmitted(true);
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg ?? 'Could not submit inquiry. Try again.');
    } finally { setSubmitting(false); }
  }

  return (
    <div className="bg-card rounded-xl border border-purple-200 p-5">
      <div className="flex justify-between items-start">
        <div>
          <p className="text-sm font-semibold text-foreground">Lease inquiry</p>
          <p className="text-xs text-muted-foreground mt-0.5">Submit your interest — marina will respond</p>
        </div>
        <div className="text-right">
          <p className="text-base font-bold text-foreground">{rateLabel(rateKind, baseRate, leaseTerm)}</p>
          <span className="inline-block mt-1 text-xs px-1.5 py-0.5 rounded-full bg-purple-50 text-purple-700">
            {leaseTerm ?? 'Lease'}
          </span>
        </div>
      </div>

      {submitted ? (
        <div className="mt-4 rounded-lg bg-emerald-50 border border-emerald-200 p-3 text-sm text-emerald-700">
          Inquiry submitted! The marina will review and respond.{' '}
          <a href="/me/lease-inquiries" className="underline font-medium">View my inquiries →</a>
        </div>
      ) : !isAuthenticated ? (
        <a href={`/login?returnTo=${encodeURIComponent(window.location.pathname + window.location.search)}`}
          className="mt-4 block text-center w-full rounded-lg bg-primary text-primary-foreground py-2 text-sm font-medium hover:bg-primary/90">
          Sign in to submit inquiry
        </a>
      ) : (
        <div className="mt-4 space-y-2">
          {vessels.length > 0 && (
            <select value={selectedVesselId} onChange={(e) => setSelectedVesselId(e.target.value)}
              className="w-full rounded-lg border border-border px-3 py-2 text-sm">
              <option value="">— No vessel yet —</option>
              {vessels.map((v) => <option key={v.id} value={v.id}>{v.name} ({v.length}′ {v.boatType})</option>)}
            </select>
          )}
          <div className="flex gap-2">
            <div className="flex-1">
              <label className="block text-xs text-muted-foreground mb-1">Lease term</label>
              <select value={desiredTerm} onChange={(e) => setDesiredTerm(e.target.value as LeaseTerm)}
                className="w-full rounded-lg border border-border px-3 py-2 text-sm">
                <option value="Monthly">Monthly</option>
                <option value="Seasonal">Seasonal</option>
                <option value="Annual">Annual</option>
              </select>
            </div>
            <div className="flex-1">
              <label className="block text-xs text-muted-foreground mb-1">Desired start</label>
              <input type="date" value={desiredStart} onChange={(e) => setDesiredStart(e.target.value)}
                className="w-full rounded-lg border border-border px-3 py-2 text-sm" />
            </div>
          </div>
          <textarea value={message} onChange={(e) => setMessage(e.target.value)}
            placeholder="Tell the marina about yourself and your boat (optional)" rows={3}
            className="w-full rounded-lg border border-border px-3 py-2 text-sm resize-none" />
          {error && <p className="text-xs text-red-600">{error}</p>}
          <button onClick={handleSubmit} disabled={submitting}
            className="w-full rounded-lg bg-purple-700 text-white py-2 text-sm font-medium hover:bg-purple-600 disabled:opacity-50">
            {submitting ? 'Submitting…' : 'Submit lease inquiry'}
          </button>
        </div>
      )}
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export function SlipDetailPage() {
  const slipId = getSlipIdFromPath();
  const { isAuthenticated } = useAuthStore();
  const authed = isAuthenticated();

  const [slip, setSlip] = useState<SlipDetailDto | null>(null);
  const [vessels, setVessels] = useState<VesselDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  const params = new URLSearchParams(window.location.search);
  const arrivesAt = params.get('arrivesAt') ?? new Date().toISOString().slice(0, 10);
  const departsAt = params.get('departsAt') ?? new Date(Date.now() + 86400000).toISOString().slice(0, 10);
  const nights = Math.max(1, Math.round((new Date(departsAt).getTime() - new Date(arrivesAt).getTime()) / 86400000));

  useEffect(() => {
    if (!slipId) { setNotFound(true); setLoading(false); return; }
    const slipFetch = getPublicSlipDetail(slipId).then(setSlip).catch(() => setNotFound(true));
    const vesselFetch = authed ? getVessels().then(setVessels).catch(() => {}) : Promise.resolve();
    Promise.all([slipFetch, vesselFetch]).finally(() => setLoading(false));
  }, [slipId, authed]);

  function handleBooked(windowId: string) {
    setSlip((prev) => prev ? { ...prev, openWindows: prev.openWindows.filter((w) => w.id !== windowId) } : prev);
  }

  if (loading) return (
    <div className="min-h-screen bg-muted/30 flex items-center justify-center text-muted-foreground/70 text-sm">Loading…</div>
  );
  if (notFound || !slip) return (
    <div className="min-h-screen bg-muted/30"><NavBar />
      <div className="max-w-4xl mx-auto py-16 px-4 text-center">
        <p className="text-muted-foreground text-sm">Slip not found or not available.</p>
        <a href="/search" className="mt-3 inline-block text-sm text-foreground/80 underline">← Back to search</a>
      </div>
    </div>
  );

  const transientWindows = slip.openWindows.filter((w) => w.listingKind === 'Transient');
  const leaseWindows     = slip.openWindows.filter((w) => w.listingKind === 'Lease');
  const hasLocation      = slip.latitude != null && slip.longitude != null;

  return (
    <div className="min-h-screen bg-muted/30">
      <NavBar />
      <div className="max-w-5xl mx-auto py-8 px-4">
        <a href="/search" className="text-sm text-muted-foreground hover:text-foreground underline">← Back to search</a>

        <div className="mt-4 grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Left: slip info + map */}
          <div className="lg:col-span-2 space-y-4">
            <div className="bg-card rounded-xl border border-border p-6">
              <h1 className="text-xl font-bold text-foreground">{slip.name}</h1>
              <p className="text-sm text-muted-foreground mt-0.5">
                {slip.marinaName}
                {slip.addressCity && ` · ${slip.addressCity}${slip.addressState ? `, ${slip.addressState}` : ''}`}
              </p>

              <dl className="mt-4 grid grid-cols-2 gap-y-3 gap-x-6 text-sm">
                <div><dt className="text-xs text-muted-foreground/70 font-medium uppercase tracking-wide">Type</dt>
                  <dd className="mt-0.5 text-foreground">{slip.slipType}</dd></div>
                <div><dt className="text-xs text-muted-foreground/70 font-medium uppercase tracking-wide">Max LOA</dt>
                  <dd className="mt-0.5 text-foreground">{slip.maxLength}′</dd></div>
                <div><dt className="text-xs text-muted-foreground/70 font-medium uppercase tracking-wide">Max Beam</dt>
                  <dd className="mt-0.5 text-foreground">{slip.maxBeam}′</dd></div>
                <div><dt className="text-xs text-muted-foreground/70 font-medium uppercase tracking-wide">Max Draft</dt>
                  <dd className="mt-0.5 text-foreground">{slip.maxDraft}′</dd></div>
                <div><dt className="text-xs text-muted-foreground/70 font-medium uppercase tracking-wide">Water</dt>
                  <dd className="mt-0.5 text-foreground">{slip.hasWater ? 'Yes' : 'No'}</dd></div>
                <div><dt className="text-xs text-muted-foreground/70 font-medium uppercase tracking-wide">Electric</dt>
                  <dd className="mt-0.5 text-foreground">{slip.hasElectric ? `${slip.electric ?? ''}A` : 'No'}</dd></div>
              </dl>

              {slip.marinaDescription && (
                <p className="mt-4 text-sm text-foreground/80 leading-relaxed">{slip.marinaDescription}</p>
              )}
              {slip.marinaPhoneNumber && (
                <p className="mt-2 text-sm text-muted-foreground">
                  Phone: <a href={`tel:${slip.marinaPhoneNumber}`} className="underline">{slip.marinaPhoneNumber}</a>
                </p>
              )}
            </div>

            {hasLocation && (
              <div className="h-64 rounded-xl overflow-hidden border border-border shadow-sm">
                <MapContainer center={[slip.latitude!, slip.longitude!]} zoom={14}
                  style={{ height: '100%', width: '100%' }} zoomControl={false}>
                  <TileLayer attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
                    url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
                  <Marker position={[slip.latitude!, slip.longitude!]} />
                </MapContainer>
              </div>
            )}
          </div>

          {/* Right: booking / inquiry cards */}
          <div className="space-y-3">
            {/* Transient windows */}
            {transientWindows.length > 0 && (
              <>
                <h2 className="text-base font-semibold text-foreground">Available windows</h2>
                {transientWindows.map((w) => (
                  <TransientWindowCard key={w.id} w={w} nights={nights}
                    arrivesAt={arrivesAt} departsAt={departsAt}
                    vessels={vessels} isAuthenticated={authed} onBooked={handleBooked} />
                ))}
              </>
            )}

            {/* Direct transient booking (no window) */}
            {slip.transientBookingAvailable && (
              <>
                {transientWindows.length === 0 && (
                  <h2 className="text-base font-semibold text-foreground">Book this slip</h2>
                )}
                <DirectBookingCard
                  slipId={slip.id}
                  rateKind={slip.defaultTransientRateKind ?? 'Flat'}
                  baseRate={slip.defaultTransientBaseRate ?? 0}
                  minCharge={slip.defaultTransientMinCharge}
                  vessels={vessels} isAuthenticated={authed}
                  arrivesAt={arrivesAt} departsAt={departsAt} nights={nights} />
              </>
            )}

            {/* Lease windows */}
            {leaseWindows.length > 0 && (
              <>
                <h2 className="text-base font-semibold text-foreground pt-2">Lease listings</h2>
                {leaseWindows.map((w) => (
                  <div key={w.id} className="bg-card rounded-xl border border-purple-200 p-4">
                    <div className="flex justify-between items-start">
                      <div>
                        <p className="text-sm font-medium text-foreground">{fmtDate(w.startsAt)} – {fmtDate(w.endsAt)}</p>
                        <span className="inline-block mt-1 text-xs px-1.5 py-0.5 rounded-full bg-purple-50 text-purple-700">
                          {w.leaseTerm ?? 'Lease'}
                        </span>
                      </div>
                      <p className="text-base font-bold text-foreground">{rateLabel(w.rateKind, w.basePricePerNight, w.leaseTerm)}</p>
                    </div>
                    <a href={`/slips/${slip.id}?inquire=1`}
                      className="mt-3 block text-center w-full rounded-lg bg-purple-700 text-white py-2 text-sm font-medium hover:bg-purple-600">
                      Submit lease inquiry
                    </a>
                  </div>
                ))}
              </>
            )}

            {/* Default lease inquiry */}
            {slip.leaseInquiryAvailable && leaseWindows.length === 0 && (
              <>
                <h2 className="text-base font-semibold text-foreground pt-2">Lease this slip</h2>
                <LeaseInquiryCard
                  slipId={slip.id}
                  rateKind={slip.defaultLeaseRateKind ?? 'Flat'}
                  baseRate={slip.defaultLeaseBaseRate ?? 0}
                  leaseTerm={slip.defaultLeaseTerm}
                  vessels={vessels} isAuthenticated={authed} />
              </>
            )}

            {/* No availability at all */}
            {!slip.transientBookingAvailable && !slip.leaseInquiryAvailable
              && transientWindows.length === 0 && leaseWindows.length === 0 && (
              <p className="text-sm text-muted-foreground/70 pt-2">No availability listed for this slip right now.</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
