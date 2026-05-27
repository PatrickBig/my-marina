import { useParams, useSearch, useNavigate, useRouter } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { X, FileText } from 'lucide-react';
import {
  getMarinaInvoices, getBillingSummary,
  voidInvoice, sendInvoice, recordPayment,
  type InvoiceSummaryDto,
} from '@/api/api';
import { PageHeader, PageBody } from '@/marina-workspace/PageHeader';
import { KPI } from '@/components/ui/kpi';
import { FilterChip } from '@/components/ui/filter-chip';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { useUrlState } from '@/hooks/useUrlState';
import { cn } from '@/lib/utils';

// ─── Types / constants ────────────────────────────────────────────────────────

type BillingStatus = 'all' | 'open' | 'overdue' | 'partial' | 'paid' | 'voided';

const STATUS_API: Record<BillingStatus, string | undefined> = {
  all:     undefined,
  open:    'Sent',
  overdue: 'Overdue',
  partial: 'PartiallyPaid',
  paid:    'Paid',
  voided:  'Voided',
};

const STATUS_CHIPS: { key: BillingStatus; label: string }[] = [
  { key: 'all',     label: 'All' },
  { key: 'open',    label: 'Open' },
  { key: 'overdue', label: 'Overdue' },
  { key: 'partial', label: 'Partial' },
  { key: 'paid',    label: 'Paid' },
  { key: 'voided',  label: 'Voided' },
];

const PAGE_SIZE = 10;

// ─── Helpers ─────────────────────────────────────────────────────────────────

function fmt$(n: number) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(n);
}

function daysPastDue(dueDate: string) {
  const due = new Date(dueDate);
  const today = new Date();
  return Math.floor((today.getTime() - due.getTime()) / 86400000);
}

function invoiceStatusBadge(status: string) {
  switch (status) {
    case 'Sent':           return <Badge variant="primary">Open</Badge>;
    case 'Overdue':        return <Badge variant="destructive" dot>Overdue</Badge>;
    case 'PartiallyPaid':  return <Badge variant="warning" dot>Partial</Badge>;
    case 'Paid':           return <Badge variant="success">Paid</Badge>;
    case 'Voided':         return <Badge variant="neutral">Voided</Badge>;
    case 'Draft':          return <Badge variant="neutral">Draft</Badge>;
    default:               return <Badge variant="neutral">{status}</Badge>;
  }
}

// ─── Aging bars component ─────────────────────────────────────────────────────

function AgingBars({ invoices }: { invoices: InvoiceSummaryDto[] }) {
  const outstanding = invoices.filter((i) =>
    ['Sent', 'Overdue', 'PartiallyPaid'].includes(i.status)
  );
  const buckets = [
    { label: 'Current', tone: '#10b981', amount: outstanding.filter((i) => daysPastDue(i.dueDate) <= 0).reduce((s, i) => s + i.balanceDue, 0) },
    { label: '1–30d',   tone: '#f59e0b', amount: outstanding.filter((i) => { const d = daysPastDue(i.dueDate); return d > 0 && d <= 30; }).reduce((s, i) => s + i.balanceDue, 0) },
    { label: '31–60d',  tone: '#f97316', amount: outstanding.filter((i) => { const d = daysPastDue(i.dueDate); return d > 30 && d <= 60; }).reduce((s, i) => s + i.balanceDue, 0) },
    { label: '60+d',    tone: '#ef4444', amount: outstanding.filter((i) => daysPastDue(i.dueDate) > 60).reduce((s, i) => s + i.balanceDue, 0) },
  ];
  const max = Math.max(...buckets.map((b) => b.amount), 1);
  return (
    <div className="flex flex-col gap-1 mt-1">
      {buckets.map((b) => (
        <div key={b.label} className="flex items-center gap-1.5">
          <div className="w-12 text-[10px] text-muted-foreground">{b.label}</div>
          <div className="flex-1 h-2 bg-muted rounded overflow-hidden">
            <div className="h-full rounded" style={{ width: `${(b.amount / max) * 100}%`, background: b.tone }} />
          </div>
          <div className="w-14 text-right text-[10px] font-mono tabular-nums">
            {fmt$(b.amount)}
          </div>
        </div>
      ))}
    </div>
  );
}

// ─── Detail panel ─────────────────────────────────────────────────────────────

