import { useParams, useNavigate, useRouter, useSearch } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Search, X, Plus, Users } from 'lucide-react';
import {
  getBillingAccounts, createBillingAccount, updateBillingAccount,
  getBillingAccountMembers, inviteAccountMember, removeAccountMember,
  getVesselRecords, createVesselRecord, getMarinaInvoices,
  type BillingAccountDto, type BillingAccountMemberDto, type VesselRecordDto,
  type InvoiceSummaryDto,
} from '@/api/api';
import { PageHeader, PageBody } from '@/marina-workspace/PageHeader';
import { FilterChip } from '@/components/ui/filter-chip';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Pagination } from '@/components/ui/pagination';
import { useIsWide } from '@/hooks/useIsWide';
import { cn } from '@/lib/utils';

// ─── Types ────────────────────────────────────────────────────────────────────

type AccountStatus = 'all' | 'active' | 'inactive';

const STATUS_CHIPS: { key: AccountStatus; label: string }[] = [
  { key: 'all',      label: 'All' },
  { key: 'active',   label: 'Active' },
  { key: 'inactive', label: 'Inactive' },
];

const PAGE_SIZE = 25;

// ─── Account row ─────────────────────────────────────────────────────────────

function AccountRow({
  account,
  selected,
  onClick,
}: {
  account: BillingAccountDto;
  selected: boolean;
  onClick: () => void;
}) {
  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onClick}
      onKeyDown={(e) => e.key === 'Enter' && onClick()}
      data-selected={selected}
      className={cn(
        'flex items-center justify-between px-4 py-3 cursor-pointer rounded-lg border transition-colors',
        selected
          ? 'border-primary bg-primary/5 shadow-[inset_0_0_0_2px_hsl(var(--primary)/0.3)]'
          : 'border-border bg-card hover:bg-muted/40',
      )}
    >
      <div className="flex items-center gap-3 min-w-0">
        <div className="h-8 w-8 rounded-full bg-muted flex items-center justify-center shrink-0">
          <Users className="h-4 w-4 text-muted-foreground" />
        </div>
        <div className="min-w-0">
          <p className="text-sm font-medium text-foreground truncate">{account.displayName}</p>
          <p className="text-xs text-muted-foreground truncate">{account.billingEmail}</p>
        </div>
      </div>
      <div className="flex items-center gap-2 shrink-0 ml-4">
        {!account.isActive && <Badge variant="neutral">Inactive</Badge>}
        {account.billingPhone && (
          <span className="text-xs text-muted-foreground hidden sm:block">{account.billingPhone}</span>
        )}
      </div>
    </div>
  );
}

// ─── Detail panel ─────────────────────────────────────────────────────────────

