import { useState } from 'react';
import { useParams, useNavigate } from '@tanstack/react-router';
import { useQuery } from '@tanstack/react-query';
import { ArrowRight } from 'lucide-react';
import { getMarinaComposition, getBillingSummary, getMarinaReservations, getMarinaWorkOrders, getMarinaInvoices } from '@/api/api';
import { useMarinaCounters } from '@/marina-workspace/useMarinaCounters';
import { PageHeader, PageBody } from '@/marina-workspace/PageHeader';
import { KPI } from '@/components/ui/kpi';
import { Card } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { cn } from '@/lib/utils';

// ─── Helpers ─────────────────────────────────────────────────────────────────

function fmt$(n: number) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(n);
}

// ─── OccupancyRing ───────────────────────────────────────────────────────────

function OccupancyRing({ filled, total, size = 152 }: { filled: number; total: number; size?: number }) {
  const pct = total > 0 ? filled / total : 0;
  const r = (size - 16) / 2;
  const c = 2 * Math.PI * r;
  const stroke = 12;
  return (
    <div className="relative shrink-0" style={{ width: size, height: size }}>
      <svg width={size} height={size} className="-rotate-90" aria-hidden>
        <circle cx={size / 2} cy={size / 2} r={r}
          stroke="var(--muted)" strokeWidth={stroke} fill="none" />
        <circle cx={size / 2} cy={size / 2} r={r}
          stroke="var(--primary)" strokeWidth={stroke} fill="none"
          strokeDasharray={`${c * pct} ${c}`} strokeLinecap="round" />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <div className="text-[28px] font-semibold tabular-nums leading-none">
          {Math.round(pct * 100)}%
        </div>
        <div className="text-xs text-muted-foreground mt-1">
          {filled} / {total} slips
        </div>
      </div>
    </div>
  );
}

// ─── Composition bar + legend ─────────────────────────────────────────────────

const SEGMENT_STYLES: Record<string, string> = {
  annual:      'bg-[#3b82f6]',
  seasonal:    'bg-[#f59e0b]',
  monthly:     'bg-[#f97316]',
  transient:   'bg-[#10b981]',
  listed:      'bg-[#8b5cf6]',
  maintenance: 'bg-[#ef4444]',
  vacant:      'bg-muted',
};

const SEGMENT_LABELS: Record<string, string> = {
  annual: 'Annual', seasonal: 'Seasonal', monthly: 'Monthly',
  transient: 'Transient', listed: 'Listed', maintenance: 'Maintenance', vacant: 'Vacant',
};

interface CompositionData {
  total: number; annual: number; seasonal: number; monthly: number;
  transient: number; listed: number; maintenance: number; vacant: number;
}

