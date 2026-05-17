import { useEffect, useState } from 'react';
import { getMyTrips, cancelReservation, type ReservationDto } from '@/api/api';
import { NavBar } from '@/components/NavBar';

const STATUS_BADGE: Record<string, string> = {
  PendingApproval:           'bg-amber-50 text-amber-700',
  PendingHostMarinaApproval: 'bg-amber-50 text-amber-700',
  Confirmed:                 'bg-emerald-50 text-emerald-700',
  Declined:                  'bg-red-50 text-red-700',
  Cancelled:                 'bg-muted text-muted-foreground',
  Completed:                 'bg-muted text-foreground/80',
  NoShow:                    'bg-red-50 text-red-600',
};

const STATUS_LABEL: Record<string, string> = {
  PendingApproval:           'Pending approval',
  PendingHostMarinaApproval: 'Pending host approval',
  Confirmed:                 'Confirmed',
  Declined:                  'Declined',
  Cancelled:                 'Cancelled',
  Completed:                 'Completed',
  NoShow:                    'No-show',
};

const ACTIVE_STATUSES = new Set(['PendingApproval', 'PendingHostMarinaApproval', 'Confirmed']);

function fmt(d: string) {
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function TripCard({ r, onCancelled }: { r: ReservationDto; onCancelled: (id: string) => void }) {
  const [cancelling, setCancelling] = useState(false);
  const canCancel = ACTIVE_STATUSES.has(r.status);

  async function handleCancel() {
    if (!window.confirm('Cancel this reservation?')) return;
    setCancelling(true);
    try {
      await cancelReservation(r.id);
      onCancelled(r.id);
    } finally {
      setCancelling(false);
    }
  }

  return (
    <div className="bg-card rounded-xl border border-border p-5">
      <div className="flex justify-between items-start">
        <div>
          <p className="text-sm font-semibold text-foreground">{r.marinaName}</p>
          <p className="text-xs text-muted-foreground mt-0.5">
            {r.slipName} · {r.vesselName}
          </p>
          <p className="text-xs text-muted-foreground mt-0.5">
            {fmt(r.arrivesAt)} – {fmt(r.departsAt)} · {r.nights} night{r.nights !== 1 ? 's' : ''}
          </p>
        </div>
        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${STATUS_BADGE[r.status] ?? 'bg-muted text-muted-foreground'}`}>
          {STATUS_LABEL[r.status] ?? r.status}
        </span>
      </div>

      <div className="mt-3 flex justify-between items-center">
        <div className="text-sm text-foreground">
          <span className="font-medium">${r.total.toFixed(2)}</span>
          <span className="text-xs text-muted-foreground/70 ml-1">· {r.paymentStatus === 'OffPlatform' ? 'Pay at marina' : r.paymentStatus}</span>
        </div>
        {canCancel && (
          <button
            onClick={handleCancel}
            disabled={cancelling}
            className="text-xs text-muted-foreground/70 hover:text-red-500 disabled:opacity-50"
          >
            {cancelling ? 'Cancelling…' : 'Cancel reservation'}
          </button>
        )}
      </div>

      {r.notes && (
        <p className="mt-2 text-xs text-muted-foreground italic">"{r.notes}"</p>
      )}
    </div>
  );
}

export function MyTripsPage() {
  const [trips, setTrips] = useState<ReservationDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getMyTrips()
      .then(setTrips)
      .finally(() => setLoading(false));
  }, []);

  function handleCancelled(id: string) {
    setTrips((prev) =>
      prev.map((r) => r.id === id ? { ...r, status: 'Cancelled' } : r)
    );
  }

  const now = new Date().toISOString();
  const upcoming = trips.filter((r) => ACTIVE_STATUSES.has(r.status) || (r.status === 'Confirmed' && r.departsAt > now));
  const past     = trips.filter((r) => r.status === 'Completed' || r.status === 'NoShow' || (r.status === 'Confirmed' && r.departsAt <= now));
  const other    = trips.filter((r) => r.status === 'Declined' || r.status === 'Cancelled');

  if (loading) return (
    <div className="min-h-screen bg-muted/30 flex items-center justify-center text-muted-foreground/70 text-sm">
      Loading…
    </div>
  );

  return (
    <div className="min-h-screen bg-muted/30">
      <NavBar />
      <div className="max-w-5xl mx-auto py-10 px-4 space-y-8">
        <h1 className="text-xl font-semibold text-foreground">My Trips</h1>

        {trips.length === 0 && (
          <div className="text-center py-16">
            <p className="text-muted-foreground/70 text-sm">No reservations yet.</p>
            <a href="/search" className="mt-3 inline-block text-sm text-foreground/80 underline">Find a slip →</a>
          </div>
        )}

        {upcoming.length > 0 && (
          <section>
            <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide mb-3">Upcoming & Active</h2>
            <div className="space-y-3">
              {upcoming.map((r) => (
                <TripCard key={r.id} r={r} onCancelled={handleCancelled} />
              ))}
            </div>
          </section>
        )}

        {past.length > 0 && (
          <section>
            <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide mb-3">Past</h2>
            <div className="space-y-3">
              {past.map((r) => (
                <TripCard key={r.id} r={r} onCancelled={handleCancelled} />
              ))}
            </div>
          </section>
        )}

        {other.length > 0 && (
          <section>
            <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide mb-3">Declined & Cancelled</h2>
            <div className="space-y-3">
              {other.map((r) => (
                <TripCard key={r.id} r={r} onCancelled={handleCancelled} />
              ))}
            </div>
          </section>
        )}
      </div>
    </div>
  );
}