function AccountDetail({
  marinaId,
  account,
  onUpdated,
  onNavigateBilling,
}: {
  marinaId: string;
  account: BillingAccountDto;
  onUpdated: (a: BillingAccountDto) => void;
  onNavigateBilling: (invoiceId: string) => void;
}) {
  const queryClient = useQueryClient();

  const { data: members = [] } = useQuery<BillingAccountMemberDto[]>({
    queryKey: ['account-members', marinaId, account.id],
    queryFn: () => getBillingAccountMembers(marinaId, account.id),
  });

  const { data: vessels = [] } = useQuery<VesselRecordDto[]>({
    queryKey: ['account-vessels', marinaId, account.id],
    queryFn: () => getVesselRecords(marinaId, account.id),
  });

  const { data: openInvoices = [] } = useQuery<InvoiceSummaryDto[]>({
    queryKey: ['account-invoices', marinaId, account.id],
    queryFn: () => getMarinaInvoices(marinaId, { billingAccountId: account.id }),
  });

  const overdueAmount = openInvoices
    .filter((i) => i.status === 'Overdue')
    .reduce((sum, i) => sum + i.balanceDue, 0);

  const updateMutation = useMutation({
    mutationFn: (data: Parameters<typeof updateBillingAccount>[2]) =>
      updateBillingAccount(marinaId, account.id, data),
    onSuccess: (updated) => {
      onUpdated(updated);
      queryClient.invalidateQueries({ queryKey: ['marina-accounts', marinaId] });
    },
  });

  const inviteMutation = useMutation({
    mutationFn: (email: string) => inviteAccountMember(marinaId, account.id, email),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['account-members', marinaId, account.id] }),
  });

  const removeMemberMutation = useMutation({
    mutationFn: (memberId: string) => removeAccountMember(marinaId, account.id, memberId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['account-members', marinaId, account.id] }),
  });

  const addVesselMutation = useMutation({
    mutationFn: (data: Parameters<typeof createVesselRecord>[1]) => createVesselRecord(marinaId, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['account-vessels', marinaId, account.id] }),
  });

  const [inviteEmail, setInviteEmail] = useState('');
  const [showVesselForm, setShowVesselForm] = useState(false);
  const [vesselName, setVesselName] = useState('');
  const [vesselMake, setVesselMake] = useState('');
  const [vesselLength, setVesselLength] = useState('');

  function handleInvite(e: React.FormEvent) {
    e.preventDefault();
    if (!inviteEmail) return;
    inviteMutation.mutate(inviteEmail, { onSuccess: () => setInviteEmail('') });
  }

  function handleAddVessel(e: React.FormEvent) {
    e.preventDefault();
    if (!vesselName.trim()) return;
    addVesselMutation.mutate(
      {
        billingAccountId: account.id,
        vesselName: vesselName.trim(),
        vesselMake: vesselMake || null,
        vesselLength: vesselLength ? Number(vesselLength) : null,
        vesselBeam: null, vesselDraft: null, claimEmail: null,
      },
      {
        onSuccess: () => {
          setVesselName(''); setVesselMake(''); setVesselLength('');
          setShowVesselForm(false);
        },
      },
    );
  }

  return (
    <div className="flex flex-col h-full overflow-y-auto">
      {/* Header */}
      <div className="p-5 border-b border-border">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h3 className="text-base font-semibold text-foreground">{account.displayName}</h3>
            <p className="text-sm text-muted-foreground">{account.billingEmail}</p>
            {account.billingPhone && (
              <p className="text-xs text-muted-foreground/70 mt-0.5">{account.billingPhone}</p>
            )}
          </div>
          <div className="flex gap-2 shrink-0">
            <Button
              size="sm"
              variant="outline"
              onClick={() => updateMutation.mutate({ isActive: !account.isActive })}
              disabled={updateMutation.isPending}
            >
              {account.isActive ? 'Deactivate' : 'Reactivate'}
            </Button>
          </div>
        </div>
        {account.emergencyContactName && (
          <p className="mt-2 text-xs text-muted-foreground/80">
            Emergency: {account.emergencyContactName}
            {account.emergencyContactPhone && ` · ${account.emergencyContactPhone}`}
          </p>
        )}
      </div>

      <div className="flex-1 p-5 space-y-5">
        {/* Overdue callout */}
        {overdueAmount > 0 && (
          <div className="rounded-lg bg-destructive/10 border border-destructive/20 px-4 py-3">
            <p className="text-sm font-medium text-destructive">
              ${overdueAmount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} overdue
            </p>
            <p className="text-xs text-destructive/80 mt-0.5">
              {openInvoices.filter((i) => i.status === 'Overdue').length} overdue invoice(s)
            </p>
          </div>
        )}

        {/* Members */}
        <section>
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-2">Members</p>
          {members.length === 0 ? (
            <p className="text-xs text-muted-foreground/70">No platform members linked.</p>
          ) : (
            <ul className="space-y-1.5">
              {members.map((m) => (
                <li key={m.id} className="flex items-center justify-between text-sm">
                  <span className="text-foreground/80 truncate">
                    {m.userName || m.userEmail}
                    <span className="text-muted-foreground/60 ml-1">· {m.role}</span>
                    {!m.acceptedAt && <span className="ml-1 text-muted-foreground/50">(pending)</span>}
                  </span>
                  <button
                    onClick={() => removeMemberMutation.mutate(m.id)}
                    className="ml-2 text-xs text-red-400 hover:text-red-600 shrink-0"
                  >
                    Remove
                  </button>
                </li>
              ))}
            </ul>
          )}
          <form onSubmit={handleInvite} className="flex gap-2 mt-2">
            <input
              value={inviteEmail}
              onChange={(e) => setInviteEmail(e.target.value)}
              type="email"
              placeholder="Invite by email…"
              className="flex-1 text-xs rounded-md border border-border bg-background px-3 py-1.5 text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:ring-2 focus:ring-ring"
            />
            <Button type="submit" size="sm" variant="outline" disabled={inviteMutation.isPending}>
              {inviteMutation.isPending ? '…' : 'Invite'}
            </Button>
          </form>
        </section>

        <Separator />

        {/* Vessels */}
        <section>
          <div className="flex items-center justify-between mb-2">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Vessels</p>
            <button
              onClick={() => setShowVesselForm((v) => !v)}
              className="text-xs text-muted-foreground/70 hover:text-foreground underline"
            >
              {showVesselForm ? 'Cancel' : '+ Add vessel'}
            </button>
          </div>
          {vessels.length === 0 && !showVesselForm && (
            <p className="text-xs text-muted-foreground/70">No vessels on file.</p>
          )}
          <ul className="space-y-1">
            {vessels.map((v) => (
              <li key={v.id} className="text-xs text-foreground/80">
                <span className="font-medium">{v.vesselName}</span>
                {v.vesselMake && ` · ${v.vesselMake}`}
                {v.vesselLength && ` · ${v.vesselLength}′`}
                {v.vesselIsGhost && <span className="ml-1 text-muted-foreground/60">(unregistered)</span>}
              </li>
            ))}
          </ul>
          {showVesselForm && (
            <form onSubmit={handleAddVessel} className="mt-2 space-y-2 p-3 bg-muted/30 rounded-lg">
              <div className="grid grid-cols-3 gap-2">
                <div className="col-span-3">
                  <label className="block text-xs text-muted-foreground mb-0.5">Name *</label>
                  <input
                    value={vesselName}
                    onChange={(e) => setVesselName(e.target.value)}
                    className="w-full text-xs rounded-md border border-border bg-background px-2 py-1.5 text-foreground"
                    required
                  />
                </div>
                <div className="col-span-2">
                  <label className="block text-xs text-muted-foreground mb-0.5">Make</label>
                  <input
                    value={vesselMake}
                    onChange={(e) => setVesselMake(e.target.value)}
                    className="w-full text-xs rounded-md border border-border bg-background px-2 py-1.5 text-foreground"
                  />
                </div>
                <div>
                  <label className="block text-xs text-muted-foreground mb-0.5">Length (ft)</label>
                  <input
                    type="number"
                    step="0.1"
                    value={vesselLength}
                    onChange={(e) => setVesselLength(e.target.value)}
                    className="w-full text-xs rounded-md border border-border bg-background px-2 py-1.5 text-foreground"
                  />
                </div>
              </div>
              <Button type="submit" size="sm" disabled={addVesselMutation.isPending}>
                {addVesselMutation.isPending ? 'Adding…' : 'Add vessel'}
              </Button>
            </form>
          )}
        </section>

        <Separator />

        {/* Open invoices */}
        <section>
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-2">Open invoices</p>
          {openInvoices.length === 0 ? (
            <p className="text-xs text-muted-foreground/70">No open invoices.</p>
          ) : (
            <ul className="space-y-1.5">
              {openInvoices.map((inv) => (
                <li key={inv.id} className="flex items-center justify-between text-xs">
                  <button
                    onClick={() => onNavigateBilling(inv.id)}
                    className="text-primary underline underline-offset-2 hover:text-primary/80 truncate"
                  >
                    {inv.invoiceNumber}
                  </button>
                  <div className="flex items-center gap-2 shrink-0 ml-2">
                    {inv.status === 'Overdue' && <Badge variant="destructive" dot>Overdue</Badge>}
                    <span className="text-muted-foreground">
                      ${inv.balanceDue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </div>
  );
}

// ─── Create account dialog ────────────────────────────────────────────────────

function CreateAccountForm({
  marinaId,
  onCreated,
  onCancel,
}: {
  marinaId: string;
  onCreated: (a: BillingAccountDto) => void;
  onCancel: () => void;
}) {
  const [form, setForm] = useState({
    displayName: '', billingEmail: '', billingPhone: '',
    emergencyContactName: '', emergencyContactPhone: '',
  });
  const [error, setError] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: () =>
      createBillingAccount(marinaId, {
        displayName: form.displayName,
        billingEmail: form.billingEmail,
        billingPhone: form.billingPhone || null,
        emergencyContactName: form.emergencyContactName || null,
        emergencyContactPhone: form.emergencyContactPhone || null,
      }),
    onSuccess: (created) => onCreated(created),
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      setError(msg ?? 'Could not create account.');
    },
  });

  const inputCls = 'w-full rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:ring-2 focus:ring-ring';

  return (
    <div className="p-5 space-y-3">
      <h3 className="text-sm font-semibold text-foreground">New billing account</h3>
      <div className="grid grid-cols-2 gap-3">
        <div className="col-span-2">
          <label className="block text-xs text-muted-foreground mb-1">Account name *</label>
          <input
            value={form.displayName}
            onChange={(e) => setForm({ ...form, displayName: e.target.value })}
            placeholder="John Smith"
            className={inputCls}
          />
        </div>
        <div className="col-span-2">
          <label className="block text-xs text-muted-foreground mb-1">Billing email *</label>
          <input
            type="email"
            value={form.billingEmail}
            onChange={(e) => setForm({ ...form, billingEmail: e.target.value })}
            className={inputCls}
          />
        </div>
        <div>
          <label className="block text-xs text-muted-foreground mb-1">Phone</label>
          <input value={form.billingPhone} onChange={(e) => setForm({ ...form, billingPhone: e.target.value })} className={inputCls} />
        </div>
        <div>
          <label className="block text-xs text-muted-foreground mb-1">Emergency contact</label>
          <input value={form.emergencyContactName} onChange={(e) => setForm({ ...form, emergencyContactName: e.target.value })} className={inputCls} />
        </div>
      </div>
      {error && <p className="text-xs text-red-600">{error}</p>}
      <div className="flex gap-2">
        <Button
          onClick={() => {
            if (!form.displayName || !form.billingEmail) { setError('Name and email are required.'); return; }
            setError(null);
            createMutation.mutate();
          }}
          disabled={createMutation.isPending}
        >
          {createMutation.isPending ? 'Creating…' : 'Create account'}
        </Button>
        <Button variant="outline" onClick={onCancel}>Cancel</Button>
      </div>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

import { useState } from 'react';

export function CustomersPage() {
  const { marinaId } = useParams({ strict: false }) as { marinaId: string };
  const navigate = useNavigate();
  const router = useRouter();
  const isWide = useIsWide(1100);
  const queryClient = useQueryClient();

  const search = useSearch({ strict: false }) as { status?: string; id?: string; page?: string; q?: string };
  const status = (search.status as AccountStatus) ?? 'all';
  const selectedId = search.id;
  const page = Math.max(1, parseInt(search.page ?? '1', 10) || 1);
  const q = search.q ?? '';

  const [showCreate, setShowCreate] = useState(false);

  function updateSearch(patch: Record<string, string | undefined>) {
    const current = { ...search };
    for (const [k, v] of Object.entries(patch)) {
      if (v == null || v === '') delete (current as Record<string, unknown>)[k];
      else (current as Record<string, unknown>)[k] = v;
    }
    navigate({ to: router.state.location.pathname, search: current, replace: false });
  }

  function setStatus(s: AccountStatus) { updateSearch({ status: s === 'all' ? undefined : s, page: undefined }); }
  function setPage(p: number) { updateSearch({ page: p === 1 ? undefined : String(p) }); }
  function setQ(v: string) { updateSearch({ q: v || undefined, page: undefined }); }
  function setSelectedId(id: string | undefined) { updateSearch({ id }); }

  const { data: accounts = [], isLoading } = useQuery<BillingAccountDto[]>({
    queryKey: ['marina-accounts', marinaId],
    queryFn: () => getBillingAccounts(marinaId),
    staleTime: 30_000,
  });

  // Filter
  const filtered = accounts.filter((a) => {
    if (status === 'active' && !a.isActive) return false;
    if (status === 'inactive' && a.isActive) return false;
    if (q) {
      const lq = q.toLowerCase();
      return a.displayName.toLowerCase().includes(lq) || a.billingEmail.toLowerCase().includes(lq);
    }
    return true;
  });

  const pageAccounts = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const selectedAccount = accounts.find((a) => a.id === selectedId);

  function closeDetail() {
    setSelectedId(undefined);
  }

  function openDetail(id: string) {
    setSelectedId(id);
    setShowCreate(false);
  }

  function navigateToBilling(invoiceId: string) {
    navigate({ to: '/marina/$marinaId/billing', params: { marinaId }, search: { id: invoiceId } });
  }

  const detailContent = selectedAccount ? (
    <AccountDetail
      key={selectedAccount.id}
      marinaId={marinaId}
      account={selectedAccount}
      onUpdated={(updated) => {
        queryClient.setQueryData<BillingAccountDto[]>(['marina-accounts', marinaId], (prev) =>
          prev?.map((a) => (a.id === updated.id ? updated : a)) ?? [],
        );
      }}
      onNavigateBilling={navigateToBilling}
    />
  ) : null;

  return (
    <>
      <PageHeader
        title="Customers"
        actions={
          <Button size="sm" onClick={() => { setShowCreate(true); setSelectedId(undefined); }}>
            <Plus className="h-4 w-4 mr-1.5" />
            New account
          </Button>
        }
      />
      <PageBody>
        {/* Search + chips */}
        <div className="flex flex-wrap items-center gap-2 mb-4">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <input
              value={q}
              onChange={(e) => { setQ(e.target.value); setPage(1); }}
              placeholder="Search accounts…"
              className="pl-9 pr-3 h-8 rounded-full border border-border bg-card text-sm text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:ring-2 focus:ring-ring w-52"
            />
          </div>
          {STATUS_CHIPS.map(({ key, label }) => (
            <FilterChip
              key={key}
              active={status === key}
              count={(() => { if (key === 'all') return undefined; const n = key === 'active' ? accounts.filter((a) => a.isActive).length : accounts.filter((a) => !a.isActive).length; return n > 0 ? n : undefined; })()}
              onClick={() => { setStatus(key); setPage(1); }}
            >
              {label}
            </FilterChip>
          ))}
        </div>

        {/* Create form (inline, above list) */}
        {showCreate && (
          <div className="mb-4 rounded-xl border border-border bg-card">
            <CreateAccountForm
              marinaId={marinaId}
              onCreated={(created) => {
                queryClient.invalidateQueries({ queryKey: ['marina-accounts', marinaId] });
                setShowCreate(false);
                openDetail(created.id);
              }}
              onCancel={() => setShowCreate(false)}
            />
          </div>
        )}

        {/* Two-column layout at wide viewports */}
        <div className={cn('flex gap-4 min-h-0', isWide && selectedAccount ? 'items-start' : '')}>
          {/* Account list */}
          <div className={cn('flex-1 min-w-0 space-y-2', isWide && selectedAccount ? 'max-w-sm' : '')}>
            {isLoading && (
              <div className="space-y-2">
                {[1, 2, 3].map((i) => <div key={i} className="h-14 bg-muted animate-pulse rounded-lg" />)}
              </div>
            )}

            {!isLoading && pageAccounts.length === 0 && (
              <div className="flex flex-col items-center justify-center py-12 text-center">
                <Users className="h-8 w-8 text-muted-foreground/40 mb-2" />
                <p className="text-sm text-muted-foreground">No accounts found</p>
              </div>
            )}

            {pageAccounts.map((account) => (
              <AccountRow
                key={account.id}
                account={account}
                selected={selectedId === account.id}
                onClick={() => selectedId === account.id ? closeDetail() : openDetail(account.id)}
              />
            ))}

            {filtered.length > PAGE_SIZE && (
              <div className="mt-4">
                <Pagination page={page} onChange={setPage} total={filtered.length} pageSize={PAGE_SIZE} />
              </div>
            )}
          </div>

          {/* Inline detail at wide viewport */}
          {isWide && selectedAccount && (
            <div className="flex-1 min-w-0 rounded-xl border border-border bg-card min-h-[400px]">
              <div className="flex items-center justify-between px-5 py-3 border-b border-border">
                <span className="text-sm font-medium text-foreground">Account details</span>
                <Button size="sm" variant="ghost" onClick={closeDetail}>
                  <X className="h-4 w-4" />
                </Button>
              </div>
              {detailContent}
            </div>
          )}
        </div>
      </PageBody>

      {/* Sheet for narrow viewports */}
      {!isWide && (
        <Sheet open={!!selectedAccount} onOpenChange={(open) => !open && closeDetail()}>
          <SheetContent side="right" className="w-full sm:max-w-md p-0 flex flex-col">
            <SheetHeader className="px-5 py-4 border-b border-border">
              <SheetTitle>{selectedAccount?.displayName ?? 'Account'}</SheetTitle>
            </SheetHeader>
            {detailContent}
          </SheetContent>
        </Sheet>
      )}
    </>
  );
}
