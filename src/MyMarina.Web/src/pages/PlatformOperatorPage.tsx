import { useEffect, useState } from 'react';
import { NavBar } from '@/components/NavBar';
import {
  getPlatformTenants, createPlatformTenant, suspendTenant, reactivateTenant,
  searchPlatformUsers, forceSignOut, deactivatePlatformUser, activatePlatformUser,
  getPlatformAuditLog, getPlatformListings, removePlatformListing,
  type TenantSummaryDto, type UserSummaryDto, type AuditLogEntryDto, type ListingModerationDto,
} from '@/api/api';
import { useAuthStore } from '@/store/authStore';

type Tab = 'tenants' | 'users' | 'listings' | 'audit';

const btn = 'rounded-lg bg-slate-800 text-white px-3 py-1.5 text-xs font-medium hover:bg-slate-700 disabled:opacity-50 transition-colors';
const btnSm = (color: string) => `rounded px-2 py-1 text-xs font-medium ${color} transition-colors`;
const input = 'rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400';

export function PlatformOperatorPage() {
  const { isPlatformOperator } = useAuthStore();
  const [tab, setTab] = useState<Tab>('tenants');

  if (!isPlatformOperator) {
    return (
      <div className="min-h-screen bg-slate-50">
        <NavBar />
        <div className="max-w-4xl mx-auto px-4 py-12 text-slate-500">Access denied.</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <NavBar />
      <div className="max-w-6xl mx-auto px-4 py-8 space-y-6">
        <h1 className="text-xl font-semibold text-slate-800">Platform Administration</h1>

        {/* Tabs */}
        <div className="flex gap-2 border-b border-slate-200">
          {(['tenants', 'users', 'listings', 'audit'] as Tab[]).map((t) => (
            <button
              key={t}
              onClick={() => setTab(t)}
              className={`px-4 py-2 text-sm font-medium capitalize border-b-2 -mb-px transition-colors ${
                tab === t
                  ? 'border-slate-800 text-slate-900'
                  : 'border-transparent text-slate-500 hover:text-slate-700'
              }`}
            >
              {t === 'audit' ? 'Audit Log' : t.charAt(0).toUpperCase() + t.slice(1)}
            </button>
          ))}
        </div>

        {tab === 'tenants' && <TenantsPanel />}
        {tab === 'users'   && <UsersPanel />}
        {tab === 'listings' && <ListingsPanel />}
        {tab === 'audit'   && <AuditLogPanel />}
      </div>
    </div>
  );
}

// ── Tenants ───────────────────────────────────────────────────────────────────

function TenantsPanel() {
  const [tenants, setTenants] = useState<TenantSummaryDto[]>([]);
  const [total, setTotal] = useState(0);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [showCreate, setShowCreate] = useState(false);
  const [newName, setNewName] = useState('');
  const [newSlug, setNewSlug] = useState('');
  const [newBillingEmail, setNewBillingEmail] = useState('');
  const [newTier, setNewTier] = useState('Free');
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getPlatformTenants(search || undefined, page).then((r) => {
      setTenants(r.items);
      setTotal(r.totalCount);
    });
  }, [search, page]);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setCreating(true);
    setError(null);
    try {
      await createPlatformTenant({ name: newName, slug: newSlug, billingEmail: newBillingEmail || undefined, subscriptionTier: newTier });
      setShowCreate(false);
      setNewName(''); setNewSlug(''); setNewBillingEmail(''); setNewTier('Free');
      getPlatformTenants(search || undefined, page).then((r) => { setTenants(r.items); setTotal(r.totalCount); });
    } catch {
      setError('Failed to create tenant.');
    } finally {
      setCreating(false);
    }
  }

  async function handleSuspend(id: string) {
    const reason = window.prompt('Reason for suspension (optional):') ?? undefined;
    await suspendTenant(id, reason);
    setTenants((prev) => prev.map((t) => t.id === id ? { ...t, isActive: false, suspendedAt: new Date().toISOString() } : t));
  }

  async function handleReactivate(id: string) {
    await reactivateTenant(id);
    setTenants((prev) => prev.map((t) => t.id === id ? { ...t, isActive: true, suspendedAt: null } : t));
  }

  const pageSize = 25;
  const totalPages = Math.ceil(total / pageSize);

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <input value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          className={`${input} flex-1 max-w-xs`} placeholder="Search tenants…" />
        <button onClick={() => setShowCreate((v) => !v)} className={btn}>
          {showCreate ? 'Cancel' : 'New tenant'}
        </button>
      </div>

      {showCreate && (
        <form onSubmit={handleCreate} className="bg-white rounded-xl border border-slate-200 p-4 space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">Name</label>
              <input value={newName} onChange={(e) => setNewName(e.target.value)} className={input} required />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">Slug</label>
              <input value={newSlug} onChange={(e) => setNewSlug(e.target.value)} className={input} required />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">Billing Email</label>
              <input type="email" value={newBillingEmail} onChange={(e) => setNewBillingEmail(e.target.value)} className={input} />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">Tier</label>
              <select value={newTier} onChange={(e) => setNewTier(e.target.value)} className={input}>
                {['Free', 'Pro', 'Premium'].map((t) => <option key={t}>{t}</option>)}
              </select>
            </div>
          </div>
          {error && <p className="text-sm text-red-600">{error}</p>}
          <button type="submit" disabled={creating} className={btn}>
            {creating ? 'Creating…' : 'Create'}
          </button>
        </form>
      )}

      <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 border-b border-slate-200">
            <tr>
              {['Name', 'Slug', 'Tier', 'Marinas', 'Status', 'Created', 'Actions'].map((h) => (
                <th key={h} className="text-left px-4 py-2 text-xs font-medium text-slate-500">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {tenants.map((t) => (
              <tr key={t.id} className="hover:bg-slate-50">
                <td className="px-4 py-2 font-medium text-slate-800">
                  {t.name}
                  {t.isDemo && <span className="ml-1 text-xs text-purple-500">(demo)</span>}
                </td>
                <td className="px-4 py-2 text-slate-500">{t.slug}</td>
                <td className="px-4 py-2 text-slate-500">{t.subscriptionTier}</td>
                <td className="px-4 py-2 text-slate-500">{t.marinaCount}</td>
                <td className="px-4 py-2">
                  <span className={`px-2 py-0.5 rounded-full text-xs ${t.isActive ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'}`}>
                    {t.isActive ? 'Active' : 'Suspended'}
                  </span>
                </td>
                <td className="px-4 py-2 text-slate-400 text-xs">{new Date(t.createdAt).toLocaleDateString()}</td>
                <td className="px-4 py-2">
                  {!t.isDemo && (
                    t.isActive
                      ? <button onClick={() => handleSuspend(t.id)} className={btnSm('bg-red-50 text-red-600 hover:bg-red-100')}>Suspend</button>
                      : <button onClick={() => handleReactivate(t.id)} className={btnSm('bg-green-50 text-green-700 hover:bg-green-100')}>Reactivate</button>
                  )}
                </td>
              </tr>
            ))}
            {tenants.length === 0 && (
              <tr><td colSpan={7} className="px-4 py-6 text-center text-slate-400">No tenants found.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex justify-end gap-2">
          <button disabled={page === 1} onClick={() => setPage((p) => p - 1)} className={btn}>Prev</button>
          <span className="text-sm text-slate-500 self-center">Page {page} / {totalPages}</span>
          <button disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)} className={btn}>Next</button>
        </div>
      )}
    </div>
  );
}