function CompositionSection({ data }: { data: CompositionData }) {
  const segments = ['annual', 'seasonal', 'monthly', 'transient', 'listed', 'maintenance', 'vacant'] as const;

  return (
    <div className="flex-1 min-w-0">
      {/* Stacked bar */}
      <div className="flex h-3 rounded-full overflow-hidden gap-px">
        {segments.map((key) => {
          const count = data[key];
          if (count === 0) return null;
          return (
            <div
              key={key}
              className={cn('min-w-[3px]', SEGMENT_STYLES[key])}
              style={{ flex: count }}
              title={`${SEGMENT_LABELS[key]}: ${count}`}
            />
          );
        })}
        {data.total === 0 && <div className="flex-1 bg-muted rounded-full" />}
      </div>

      {/* Legend grid */}
      <div className="mt-3 grid grid-cols-2 gap-x-4 gap-y-1.5">
        {segments.map((key) => (
          <div key={key} className="flex items-center gap-2 text-xs">
            <span className={cn('size-2 rounded-sm shrink-0', SEGMENT_STYLES[key])} />
            <span className="text-muted-foreground flex-1">{SEGMENT_LABELS[key]}</span>
            <span className="tabular-nums font-medium">{data[key]}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

// ─── Inbox card ───────────────────────────────────────────────────────────────

function InboxCard({ marinaId }: { marinaId: string }) {
  const [tab, setTab] = useState<'reservations' | 'workorders' | 'billing' | 'sublets'>('reservations');
  const navigate = useNavigate();
  const { data: counters } = useMarinaCounters(marinaId);

  const { data: reservations = [] } = useQuery({
    queryKey: ['marina-inbox-reservations', marinaId],
    queryFn: () => getMarinaReservations(marinaId, 'PendingApproval'),
    staleTime: 60_000,
  });

  const { data: workOrders = [] } = useQuery({
    queryKey: ['marina-inbox-workorders', marinaId],
    queryFn: () => getMarinaWorkOrders(marinaId, 'InProgress'),
    staleTime: 60_000,
  });

  const { data: invoices = [] } = useQuery({
    queryKey: ['marina-inbox-invoices', marinaId],
    queryFn: () => getMarinaInvoices(marinaId, { status: 'Overdue' }),
    staleTime: 60_000,
  });

  return (
    <Card className="mt-4">
      <Tabs value={tab} onValueChange={(v) => setTab(v as typeof tab)}>
        <div className="px-4 pt-4 border-b border-border">
          <TabsList className="gap-0 bg-transparent p-0 h-auto">
            <TabsTrigger value="reservations" className="text-xs rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent pb-2.5 px-3">
              Reservations
              {(counters?.pendingReservations ?? 0) > 0 && (
                <span className="ml-1.5 text-[10px] font-semibold bg-primary/15 text-primary rounded-full px-1.5">
                  {counters?.pendingReservations}
                </span>
              )}
            </TabsTrigger>
            <TabsTrigger value="workorders" className="text-xs rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent pb-2.5 px-3">
              Work orders
              {(counters?.openWorkOrders ?? 0) > 0 && (
                <span className="ml-1.5 text-[10px] font-semibold bg-primary/15 text-primary rounded-full px-1.5">
                  {counters?.openWorkOrders}
                </span>
              )}
            </TabsTrigger>
            <TabsTrigger value="billing" className="text-xs rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent pb-2.5 px-3">
              Billing
              {(counters?.overdueInvoices ?? 0) > 0 && (
                <span className="ml-1.5 text-[10px] font-semibold bg-destructive/15 text-destructive rounded-full px-1.5">
                  {counters?.overdueInvoices}
                </span>
              )}
            </TabsTrigger>
            <TabsTrigger value="sublets" className="text-xs rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent pb-2.5 px-3">
              Sublets
            </TabsTrigger>
          </TabsList>
        </div>

        <TabsContent value="reservations" className="mt-0">
          {reservations.length === 0 ? (
            <EmptyInbox message="No pending reservations" />
          ) : (
            <InboxList>
              {reservations.slice(0, 5).map((r) => (
                <InboxRow
                  key={r.id}
                  primary={r.vesselName}
                  secondary={`${r.slipName} · ${new Date(r.arrivesAt).toLocaleDateString()}`}
                  meta={fmt$(r.total)}
                  badge={<Badge variant="warning" dot>Pending</Badge>}
                  onClick={() => navigate({ to: '/marina/$marinaId/reservations', params: { marinaId }, search: { id: r.id, status: 'pending' } as Record<string, string> })}
                />
              ))}
              {reservations.length > 5 && (
                <ViewAllRow
                  label={`View all ${reservations.length} pending`}
                  onClick={() => navigate({ to: '/marina/$marinaId/reservations', params: { marinaId }, search: { status: 'pending' } as Record<string, string> })}
                />
              )}
            </InboxList>
          )}
        </TabsContent>

        <TabsContent value="workorders" className="mt-0">
          {workOrders.length === 0 ? (
            <EmptyInbox message="No open work orders" />
          ) : (
            <InboxList>
              {workOrders.slice(0, 5).map((wo) => (
                <InboxRow
                  key={wo.id}
                  primary={wo.title}
                  secondary={wo.assignedToName ?? 'Unassigned'}
                  meta={wo.priority}
                  badge={<Badge variant="primary">In progress</Badge>}
                  onClick={() => navigate({ to: '/marina/$marinaId/maintenance', params: { marinaId }, search: { col: 'inprogress' } as Record<string, string> })}
                />
              ))}
              {workOrders.length > 5 && (
                <ViewAllRow
                  label={`View all ${workOrders.length} in progress`}
                  onClick={() => navigate({ to: '/marina/$marinaId/maintenance', params: { marinaId }, search: { col: 'inprogress' } as Record<string, string> })}
                />
              )}
            </InboxList>
          )}
        </TabsContent>

        <TabsContent value="billing" className="mt-0">
          {invoices.length === 0 ? (
            <EmptyInbox message="No overdue invoices" />
          ) : (
            <InboxList>
              {invoices.slice(0, 5).map((inv) => (
                <InboxRow
                  key={inv.id}
                  primary={inv.billingAccountName}
                  secondary={`#${inv.invoiceNumber} · Due ${new Date(inv.dueDate).toLocaleDateString()}`}
                  meta={fmt$(inv.balanceDue)}
                  badge={<Badge variant="destructive" dot>Overdue</Badge>}
                  onClick={() => navigate({ to: '/marina/$marinaId/billing', params: { marinaId }, search: { id: inv.id, status: 'overdue' } as Record<string, string> })}
                />
              ))}
              {invoices.length > 5 && (
                <ViewAllRow
                  label={`View all ${invoices.length} overdue`}
                  onClick={() => navigate({ to: '/marina/$marinaId/billing', params: { marinaId }, search: { status: 'overdue' } as Record<string, string> })}
                />
              )}
            </InboxList>
          )}
        </TabsContent>

        <TabsContent value="sublets" className="mt-0">
          <EmptyInbox message="Sublet management coming soon">
            <button
              type="button"
              className="mt-2 text-xs text-primary hover:underline"
              onClick={() => navigate({ to: '/marina/$marinaId/listings', params: { marinaId }, search: { slipId: undefined, slipName: undefined, windowId: undefined } })}
            >
              View listings →
            </button>
          </EmptyInbox>
        </TabsContent>
      </Tabs>
    </Card>
  );
}

function InboxList({ children }: { children: React.ReactNode }) {
  return <div className="divide-y divide-border">{children}</div>;
}

function InboxRow({
  primary, secondary, meta, badge, onClick,
}: { primary: string; secondary: string; meta: string; badge: React.ReactNode; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-muted/50 transition-colors"
    >
      <div className="flex-1 min-w-0">
        <div className="text-sm font-medium truncate">{primary}</div>
        <div className="text-xs text-muted-foreground truncate">{secondary}</div>
      </div>
      {badge}
      <span className="text-sm font-semibold tabular-nums shrink-0">{meta}</span>
    </button>
  );
}

function ViewAllRow({ label, onClick }: { label: string; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="w-full flex items-center justify-center gap-1 px-4 py-2.5 text-xs font-medium text-primary hover:bg-muted/50 transition-colors"
    >
      {label} <ArrowRight className="size-3" />
    </button>
  );
}

function EmptyInbox({ message, children }: { message: string; children?: React.ReactNode }) {
  return (
    <div className="flex flex-col items-center justify-center py-8 text-sm text-muted-foreground">
      {message}
      {children}
    </div>
  );
}

// ─── Main dashboard page ──────────────────────────────────────────────────────

export function DashboardPage() {
  const { marinaId = '' } = useParams({ strict: false }) as { marinaId: string };
  const navigate = useNavigate();

  const { data: counters } = useMarinaCounters(marinaId);

  const { data: composition } = useQuery({
    queryKey: ['marina-composition', marinaId],
    queryFn: () => getMarinaComposition(marinaId),
    staleTime: 60_000,
  });

  const { data: billing } = useQuery({
    queryKey: ['marina-billing-summary', marinaId],
    queryFn: () => getBillingSummary(marinaId),
    staleTime: 60_000,
  });

  const occupied = composition
    ? composition.annual + composition.seasonal + composition.monthly + composition.transient
    : 0;

  return (
    <>
      <PageHeader title="Dashboard" />
      <PageBody>
        {/* Hero row */}
        <div className="grid gap-4" style={{ gridTemplateColumns: '1.6fr 1fr' }}>

          {/* Occupancy card */}
          <Card className="p-5">
            <div className="flex gap-5">
              <OccupancyRing filled={occupied} total={composition?.total ?? 0} />
              {composition ? (
                <CompositionSection data={composition} />
              ) : (
                <div className="flex-1 animate-pulse bg-muted rounded h-24" />
              )}
            </div>
            <div className="mt-4 flex justify-end">
              <button
                type="button"
                className="text-xs text-primary hover:underline flex items-center gap-1"
                onClick={() => navigate({ to: '/marina/$marinaId/slips', params: { marinaId } })}
              >
                By dock <ArrowRight className="size-3" />
              </button>
            </div>
          </Card>

          {/* KPI grid */}
          <div className="grid grid-cols-2 gap-3 content-start">
            <KPI
              label="Pending requests"
              value={counters?.pendingReservations ?? '—'}
              hint={counters?.pendingReservations ? `awaiting approval` : undefined}
              accent
              onClick={() => navigate({
                to: '/marina/$marinaId/reservations',
                params: { marinaId },
                search: { status: 'pending' } as Record<string, string>,
              })}
            />
            <KPI
              label="Open invoices"
              value={billing ? fmt$(billing.totalOutstanding) : '—'}
              hint={billing?.overdueCount ? `${billing.overdueCount} overdue` : undefined}
              onClick={() => navigate({
                to: '/marina/$marinaId/billing',
                params: { marinaId },
                search: { status: 'open' } as Record<string, string>,
              })}
            />
            <KPI
              label="MTD earnings"
              value={billing ? fmt$(billing.collectedThisMonth) : '—'}
              onClick={() => navigate({
                to: '/marina/$marinaId/billing',
                params: { marinaId },
                search: { status: 'paid' } as Record<string, string>,
              })}
            />
            <KPI
              label="Open work orders"
              value={counters?.openWorkOrders ?? '—'}
              onClick={() => navigate({
                to: '/marina/$marinaId/maintenance',
                params: { marinaId },
                search: { col: 'inprogress' } as Record<string, string>,
              })}
            />
          </div>
        </div>

        {/* Inbox */}
        <InboxCard marinaId={marinaId} />
      </PageBody>
    </>
  );
}
