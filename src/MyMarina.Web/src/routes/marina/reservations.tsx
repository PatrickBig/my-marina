import { useEffect, useState } from 'react';
import { useParams, useSearch, useNavigate, useRouter } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { X, Anchor, Check } from 'lucide-react';
import {
  getMarinaReservations, approveReservation, declineReservation, markNoShow,
  type ReservationDto,
} from '@/api/api';
import { useMarinaCounters } from '@/marina-workspace/useMarinaCounters';
import { PageHeader, PageBody } from '@/marina-workspace/PageHeader';
import { FilterChip } from '@/components/ui/filter-chip';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { useUrlState } from '@/hooks/useUrlState';
import { cn } from '@/lib/utils';

// ─── Constants ────────────────────────────────────────────────────────────────

const PAGE_SIZE = 10;

type StatusFilter = 'all' | 'pending' | 'confirmed' | 'today' | 'past' | 'cancelled';

// Maps UI filter → API status param (undefined = fetch all)
const STATUS_API: Record<StatusFilter, string | undefined> = {
  all:       undefined,
  pending:   'PendingApproval',
  confirmed: 'Confirmed',
  today:     undefined,
  past:      'Completed',
  cancelled: 'Cancelled',
};

const STATUS_CHIPS: { key: StatusFilter; label: string }[] = [
  { key: 'all',       label: 'All' },
  { key: 'pending',   label: 'Pending' },
  { key: 'confirmed', label: 'Confirmed' },
  { key: 'today',     label: 'Today' },
  { key: 'past',      label: 'Past' },
  { key: 'cancelled', label: 'Cancelled' },
];

// ─── Helpers ─────────────────────────────────────────────────────────────────

function fmtDate(d: string) {
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function fmt$(n: number) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(n);
}

function isToday(d: string) {
  return new Date(d).toDateString() === new Date().toDateString();
}

function isCurrentStay(r: ReservationDto) {
  const now = new Date();
  return new Date(r.arrivesAt) <= now && new Date(r.departsAt) >= now;
}

// ─── Reservation status helpers ───────────────────────────────────────────────

function statusBadge(r: ReservationDto) {
  switch (r.status) {
    case 'PendingApproval':
    case 'PendingHostMarinaApproval':
      return <Badge variant="warning" dot>Pending</Badge>;
    case 'Confirmed':
      return <Badge variant="success" dot>Confirmed</Badge>;
    case 'Completed':
      return <Badge variant="neutral">Completed</Badge>;
    case 'Declined':
      return <Badge variant="destructive">Declined</Badge>;
    case 'Cancelled':
      return <Badge variant="neutral">Cancelled</Badge>;
    case 'NoShow':
      return <Badge variant="destructive">No-show</Badge>;
    default:
      return <Badge variant="neutral">{r.status}</Badge>;
  }
}

// ─── Status stepper ───────────────────────────────────────────────────────────

const STEPS = ['Requested', 'Pending approval', 'Confirmed', 'Completed'] as const;

function stepIndex(status: string): number {
  if (status === 'PendingApproval' || status === 'PendingHostMarinaApproval') return 1;
  if (status === 'Confirmed') return 2;
  if (status === 'Completed') return 3;
  return 0;
}

function StatusStepper({ status }: { status: string }) {
  const active = stepIndex(status);
  const failed = ['Declined', 'Cancelled', 'NoShow'].includes(status);
  return (
    <div className="flex items-center gap-1">
      {STEPS.map((label, i) => (
        <div key={label} className="flex items-center gap-1 flex-1 min-w-0">
          <div className="flex flex-col items-center gap-1 flex-1">
            <div className={cn(
              'size-5 rounded-full border-2 flex items-center justify-center shrink-0',
              i < active ? 'bg-primary border-primary' :
              i === active && !failed ? 'border-primary' :
              i === active && failed ? 'bg-destructive border-destructive' :
              'border-muted-foreground/30',
            )}>
              {i < active && <Check className="size-3 text-primary-foreground" />}
              {i === active && failed && <X className="size-3 text-destructive-foreground" />}
            </div>
            <span className={cn(
              'text-[10px] text-center leading-tight whitespace-nowrap',
              i === active ? 'text-foreground font-medium' : 'text-muted-foreground',
            )}>
              {label}
            </span>
          </div>
          {i < STEPS.length - 1 && (
            <div className={cn(
              'h-px flex-1 mb-4',
              i < active ? 'bg-primary' : 'bg-muted-foreground/20',
            )} />
          )}
        </div>
      ))}
    </div>
  );
}

// ─── Detail panel content ─────────────────────────────────────────────────────