// ── Users ─────────────────────────────────────────────────────────────────────

function UsersPanel() {
  const [users, setUsers] = useState<UserSummaryDto[]>([]);
  const [total, setTotal] = useState(0);
  const [q, setQ] = useState('');
  const [page, setPage] = useState(1);

  useEffect(() => {
    searchPlatformUsers(q || undefined, page).then((r) => {
      setUsers(r.items);
      setTotal(r.totalCount);
    });
  }, [q, page]);

  async function handleForceSignOut(id: string) {
    if (!window.confirm('Revoke all active sessions for this user?')) return;
    await forceSignOut(id);
  }

  async function handleDeactivate(id: string) {
    const reason = window.prompt('Reason (optional):') ?? undefined;
    await deactivatePlatformUser(id, reason);
    setUsers((prev) => prev.map((u) => u.id === id ? { ...u, isActive: false } : u));
  }

  async function handleActivate(id: string) {
    await activatePlatformUser(id);
    setUsers((prev) => prev.map((u) => u.id === id ? { ...u, isActive: true } : u));
  }

  const pageSize = 25;
  const totalPages = Math.ceil(total / pageSize);

  return (
    <div className="space-y-4">
      <input value={q} onChange={(e) => { setQ(e.target.value); setPage(1); }}
        className={`${input} max-w-xs`} placeholder="Search by email or name…" />

      <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 border-b border-slate-200">
            <tr>
              {['Name', 'Email', 'Status', 'Confirmed', 'Joined', 'Actions'].map((h) => (
                <th key={h} className="text-left px-4 py-2 text-xs font-medium text-slate-500">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {users.map((u) => (
              <tr key={u.id} className="hover:bg-slate-50">
                <td className="px-4 py-2 font-medium text-slate-800">
                  {u.firstName} {u.lastName}
                  {u.isPlatformOperator && <span className="ml-1 text-xs text-indigo-500">(operator)</span>}
                </td>
                <td className="px-4 py-2 text-slate-500">{u.email}</td>
                <td className="px-4 py-2">
                  <span className={`px-2 py-0.5 rounded-full text-xs ${u.isActive ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'}`}>
                    {u.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="px-4 py-2 text-slate-400 text-xs">{u.emailConfirmed ? 'Yes' : 'No'}</td>
                <td className="px-4 py-2 text-slate-400 text-xs">{new Date(u.createdAt).toLocaleDateString()}</td>
                <td className="px-4 py-2">
                  <div className="flex gap-1">
                    <button onClick={() => handleForceSignOut(u.id)} className={btnSm('bg-slate-100 text-slate-600 hover:bg-slate-200')}>
                      Sign out
                    </button>
                    {u.isActive
                      ? <button onClick={() => handleDeactivate(u.id)} className={btnSm('bg-red-50 text-red-600 hover:bg-red-100')}>Deactivate</button>
                      : <button onClick={() => handleActivate(u.id)} className={btnSm('bg-green-50 text-green-700 hover:bg-green-100')}>Activate</button>
                    }
                  </div>
                </td>
              </tr>
            ))}
            {users.length === 0 && (
              <tr><td colSpan={6} className="px-4 py-6 text-center text-slate-400">No users found.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex justify-end gap-2">
          <button disabled={page === 1} onClick={() => setPage((p) => p - 1)} className={btn}>Prev</button>
          <span className="text-sm text-slate-500 self-center">Page {page} / {totalPages}</span>
          <button disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)} className={btn}>Next</button>
        </div>
      )}
    </div>
  );
}

// ── Listings moderation ───────────────────────────────────────────────────────

function ListingsPanel() {
  const [listings, setListings] = useState<ListingModerationDto[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);

  useEffect(() => {
    getPlatformListings(page).then((r) => { setListings(r.items); setTotal(r.totalCount); });
  }, [page]);

  async function handleRemove(id: string) {
    const reason = window.prompt('Reason for removal (optional):') ?? undefined;
    await removePlatformListing(id, reason);
    setListings((prev) => prev.filter((l) => l.id !== id));
    setTotal((t) => t - 1);
  }

  const pageSize = 50;
  const totalPages = Math.ceil(total / pageSize);

  return (
    <div className="space-y-4">
      <p className="text-sm text-slate-500">{total} active listing{total !== 1 ? 's' : ''}</p>

      <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 border-b border-slate-200">
            <tr>
              {['Slip', 'Marina', 'Tenant', 'Dates', 'Price/night', 'Kind', 'Status', 'Actions'].map((h) => (
                <th key={h} className="text-left px-4 py-2 text-xs font-medium text-slate-500">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {listings.map((l) => (
              <tr key={l.id} className="hover:bg-slate-50">
                <td className="px-4 py-2 font-medium text-slate-800">{l.slipName}</td>
                <td className="px-4 py-2 text-slate-500">{l.marinaName}</td>
                <td className="px-4 py-2 text-slate-500">{l.tenantName}</td>
                <td className="px-4 py-2 text-slate-400 text-xs">
                  {new Date(l.startsAt).toLocaleDateString()} – {new Date(l.endsAt).toLocaleDateString()}
                </td>
                <td className="px-4 py-2 text-slate-500">${l.basePricePerNight.toFixed(2)}</td>
                <td className="px-4 py-2 text-slate-400 text-xs">{l.listedByKind}</td>
                <td className="px-4 py-2">
                  <span className={`px-2 py-0.5 rounded-full text-xs ${
                    l.status === 'Open' ? 'bg-green-50 text-green-700' : 'bg-yellow-50 text-yellow-700'
                  }`}>{l.status}</span>
                </td>
                <td className="px-4 py-2">
                  <button onClick={() => handleRemove(l.id)} className={btnSm('bg-red-50 text-red-600 hover:bg-red-100')}>
                    Remove
                  </button>
                </td>
              </tr>
            ))}
            {listings.length === 0 && (
              <tr><td colSpan={8} className="px-4 py-6 text-center text-slate-400">No active listings.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex justify-end gap-2">
          <button disabled={page === 1} onClick={() => setPage((p) => p - 1)} className={btn}>Prev</button>
          <span className="text-sm text-slate-500 self-center">Page {page} / {totalPages}</span>
          <button disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)} className={btn}>Next</button>
        </div>
      )}
    </div>
  );
}

// ── Audit Log ─────────────────────────────────────────────────────────────────

function AuditLogPanel() {
  const [entries, setEntries] = useState<AuditLogEntryDto[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [tenantFilter, setTenantFilter] = useState('');

  useEffect(() => {
    getPlatformAuditLog(tenantFilter || undefined, page).then((r) => {
      setEntries(r.items);
      setTotal(r.totalCount);
    });
  }, [tenantFilter, page]);

  const pageSize = 50;
  const totalPages = Math.ceil(total / pageSize);

  return (
    <div className="space-y-4">
      <input value={tenantFilter} onChange={(e) => { setTenantFilter(e.target.value); setPage(1); }}
        className={`${input} max-w-xs`} placeholder="Filter by Tenant ID…" />

      <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 border-b border-slate-200">
            <tr>
              {['When', 'Actor', 'Action', 'Target', 'Details'].map((h) => (
                <th key={h} className="text-left px-4 py-2 text-xs font-medium text-slate-500">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {entries.map((e) => (
              <tr key={e.id} className="hover:bg-slate-50">
                <td className="px-4 py-2 text-slate-400 text-xs whitespace-nowrap">
                  {new Date(e.occurredAt).toLocaleString()}
                </td>
                <td className="px-4 py-2 text-slate-600">{e.actorName || '—'}</td>
                <td className="px-4 py-2">
                  <span className="font-mono text-xs bg-slate-100 px-1.5 py-0.5 rounded">{e.action}</span>
                </td>
                <td className="px-4 py-2 text-slate-400 text-xs">
                  {e.targetType && <span>{e.targetType}</span>}
                  {e.targetId && <span className="ml-1 font-mono">{e.targetId.slice(0, 8)}…</span>}
                </td>
                <td className="px-4 py-2 text-slate-500 text-xs">{e.details ?? '—'}</td>
              </tr>
            ))}
            {entries.length === 0 && (
              <tr><td colSpan={5} className="px-4 py-6 text-center text-slate-400">No audit log entries.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex justify-end gap-2">
          <button disabled={page === 1} onClick={() => setPage((p) => p - 1)} className={btn}>Prev</button>
          <span className="text-sm text-slate-500 self-center">Page {page} / {totalPages}</span>
          <button disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)} className={btn}>Next</button>
        </div>
      )}
    </div>
  );
}
