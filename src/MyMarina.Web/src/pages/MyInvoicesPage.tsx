import { useEffect, useState } from 'react';
import { getMyInvoices, type InvoiceSummaryDto } from '@/api/api';
import { NavBar } from '@/components/NavBar';

const STATUS_BADGE: Record<string, string> = {
  Draft:          'bg-muted text-muted-foreground',
  Sent:           'bg-blue-50 text-blue-700',
  PartiallyPaid:  'bg-amber-50 text-amber-700',
  Paid:           'bg-emerald-50 text-emerald-700',
  Overdue:        'bg-red-50 text-red-700',
  Voided:         'bg-muted text-muted-foreground/70 line-through',
};

function fmt(d: string) {
  const [y, m, day] = d.split('-');
  return new Date(Number(y), Number(m) - 1, Number(day)).toLocaleDateString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
  });
}

function InvoiceRow({ inv }: { inv: InvoiceSummaryDto }) {
  const badgeClass = STATUS_BADGE[inv.status] ?? 'bg-muted text-muted-foreground';
  const isOverdue = inv.status === 'Overdue';

  return (
    <div className="bg-card rounded-xl border border-border p-5 flex justify-between items-start gap-4">
      <div className="min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <p className="text-sm font-semibold text-foreground">{inv.invoiceNumber}</p>
          <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${badgeClass}`}>
            {inv.status}
          </span>
        </div>
        <p className="text-xs text-muted-foreground mt-0.5">{inv.billingAccountName}</p>
        <p className={`text-xs mt-0.5 ${isOverdue ? 'text-red-600 font-medium' : 'text-muted-foreground/70'}`}>
          Due {fmt(inv.dueDate)}
        </p>
      </div>
      <div className="text-right shrink-0">
        <p className="text-sm font-semibold text-foreground">${inv.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</p>
        {inv.balanceDue > 0 && inv.status !== 'Voided' && (
          <p className={`text-xs mt-0.5 ${isOverdue ? 'text-red-600 font-medium' : 'text-muted-foreground'}`}>
            Balance: ${inv.balanceDue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </p>
        )}
        {inv.status === 'Paid' && (
          <p className="text-xs text-emerald-600 mt-0.5">Paid in full</p>
        )}
      </div>
    </div>
  );
}

export function MyInvoicesPage() {
  const [invoices, setInvoices] = useState<InvoiceSummaryDto[]>([]);
  const [loading, setLoading]   = useState(true);

  useEffect(() => {
    getMyInvoices()
      .then(setInvoices)
      .finally(() => setLoading(false));
  }, []);

  if (loading) return (
    <div className="min-h-screen bg-muted/30 flex items-center justify-center text-muted-foreground/70 text-sm">Loading…</div>
  );

  const open   = invoices.filter((i) => ['Sent', 'PartiallyPaid', 'Overdue'].includes(i.status));
  const closed = invoices.filter((i) => !['Sent', 'PartiallyPaid', 'Overdue'].includes(i.status));

  return (
    <div className="min-h-screen bg-muted/30">
      <NavBar />
      <div className="max-w-5xl mx-auto py-10 px-4 space-y-8">
        <h1 className="text-xl font-semibold text-foreground">My Invoices</h1>

        {invoices.length === 0 && (
          <div className="text-center py-16">
            <p className="text-muted-foreground/70 text-sm">No invoices yet.</p>
            <p className="text-xs text-muted-foreground/70 mt-1">Your marina will issue invoices here once they're sent.</p>
          </div>
        )}

        {open.length > 0 && (
          <section className="space-y-3">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Outstanding</p>
            {open.map((i) => <InvoiceRow key={i.id} inv={i} />)}
          </section>
        )}

        {closed.length > 0 && (
          <section className="space-y-3">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">History</p>
            {closed.map((i) => <InvoiceRow key={i.id} inv={i} />)}
          </section>
        )}
      </div>
    </div>
  );
}
