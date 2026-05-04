import { useEffect, useState } from 'react';
import { getMyMarinas, type MyMarinaDto } from '@/api/api';
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

function MarinaCard({ m }: { m: MyMarinaDto }) {
  const location = [m.addressCity, m.addressState].filter(Boolean).join(', ');
  const isStaff = m.relationshipKind === 'Staff';

  return (
    <a
      href={isStaff ? `/marina/${m.id}` : '#'}
      className={`block rounded-xl border bg-white p-5 shadow-sm transition-shadow ${
        isStaff ? 'border-slate-200 hover:shadow-md' : 'border-slate-200 cursor-default'
      }`}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <p className="text-xs font-medium text-slate-400 uppercase tracking-wide">
              {MARINA_TYPE_LABELS[m.marinaType] ?? m.marinaType}
            </p>
            {m.userRole && (
              <span className="text-xs bg-slate-100 text-slate-600 rounded px-1.5 py-0.5">
                {m.userRole}
              </span>
            )}
            {!isStaff && (
              <span className="text-xs bg-blue-50 text-blue-600 rounded px-1.5 py-0.5">
                {RELATIONSHIP_LABELS[m.relationshipKind]}
              </span>
            )}
          </div>
          <h2 className="mt-1 text-base font-semibold text-slate-800 truncate">{m.name}</h2>
          {location && <p className="mt-0.5 text-sm text-slate-500">{location}</p>}
        </div>
        <span className="text-xl shrink-0">⚓</span>
      </div>
      {isStaff && (
        <p className="mt-3 text-xs text-slate-400">Open dashboard →</p>
      )}
    </a>
  );
}

export function HomePage() {
  const { user, isPlatformOperator } = useAuthStore();
  const [marinas, setMarinas] = useState<MyMarinaDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getMyMarinas()
      .then(setMarinas)
      .finally(() => setLoading(false));
  }, []);

  const staffMarinas = marinas.filter((m) => m.relationshipKind === 'Staff');
  const customerMarinas = marinas.filter((m) => m.relationshipKind === 'BillingAccount');

  return (
    <div className="min-h-screen bg-slate-50">
      <NavBar />
      <div className="max-w-4xl mx-auto py-10 px-4">
        <h1 className="text-2xl font-semibold text-slate-800">
          Welcome back{user ? `, ${user.firstName}` : ''}
        </h1>
        <p className="text-slate-500 mt-1 text-sm">What would you like to do today?</p>

        {/* Marina operator section */}
        <div className="mt-8">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-sm font-medium text-slate-500 uppercase tracking-wide">My Marinas</h2>
            <a href="/marina/new" className="text-sm text-slate-500 hover:text-slate-800">+ Add marina</a>
          </div>

          {loading ? (
            <p className="text-sm text-slate-400">Loading…</p>
          ) : staffMarinas.length > 0 ? (
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              {staffMarinas.map((m) => <MarinaCard key={m.id} m={m} />)}
            </div>
          ) : (
            <a
              href="/marina/new"
              className="flex items-center gap-4 rounded-xl border-2 border-dashed border-slate-300 bg-white p-5 hover:border-slate-400 transition-colors"
            >
              <span className="text-2xl">+</span>
              <div>
                <p className="text-sm font-medium text-slate-700">Set up a marina</p>
                <p className="text-xs text-slate-500 mt-0.5">List your slips on the marketplace</p>
              </div>
            </a>
          )}
        </div>

        {/* Platform operator card */}
        {isPlatformOperator && (
          <div className="mt-6">
            <h2 className="text-sm font-medium text-slate-500 uppercase tracking-wide mb-3">Platform</h2>
            <a
              href="/admin"
              className="flex items-center gap-4 rounded-xl border border-red-200 bg-red-50 p-5 hover:shadow-md transition-shadow"
            >
              <span className="text-xl">🔧</span>
              <div>
                <p className="text-sm font-medium text-red-800">Admin panel</p>
                <p className="text-xs text-red-600 mt-0.5">Tenants, users, moderation</p>
              </div>
            </a>
          </div>
        )}

        {/* Customer marinas (billing account relationship) */}
        {customerMarinas.length > 0 && (
          <div className="mt-6">
            <h2 className="text-sm font-medium text-slate-500 uppercase tracking-wide mb-3">My Marinas (as customer)</h2>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              {customerMarinas.map((m) => <MarinaCard key={m.id} m={m} />)}
            </div>
          </div>
        )}

        {/* Boater quick-links */}
        <div className="mt-8">
          <h2 className="text-sm font-medium text-slate-500 uppercase tracking-wide mb-3">Boater</h2>
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
                className="inline-flex items-center rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm text-slate-600 hover:bg-slate-50 hover:text-slate-800 transition-colors shadow-sm"
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
