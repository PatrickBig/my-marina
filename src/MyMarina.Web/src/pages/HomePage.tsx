import { useEffect, useState } from 'react';
import { getMyMarinas, deleteDraftMarina, updateMarina, type MyMarinaDto } from '@/api/api';
import { useAuthStore } from '@/store/authStore';
import { NavBar } from '@/components/NavBar';

const MARINA_TYPE_LABELS: Record<string, string> = {
  Commercial: 'Commercial Marina',
  YachtClub: 'Yacht Club',
  PrivateCommunity: 'Private Community',
  Dockominium: 'Dockominium',
  PrivateDock: 'Private Dock',
};

const RELATIONSHIP_LABELS: Record<string, string> = {
  Staff: 'Staff',
  BillingAccount: 'Customer',
};

const SETUP_BANNER_KEY = 'marina-setup-banner-dismissed';

function MarinaCard({ m, onDeleted, onUpdated }: { m: MyMarinaDto; onDeleted?: (id: string) => void; onUpdated?: (updated: MyMarinaDto) => void }) {
  const location = [m.addressCity, m.addressState].filter(Boolean).join(', ');
  const isStaff = m.relationshipKind === 'Staff';
  const isDraft = isStaff && !m.isSetupComplete;
  const isNotListed = isStaff && m.isSetupComplete && !m.isListed;
  const [deleting, setDeleting] = useState(false);
  const [goingLive, setGoingLive] = useState(false);

  if (isDraft) {
    return (
      <div className="rounded-xl border-2 border-dashed border-amber-300 bg-amber-50 dark:bg-amber-950/20 dark:border-amber-700 p-5">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                {MARINA_TYPE_LABELS[m.marinaType] ?? m.marinaType}
              </p>
              <span className="text-xs bg-amber-200 text-amber-800 dark:bg-amber-900/60 dark:text-amber-300 rounded px-1.5 py-0.5 font-medium">
                Draft
              </span>
            </div>
            <h2 className="mt-1 text-base font-semibold text-foreground truncate">{m.name}</h2>
            {location && <p className="mt-0.5 text-sm text-muted-foreground">{location}</p>}
          </div>
          <span className="text-xl shrink-0">🚧</span>
        </div>
        <div className="mt-3 flex gap-2">
          <a
            href={`/marina/${m.id}/setup`}
            className="flex-1 text-center rounded-lg bg-foreground text-background text-sm font-medium px-3 py-2 hover:opacity-90 transition-opacity"
          >
            Continue setup →
          </a>
          <button
            type="button"
            disabled={deleting}
            onClick={async () => {
              if (!confirm('Delete this draft marina? This cannot be undone.')) return;
              setDeleting(true);
              try {
                await deleteDraftMarina(m.id);
                onDeleted?.(m.id);
              } catch {
                alert('Failed to delete. Please try again.');
                setDeleting(false);
              }
            }}
            className="rounded-lg border border-destructive/30 text-destructive text-sm px-3 py-2 hover:bg-destructive/10 disabled:opacity-50 transition-colors"
          >
            {deleting ? 'Deleting…' : 'Delete draft'}
          </button>
        </div>
      </div>
    );
  }

  if (isNotListed) {
    return (
      <div className="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
        <a href={`/marina/${m.id}`} className="block p-5 hover:bg-muted/30 transition-colors">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <div className="flex items-center gap-2 flex-wrap">
                <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  {MARINA_TYPE_LABELS[m.marinaType] ?? m.marinaType}
                </p>
                {m.userRole && (
                  <span className="text-xs bg-muted text-muted-foreground rounded px-1.5 py-0.5">
                    {m.userRole}
                  </span>
                )}
                <span className="text-xs bg-muted text-muted-foreground rounded px-1.5 py-0.5 font-medium">
                  Not Listed
                </span>
              </div>
              <h2 className="mt-1 text-base font-semibold text-foreground truncate">{m.name}</h2>
              {location && <p className="mt-0.5 text-sm text-muted-foreground">{location}</p>}
            </div>
            <span className="text-xl shrink-0">⚓</span>
          </div>
          <p className="mt-3 text-xs text-muted-foreground">Open dashboard →</p>
        </a>
        <div className="border-t border-border px-5 py-3 bg-muted/30 flex items-center justify-between gap-3">
          <p className="text-xs text-muted-foreground">
            Your marina is set up but not visible to boaters yet.
          </p>
          <button
            type="button"
            disabled={goingLive}
            onClick={async () => {
              setGoingLive(true);
              try {
                const updated = await updateMarina(m.id, { isListed: true });
                onUpdated?.({ ...m, isListed: updated.isListed });
              } catch {
                alert('Failed to go live. Please try again.');
              } finally {
                setGoingLive(false);
              }
            }}
            className="shrink-0 rounded-lg bg-emerald-600 text-white text-sm font-medium px-4 py-1.5 hover:bg-emerald-700 disabled:opacity-50 transition-colors"
          >
            {goingLive ? 'Publishing…' : 'Go live →'}
          </button>
        </div>
      </div>
    );
  }

  return (
    <a
      href={isStaff ? `/marina/${m.id}` : '#'}
      className={`block rounded-xl border bg-card p-5 shadow-sm transition-shadow ${
        isStaff ? 'border-border hover:shadow-md' : 'border-border cursor-default'
      }`}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
              {MARINA_TYPE_LABELS[m.marinaType] ?? m.marinaType}
            </p>
            {m.userRole && (
              <span className="text-xs bg-muted text-muted-foreground rounded px-1.5 py-0.5">
                {m.userRole}
              </span>
            )}
            {isStaff && (
              <span className="text-xs bg-emerald-50 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400 rounded px-1.5 py-0.5 font-medium">
                Listed
              </span>
            )}
            {!isStaff && (
              <span className="text-xs bg-primary/10 text-primary rounded px-1.5 py-0.5">
                {RELATIONSHIP_LABELS[m.relationshipKind]}
              </span>
            )}
          </div>
          <h2 className="mt-1 text-base font-semibold text-foreground truncate">{m.name}</h2>
          {location && <p className="mt-0.5 text-sm text-muted-foreground">{location}</p>}
        </div>
        <span className="text-xl shrink-0">⚓</span>
      </div>
      {isStaff && (
        <p className="mt-3 text-xs text-muted-foreground">Open dashboard →</p>
      )}
    </a>
  );
}