function DetailContent({
  reservation, marinaId, onClose,
}: { reservation: ReservationDto; marinaId: string; onClose: () => void }) {
  const queryClient = useQueryClient();

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['marina-reservations', marinaId] });
    queryClient.invalidateQueries({ queryKey: ['marina-counters', marinaId] });
    queryClient.invalidateQueries({ queryKey: ['marina-inbox-reservations', marinaId] });
  }

  const approve = useMutation({
    mutationFn: () => approveReservation(marinaId, reservation.id),
    onSuccess: invalidate,
  });

  const decline = useMutation({
    mutationFn: () => declineReservation(marinaId, reservation.id),
    onSuccess: invalidate,
  });

  const noShow = useMutation({
    mutationFn: () => markNoShow(marinaId, reservation.id),
    onSuccess: invalidate,
  });

  const isPending = reservation.status === 'PendingApproval' || reservation.status === 'PendingHostMarinaApproval';
  const isConfirmed = reservation.status === 'Confirmed';
  const isHostMarina = reservation.status === 'PendingHostMarinaApproval' && reservation.hostMarinaId !== marinaId;

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-start justify-between p-4 border-b border-border">
        <div className="flex items-center gap-3">
          <div className="size-9 rounded-full bg-primary/15 text-primary flex items-center justify-center shrink-0">
            <Anchor className="size-4" />
          </div>
          <div>
            <div className="text-sm font-semibold">{reservation.vesselName}</div>
            <div className="text-xs text-muted-foreground">{reservation.slipName}</div>
          </div>
        </div>
        <Button variant="ghost" size="icon" className="size-7 shrink-0" onClick={onClose}>
          <X className="size-4" />
        </Button>
      </div>

      {/* Scrollable body */}
      <div className="flex-1 overflow-y-auto">
        {/* Badges row */}
        <div className="px-4 py-3 flex flex-wrap gap-2">
          {statusBadge(reservation)}
          {reservation.instantBook && <Badge variant="accent">Instant book</Badge>}
        </div>

        <Separator />

        {/* Key/value list */}
        <div className="px-4 py-3 space-y-2">
          {[
            ['Slip', reservation.slipName],
            ['Check-in', fmtDate(reservation.arrivesAt)],
            ['Check-out', fmtDate(reservation.departsAt)],
            ['Nights', String(reservation.nights)],
            ['Total', fmt$(reservation.total)],
            ['Source', reservation.instantBook ? 'Instant book' : 'Manual request'],
          ].map(([label, value]) => (
            <div key={label} className="flex justify-between text-sm">
              <span className="text-muted-foreground">{label}</span>
              <span className="font-medium text-right">{value}</span>
            </div>
          ))}
          {reservation.notes && (
            <div className="mt-1 text-xs text-muted-foreground italic">"{reservation.notes}"</div>
          )}
        </div>

        <Separator />

        {/* Status stepper */}
        <div className="px-4 py-4">
          <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">Status flow</p>
          <StatusStepper status={reservation.status} />
        </div>
      </div>

      {/* Sticky actions */}
      {(isPending || isConfirmed) && (
        <div className="border-t border-border p-4 flex gap-2">
          {isPending && !isHostMarina && (
            <>
              <Button
                size="sm" className="flex-1"
                onClick={() => approve.mutate()}
                disabled={approve.isPending || decline.isPending}
              >
                {approve.isPending ? 'Approving…' : 'Approve'}
              </Button>
              <Button
                variant="outline" size="sm" className="flex-1 text-destructive hover:text-destructive"
                onClick={() => decline.mutate()}
                disabled={approve.isPending || decline.isPending}
              >
                {decline.isPending ? 'Declining…' : 'Decline'}
              </Button>
            </>
          )}
          {isPending && isHostMarina && (
            <p className="text-xs text-muted-foreground italic">Awaiting host marina approval.</p>
          )}
          {isConfirmed && (
            <Button
              variant="outline" size="sm"
              onClick={() => noShow.mutate()}
              disabled={noShow.isPending}
            >
              {noShow.isPending ? 'Marking…' : 'Mark no-show'}
            </Button>
          )}
        </div>
      )}
    </div>
  );
}

// ─── Reservation card ─────────────────────────────────────────────────────────