function InvoiceDetail({
  invoice, marinaId, onClose,
}: { invoice: InvoiceSummaryDto; marinaId: string; onClose: () => void }) {
  const queryClient = useQueryClient();

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['marina-invoices', marinaId] });
    queryClient.invalidateQueries({ queryKey: ['marina-counters', marinaId] });
    queryClient.invalidateQueries({ queryKey: ['marina-billing-summary', marinaId] });
    queryClient.invalidateQueries({ queryKey: ['marina-inbox-invoices', marinaId] });
  }

  const send = useMutation({
    mutationFn: () => sendInvoice(marinaId, invoice.id),
    onSuccess: invalidate,
  });

  const void_ = useMutation({
    mutationFn: () => voidInvoice(marinaId, invoice.id),
    onSuccess: invalidate,
  });

  const record = useMutation({
    mutationFn: () => recordPayment(marinaId, invoice.id, {
      amount: invoice.balanceDue,
      paidOn: new Date().toISOString().split('T')[0],
      method: 'OffPlatform',
    }),
    onSuccess: invalidate,
  });

  const isActionable = ['Sent', 'Overdue', 'PartiallyPaid'].includes(invoice.status);

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-start justify-between p-4 border-b border-border">
        <div className="flex items-center gap-3">
          <div className="size-9 rounded-lg bg-primary/15 text-primary flex items-center justify-center shrink-0">
            <FileText className="size-4" />
          </div>
          <div>
            <div className="text-sm font-semibold font-mono">#{invoice.invoiceNumber}</div>
            <div className="text-xs text-muted-foreground">{invoice.billingAccountName}</div>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {invoiceStatusBadge(invoice.status)}
          <Button variant="ghost" size="icon" className="size-7" onClick={onClose}>
            <X className="size-4" />
          </Button>
        </div>
      </div>

      {/* Body */}
      <div className="flex-1 overflow-y-auto">
        <div className="px-4 py-3 space-y-2">
          {[
            ['Issued', new Date(invoice.issuedDate).toLocaleDateString()],
            ['Due', new Date(invoice.dueDate).toLocaleDateString()],
            ['Total', fmt$(invoice.totalAmount)],
            ['Paid', fmt$(invoice.amountPaid)],
            ['Balance', fmt$(invoice.balanceDue)],
          ].map(([label, value]) => (
            <div key={label} className="flex justify-between text-sm">
              <span className="text-muted-foreground">{label}</span>
              <span className={cn('font-medium', label === 'Balance' && invoice.status === 'Overdue' && 'text-destructive')}>
                {value}
              </span>
            </div>
          ))}
        </div>

        {invoice.status === 'Overdue' && (
          <div className="mx-4 my-2 p-3 bg-destructive/8 border border-destructive/20 rounded-lg text-sm">
            <div className="font-medium text-destructive">{fmt$(invoice.balanceDue)} overdue</div>
            <div className="text-xs text-muted-foreground mt-0.5">{daysPastDue(invoice.dueDate)} days past due</div>
          </div>
        )}

        <Separator />
        <div className="px-4 py-3 text-xs text-muted-foreground italic">
          Line items available in the full invoice view — post-MVP.
        </div>
      </div>

      {/* Actions */}
      {isActionable && (
        <div className="border-t border-border p-4 flex flex-col gap-2">
          <Button
            size="sm"
            onClick={() => record.mutate()}
            disabled={record.isPending || void_.isPending}
          >
            {record.isPending ? 'Recording…' : 'Mark paid'}
          </Button>
          {invoice.status === 'Sent' && (
            <Button
              variant="outline" size="sm"
              onClick={() => send.mutate()}
              disabled={send.isPending}
            >
              {send.isPending ? 'Sending…' : 'Send reminder'}
            </Button>
          )}
          <Button
            variant="outline" size="sm"
            className="text-destructive hover:text-destructive"
            onClick={() => void_.mutate()}
            disabled={record.isPending || void_.isPending}
          >
            {void_.isPending ? 'Voiding…' : 'Void invoice'}
          </Button>
        </div>
      )}
    </div>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export function BillingPage() {
  const { marinaId = '' } = useParams({ strict: false }) as { marinaId: string };
  const navigate = useNavigate();
  const router = useRouter();
  const search = useSearch({ strict: false }) as { status?: string; id?: string; page?: string };

  const [status, setStatus] = useUrlState<BillingStatus>('status', 'all');
  const selectedId = search.id ?? null;
  const page = Math.max(1, parseInt(search.page ?? '1', 10) || 1);

  // Billing summary for KPIs
  const { data: summary } = useQuery({
    queryKey: ['marina-billing-summary', marinaId],
    queryFn: () => getBillingSummary(marinaId),
    staleTime: 60_000,
  });

  // All outstanding invoices for aging bars
  const { data: allInvoices = [] } = useQuery({
    queryKey: ['marina-invoices-outstanding', marinaId],
    queryFn: () => getMarinaInvoices(marinaId),
    staleTime: 60_000,
  });

  // Filtered invoice list for table
  const apiStatus = STATUS_API[status];
  const { data: filteredInvoices = [], isLoading } = useQuery({
    queryKey: ['marina-invoices', marinaId, apiStatus ?? 'all'],
    queryFn: () => getMarinaInvoices(marinaId, apiStatus ? { status: apiStatus } : undefined),
    staleTime: 30_000,
  });

  // Chip counts from all invoices
  const chipCounts: Record<BillingStatus, number> = {
    all:     allInvoices.length,
    open:    allInvoices.filter((i) => i.status === 'Sent').length,
    overdue: allInvoices.filter((i) => i.status === 'Overdue').length,
    partial: allInvoices.filter((i) => i.status === 'PartiallyPaid').length,
    paid:    allInvoices.filter((i) => i.status === 'Paid').length,
    voided:  allInvoices.filter((i) => i.status === 'Voided').length,
  };

  // Paginate
  const totalPages = Math.ceil(filteredInvoices.length / PAGE_SIZE);
  const paged = filteredInvoices.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  const selectedInvoice = selectedId ? filteredInvoices.find((i) => i.id === selectedId) ?? null : null;

  function setSelectedId(id: string | null) {
    navigate({
      to: router.state.location.pathname,
      search: (prev: Record<string, unknown>) => {
        const merged = { ...prev };
        if (id) merged.id = id; else delete merged.id;
        return merged;
      },
    });
  }

  return (
    <>
      <PageHeader title="Billing" />
      <PageBody>
        {/* KPI row */}
        <div className="grid grid-cols-2 gap-3 mb-6 @min-[900px]/workspace:grid-cols-4">
          <KPI
            label="Outstanding"
            value={summary ? fmt$(summary.totalOutstanding) : '—'}
            hint={summary ? `${summary.sentCount} invoices` : undefined}
            onClick={() => setStatus('open')}
          />
          <KPI
            label="Overdue"
            value={summary ? fmt$(summary.totalOverdue) : '—'}
            hint={summary ? `${summary.overdueCount} invoices` : undefined}
            onClick={() => setStatus('overdue')}
          />
          <KPI
            label="MTD collected"
            value={summary ? fmt$(summary.collectedThisMonth) : '—'}
            onClick={() => setStatus('paid')}
          />
          <div className="rounded-lg border border-border bg-card p-4">
            <div className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Aging
            </div>
            <AgingBars invoices={allInvoices} />
          </div>
        </div>

        {/* Filter chips */}
        <div className="flex flex-wrap gap-2 mb-4">
          {STATUS_CHIPS.map(({ key, label }) => (
            <FilterChip
              key={key}
              active={status === key}
              count={key !== 'all' && chipCounts[key] > 0 ? chipCounts[key] : undefined}
              onClick={() => setStatus(key)}
            >
              {label}
            </FilterChip>
          ))}
        </div>

        {/* Table + detail layout */}
        <div className="flex gap-4 min-h-0">
          {/* Invoice table */}
          <div className="flex-1 min-w-0">
            {isLoading && (
              <div className="space-y-2">
                {[1, 2, 3].map((i) => <div key={i} className="h-12 bg-muted animate-pulse rounded" />)}
              </div>
            )}
            {!isLoading && (
              <div className="rounded-lg border border-border overflow-hidden">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-border bg-muted/40">
                      {['Invoice', 'Account', 'Issued', 'Due', 'Amount', 'Status', ''].map((h) => (
                        <th key={h} className="px-3 py-2.5 text-left text-xs font-semibold text-muted-foreground uppercase tracking-wider last:w-20">
                          {h}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {paged.length === 0 && (
                      <tr>
                        <td colSpan={7} className="px-3 py-12 text-center text-sm text-muted-foreground">
                          No invoices found.
                        </td>
                      </tr>
                    )}
                    {paged.map((inv) => {
                      const overdueDays = inv.status === 'Overdue' ? daysPastDue(inv.dueDate) : 0;
                      return (
                        <tr
                          key={inv.id}
                          onClick={() => setSelectedId(selectedId === inv.id ? null : inv.id)}
                          className={cn(
                            'cursor-pointer hover:bg-muted/40 transition-colors',
                            inv.status === 'Voided' && 'opacity-55',
                            selectedId === inv.id && 'bg-primary/6',
                          )}
                        >
                          <td className="px-3 py-2.5 font-mono font-medium text-xs">#{inv.invoiceNumber}</td>
                          <td className="px-3 py-2.5">{inv.billingAccountName}</td>
                          <td className="px-3 py-2.5 text-muted-foreground text-xs">
                            {new Date(inv.issuedDate).toLocaleDateString()}
                          </td>
                          <td className="px-3 py-2.5 text-xs">
                            <div className="text-muted-foreground">{new Date(inv.dueDate).toLocaleDateString()}</div>
                            {overdueDays > 0 && (
                              <div className="text-destructive text-[10px]">{overdueDays}d overdue</div>
                            )}
                          </td>
                          <td className="px-3 py-2.5 font-mono font-semibold tabular-nums text-right">
                            {inv.status === 'PartiallyPaid'
                              ? <span>{fmt$(inv.amountPaid)} <span className="text-muted-foreground font-normal">of</span> {fmt$(inv.totalAmount)}</span>
                              : fmt$(inv.totalAmount)}
                          </td>
                          <td className="px-3 py-2.5">{invoiceStatusBadge(inv.status)}</td>
                          <td className="px-3 py-2.5 text-right">
                            {inv.status === 'Overdue' && (
                              <span className="text-xs text-primary hover:underline">Remind</span>
                            )}
                            {['Sent', 'PartiallyPaid'].includes(inv.status) && (
                              <span className="text-xs text-primary hover:underline">Record</span>
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}

            {totalPages > 1 && (
              <div className="flex items-center justify-between pt-2 text-sm">
                <span className="text-xs text-muted-foreground">
                  {(page - 1) * PAGE_SIZE + 1}–{Math.min(page * PAGE_SIZE, filteredInvoices.length)} of {filteredInvoices.length}
                </span>
                <div className="flex gap-1">
                  <Button variant="outline" size="sm" disabled={page <= 1}
                    onClick={() => navigate({ to: router.state.location.pathname, search: (prev: Record<string, unknown>) => ({ ...prev, page: String(page - 1) }) })}>
                    Prev
                  </Button>
                  <Button variant="outline" size="sm" disabled={page >= totalPages}
                    onClick={() => navigate({ to: router.state.location.pathname, search: (prev: Record<string, unknown>) => ({ ...prev, page: String(page + 1) }) })}>
                    Next
                  </Button>
                </div>
              </div>
            )}
          </div>

          {/* Inline detail panel */}
          {selectedInvoice && (
            <aside className="hidden @min-[1100px]/workspace:flex w-[340px] shrink-0 border border-border rounded-lg flex-col overflow-hidden">
              <InvoiceDetail
                invoice={selectedInvoice}
                marinaId={marinaId}
                onClose={() => setSelectedId(null)}
              />
            </aside>
          )}
        </div>
      </PageBody>

      {/* Sheet for narrow viewport */}
      <Sheet open={!!selectedId} onOpenChange={(open) => { if (!open) setSelectedId(null); }}>
        <SheetContent side="right" className="p-0 flex flex-col w-full sm:max-w-[380px] @min-[1100px]/workspace:hidden">
          <SheetHeader className="sr-only">
            <SheetTitle>Invoice detail</SheetTitle>
          </SheetHeader>
          {selectedInvoice && (
            <InvoiceDetail
              invoice={selectedInvoice}
              marinaId={marinaId}
              onClose={() => setSelectedId(null)}
            />
          )}
        </SheetContent>
      </Sheet>
    </>
  );
}