export function HomePage() {
  const { user, isPlatformOperator } = useAuthStore();
  const [marinas, setMarinas] = useState<MyMarinaDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [bannerDismissed, setBannerDismissed] = useState(
    () => localStorage.getItem(SETUP_BANNER_KEY) === '1'
  );

  useEffect(() => {
    getMyMarinas()
      .then(setMarinas)
      .finally(() => setLoading(false));
  }, []);

  function handleDismissBanner() {
    localStorage.setItem(SETUP_BANNER_KEY, '1');
    setBannerDismissed(true);
  }

  function handleMarinaDeleted(id: string) {
    setMarinas((prev) => prev.filter((m) => m.id !== id));
  }

  function handleMarinaUpdated(updated: MyMarinaDto) {
    setMarinas((prev) => prev.map((m) => m.id === updated.id ? updated : m));
  }

  const staffMarinas = marinas.filter((m) => m.relationshipKind === 'Staff');
  const activeStaffMarinas = staffMarinas.filter((m) => m.isSetupComplete);
  const customerMarinas = marinas.filter((m) => m.relationshipKind === 'BillingAccount');
  const showSetupBanner = !bannerDismissed && !loading && activeStaffMarinas.length === 0;

  return (
    <div className="min-h-screen bg-background">
      <NavBar />

      {/* Setup banner — shown when user has no active marinas */}
      {showSetupBanner && (
        <div className="bg-primary text-primary-foreground px-4 py-3">
          <div className="max-w-6xl mx-auto flex items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <span className="text-xl">⚓</span>
              <div>
                <p className="text-sm font-medium">Ready to list your marina?</p>
                <p className="text-xs text-primary-foreground/70 mt-0.5">
                  Set up your docks and slips to start accepting bookings.
                </p>
              </div>
            </div>
            <div className="flex items-center gap-2 shrink-0">
              <a
                href="/marina/new"
                className="rounded-lg bg-background text-primary text-sm font-medium px-3 py-1.5 hover:opacity-90 transition-opacity"
              >
                Get started
              </a>
              <button
                type="button"
                onClick={handleDismissBanner}
                className="text-primary-foreground/70 hover:text-primary-foreground text-lg leading-none px-1"
                aria-label="Dismiss"
              >
                ×
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="max-w-6xl mx-auto py-10 px-4">
        <h1 className="text-2xl font-semibold text-foreground">
          Welcome back{user ? `, ${user.firstName}` : ''}
        </h1>
        <p className="text-muted-foreground mt-1 text-sm">What would you like to do today?</p>

        {/* Marina operator section */}
        <div className="mt-8">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide">My Marinas</h2>
            <a href="/marina/new" className="text-sm text-muted-foreground hover:text-foreground">+ Add marina</a>
          </div>

          {loading ? (
            <p className="text-sm text-muted-foreground">Loading…</p>
          ) : staffMarinas.length > 0 ? (
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              {staffMarinas.map((m) => (
                <MarinaCard key={m.id} m={m} onDeleted={handleMarinaDeleted} onUpdated={handleMarinaUpdated} />
              ))}
            </div>
          ) : (
            <a
              href="/marina/new"
              className="flex items-center gap-4 rounded-xl border-2 border-dashed border-border bg-card p-5 hover:border-primary/40 transition-colors"
            >
              <span className="text-2xl">+</span>
              <div>
                <p className="text-sm font-medium text-foreground">Set up a marina</p>
                <p className="text-xs text-muted-foreground mt-0.5">List your slips on the marketplace</p>
              </div>
            </a>
          )}
        </div>

        {/* Platform operator card */}
        {isPlatformOperator && (
          <div className="mt-6">
            <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide mb-3">Platform</h2>
            <a
              href="/admin"
              className="flex items-center gap-4 rounded-xl border border-destructive/30 bg-destructive/5 p-5 hover:shadow-md transition-shadow"
            >
              <span className="text-xl">🔧</span>
              <div>
                <p className="text-sm font-medium text-destructive">Admin panel</p>
                <p className="text-xs text-destructive/70 mt-0.5">Tenants, users, moderation</p>
              </div>
            </a>
          </div>
        )}

        {/* Customer marinas (billing account relationship) */}
        {customerMarinas.length > 0 && (
          <div className="mt-6">
            <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide mb-3">My Marinas (as customer)</h2>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              {customerMarinas.map((m) => <MarinaCard key={m.id} m={m} />)}
            </div>
          </div>
        )}

        {/* Boater quick-links */}
        <div className="mt-8">
          <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide mb-3">Boater</h2>
          <div className="flex flex-wrap gap-2">
            {[
              { href: '/search', label: 'Find a slip' },
              { href: '/trips', label: 'My Trips' },
              { href: '/my-slips', label: 'My Slips' },
              { href: '/invoices', label: 'Invoices' },
              { href: '/maintenance', label: 'Maintenance' },
              { href: '/boats', label: 'My Boats' },
              { href: '/profile', label: 'Profile' },
            ].map((link) => (
              <a
                key={link.href}
                href={link.href}
                className="inline-flex items-center rounded-lg border border-border bg-card px-4 py-2 text-sm text-muted-foreground hover:bg-muted/30 hover:text-foreground transition-colors shadow-sm"
              >
                {link.label}
              </a>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