function ReservationCard({
  reservation, selected, onClick,
}: { reservation: ReservationDto; selected: boolean; onClick: () => void }) {
  return (
    <Card
      onClick={onClick}
      data-state={selected ? 'selected' : undefined}
      className={cn(
        'p-4 cursor-pointer transition-[border-color,box-shadow]',
        'hover:[border-color:color-mix(in_oklch,var(--primary)_30%,var(--border))]',
        selected && [
          '[border-color:var(--primary)]',
          '[box-shadow:0_0_0_1px_var(--primary)_inset]',
          '[background:color-mix(in_oklch,var(--primary)_6%,var(--card))]',
        ],
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-3 min-w-0">
          <div className="size-9 rounded-full bg-primary/15 text-primary flex items-center justify-center shrink-0">
            <Anchor className="size-4" />
          </div>
          <div className="min-w-0">
            <div className="text-sm font-medium truncate">{reservation.vesselName}</div>
            <div className="text-xs text-muted-foreground">
              {reservation.slipName} · {fmtDate(reservation.arrivesAt)} – {fmtDate(reservation.departsAt)}
            </div>
          </div>
        </div>
        <div className="flex flex-col items-end gap-1 shrink-0">
          {statusBadge(reservation)}
          <span className="text-sm font-semibold tabular-nums">{fmt$(reservation.total)}</span>
        </div>
      </div>
      {reservation.notes && (
        <p className="mt-2 text-xs text-muted-foreground italic">"{reservation.notes}"</p>
      )}
    </Card>
  );
}

// ─── Narrow-screen breakpoint helper ─────────────────────────────────────────

function useIsWide(breakpoint = 1100) {
  const [wide, setWide] = useState(() => typeof window !== 'undefined' && window.innerWidth >= breakpoint);
  useEffect(() => {
    const update = () => setWide(window.innerWidth >= breakpoint);
    window.addEventListener('resize', update);
    return () => window.removeEventListener('resize', update);
  }, [breakpoint]);
  return wide;
}

// ─── Main page ────────────────────────────────────────────────────────────────

export function ReservationsPage() {
  const { marinaId = '' } = useParams({ strict: false }) as { marinaId: string };
  const navigate = useNavigate();
  const router = useRouter();
  const search = useSearch({ strict: false }) as { status?: string; id?: string; page?: string };
  const isWide = useIsWide();

  const [status, setStatus] = useUrlState<StatusFilter>('status', 'pending');
  const selectedId = search.id ?? null;
  const page = Math.max(1, parseInt(search.page ?? '1', 10) || 1);

  const { data: counters } = useMarinaCounters(marinaId);

  const apiStatus = STATUS_API[status];
  const { data: allReservations = [], isLoading } = useQuery({
    queryKey: ['marina-reservations', marinaId, apiStatus ?? 'all'],
    queryFn: () => getMarinaReservations(marinaId, apiStatus),
    staleTime: 30_000,
  });

  // Apply client-side "today" filter
  const reservations = status === 'today'
    ? allReservations.filter((r) => isToday(r.arrivesAt) || isCurrentStay(r))
    : allReservations;

  // Paginate
  const totalPages = Math.ceil(reservations.length / PAGE_SIZE);
  const paged = reservations.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  const selectedReservation = selectedId ? allReservations.find((r) => r.id === selectedId) ?? null : null;

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

  const sheetOpen = !isWide && !!selectedId;

  return (
    <>
      <PageHeader title="Reservations" />
      <PageBody>
        {/* Filter chips */}
        <div className="flex flex-wrap gap-2 mb-4">
          {STATUS_CHIPS.map(({ key, label }) => (
            <FilterChip
              key={key}
              active={status === key}
              count={key === 'pending' ? counters?.pendingReservations : undefined}
              onClick={() => setStatus(key)}
            >
              {label}
            </FilterChip>
          ))}
        </div>

        {/* Two-column layout */}
        <div className="flex gap-4 min-h-0">
          {/* List */}
          <div className="flex-1 min-w-0 space-y-2">
            {isLoading && (
              <div className="space-y-2">
                {[1, 2, 3].map((i) => (
                  <div key={i} className="h-20 rounded-lg bg-muted animate-pulse" />
                ))}
              </div>
            )}
            {!isLoading && paged.length === 0 && (
              <p className="py-12 text-center text-sm text-muted-foreground">No reservations found.</p>
            )}
            {paged.map((r) => (
              <ReservationCard
                key={r.id}
                reservation={r}
                selected={selectedId === r.id}
                onClick={() => setSelectedId(selectedId === r.id ? null : r.id)}
              />
            ))}
            {totalPages > 1 && (
              <div className="flex items-center justify-between pt-2 text-sm">
                <span className="text-muted-foreground text-xs">
                  {(page - 1) * PAGE_SIZE + 1}–{Math.min(page * PAGE_SIZE, reservations.length)} of {reservations.length}
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

          {/* Inline detail panel at wide viewport */}
          {isWide && selectedReservation && (
            <aside className="w-[360px] shrink-0 border border-border rounded-lg overflow-hidden flex flex-col">
              <DetailContent
                reservation={selectedReservation}
                marinaId={marinaId}
                onClose={() => setSelectedId(null)}
              />
            </aside>
          )}
        </div>
      </PageBody>

      {/* Sheet for narrow viewport */}
      <Sheet open={sheetOpen} onOpenChange={(open) => { if (!open) setSelectedId(null); }}>
        <SheetContent side="right" className="p-0 flex flex-col w-full sm:max-w-[400px]">
          <SheetHeader className="sr-only">
            <SheetTitle>Reservation detail</SheetTitle>
          </SheetHeader>
          {selectedReservation && (
            <DetailContent
              reservation={selectedReservation}
              marinaId={marinaId}
              onClose={() => setSelectedId(null)}
            />
          )}
        </SheetContent>
      </Sheet>
    </>
  );
}
