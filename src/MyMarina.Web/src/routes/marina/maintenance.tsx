import { useParams, useSearch, useNavigate, useRouter } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { LayoutList, Columns3, X, Plus } from 'lucide-react';
import {
  getMarinaMaintenanceRequests, updateMaintenanceRequestStatus,
  getMarinaWorkOrders, updateWorkOrder, createWorkOrder,
  type MaintenanceRequestDto, type WorkOrderDto,
} from '@/api/api';
import { PageHeader, PageBody } from '@/marina-workspace/PageHeader';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { useUrlState } from '@/hooks/useUrlState';
import { cn } from '@/lib/utils';

// ─── Types ────────────────────────────────────────────────────────────────────

type DoneWindow = '7d' | '30d' | 'all';
type ColFilter = 'new' | 'scheduled' | 'inprogress' | 'done';

// ─── Helpers ─────────────────────────────────────────────────────────────────

function priorityVariant(p: string): 'destructive' | 'warning' | 'neutral' | 'success' {
  if (p === 'Urgent') return 'destructive';
  if (p === 'High')   return 'warning';
  if (p === 'Low')    return 'success';
  return 'neutral';
}

function withinDays(dateStr: string | null, days: number) {
  if (!dateStr) return false;
  const cutoff = new Date();
  cutoff.setDate(cutoff.getDate() - days);
  return new Date(dateStr) >= cutoff;
}

function applyDoneFilter(orders: WorkOrderDto[], done: DoneWindow) {
  const completed = orders.filter((w) => w.status === 'Completed');
  if (done === 'all') return completed;
  return completed.filter((w) => withinDays(w.completedAt, done === '7d' ? 7 : 30));
}

// ─── Card components ──────────────────────────────────────────────────────────

function RequestCard({ request, onAdvance, advancing }: {
  request: MaintenanceRequestDto;
  onAdvance: () => void;
  advancing: boolean;
}) {
  return (
    <Card className="p-3 text-sm space-y-2">
      <div className="flex items-start justify-between gap-2">
        <div className="font-medium leading-snug">{request.title}</div>
        <Badge variant="destructive" className="shrink-0 text-[10px]">Request</Badge>
      </div>
      <div className="text-xs text-muted-foreground">
        {request.boaterName} · {new Date(request.submittedAt).toLocaleDateString()}
      </div>
      {request.description && (
        <div className="text-xs text-muted-foreground/70 line-clamp-2">{request.description}</div>
      )}
      <div className="flex items-center justify-between">
        <Badge variant={priorityVariant(request.priority)} className="text-[10px]">{request.priority}</Badge>
        {!['Completed', 'Declined'].includes(request.status) && (
          <Button variant="ghost" size="sm" className="h-6 text-xs px-2" onClick={onAdvance} disabled={advancing}>
            {advancing ? '…' : 'Advance'}
          </Button>
        )}
      </div>
    </Card>
  );
}

function WorkOrderCard({ order, onAdvance, advancing }: {
  order: WorkOrderDto;
  onAdvance?: () => void;
  advancing?: boolean;
}) {
  return (
    <Card className="p-3 text-sm space-y-2">
      <div className="flex items-start justify-between gap-2">
        <div className="font-medium leading-snug">{order.title}</div>
        <Badge variant="primary" className="shrink-0 text-[10px]">Work order</Badge>
      </div>
      {order.assignedToName && (
        <div className="text-xs text-muted-foreground">{order.assignedToName}</div>
      )}
      {order.scheduledDate && order.status !== 'Completed' && (
        <div className="text-xs text-muted-foreground/70">
          Scheduled {new Date(order.scheduledDate).toLocaleDateString()}
        </div>
      )}
      {order.completedAt && (
        <div className="text-xs text-emerald-600/80">
          Completed {new Date(order.completedAt).toLocaleDateString()}
        </div>
      )}
      {order.notes && order.status === 'Completed' && (
        <div className="text-xs bg-emerald-50 dark:bg-emerald-950/30 text-emerald-700 dark:text-emerald-300 rounded p-2">
          {order.notes}
        </div>
      )}
      <div className="flex items-center justify-between">
        <Badge variant={priorityVariant(order.priority)} className="text-[10px]">{order.priority}</Badge>
        {onAdvance && !['Completed', 'Cancelled'].includes(order.status) && (
          <Button variant="ghost" size="sm" className="h-6 text-xs px-2" onClick={onAdvance} disabled={advancing}>
            {advancing ? '…' : 'Advance'}
          </Button>
        )}
      </div>
    </Card>
  );
}

// ─── Kanban column ────────────────────────────────────────────────────────────

const COLUMN_TONES: Record<string, string> = {
  new:         'text-destructive',
  scheduled:   'text-accent-foreground',
  inprogress:  'text-primary',
  done:        'text-emerald-600 dark:text-emerald-400',
};

const COLUMN_BG: Record<string, string> = {
  new:         'bg-destructive/5',
  scheduled:   'bg-accent/10',
  inprogress:  'bg-primary/5',
  done:        'bg-emerald-50/50 dark:bg-emerald-950/20',
};

function KanbanColumn({
  id, label, count, onAdd, children,
  doneFilter, onDoneFilterChange,
}: {
  id: ColFilter;
  label: string;
  count: number;
  onAdd?: () => void;
  children: React.ReactNode;
  doneFilter?: DoneWindow;
  onDoneFilterChange?: (v: DoneWindow) => void;
}) {
  return (
    <div className={cn('flex-1 min-w-[200px] rounded-lg p-3 flex flex-col gap-2', COLUMN_BG[id])}>
      <div className="flex items-center justify-between">
        <div className={cn('text-xs font-semibold uppercase tracking-wider flex items-center gap-1.5', COLUMN_TONES[id])}>
          <span className={cn('size-2 rounded-full bg-current')} />
          {label}
          <span className="text-muted-foreground font-normal normal-case tracking-normal ml-0.5">{count}</span>
        </div>
        <div className="flex items-center gap-1">
          {id === 'done' && doneFilter && onDoneFilterChange && (
            <select
              value={doneFilter}
              onChange={(e) => onDoneFilterChange(e.target.value as DoneWindow)}
              className="text-[10px] border border-border rounded px-1.5 py-0.5 bg-card text-foreground/70 h-6"
            >
              <option value="7d">Last 7 days</option>
              <option value="30d">Last 30 days</option>
              <option value="all">All time</option>
            </select>
          )}
          {onAdd && (
            <Button variant="ghost" size="icon" className="size-6" onClick={onAdd}>
              <Plus className="size-3" />
            </Button>
          )}
        </div>
      </div>
      <div className="flex flex-col gap-2 overflow-y-auto max-h-[calc(100vh-280px)]">
        {count === 0
          ? <p className="text-xs text-muted-foreground/60 text-center py-4">No items</p>
          : children}
      </div>
    </div>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export function MaintenancePage() {
  const { marinaId = '' } = useParams({ strict: false }) as { marinaId: string };
  const navigate = useNavigate();
  const router = useRouter();
  const search = useSearch({ strict: false }) as { view?: string; done?: string; col?: string };
  const queryClient = useQueryClient();

  const [view, setView] = useUrlState<'board' | 'list'>('view', 'board');
  const [done, setDone] = useUrlState<DoneWindow>('done', '7d');
  const col = (search.col ?? null) as ColFilter | null;

  function clearCol() {
    navigate({
      to: router.state.location.pathname,
      search: (prev: Record<string, unknown>) => { const m = { ...prev }; delete m.col; return m; },
    });
  }

  const { data: requests = [], isLoading: loadingReqs } = useQuery({
    queryKey: ['marina-maintenance-requests', marinaId],
    queryFn: () => getMarinaMaintenanceRequests(marinaId),
    staleTime: 30_000,
  });

  const { data: workOrders = [], isLoading: loadingWOs } = useQuery({
    queryKey: ['marina-work-orders', marinaId],
    queryFn: () => getMarinaWorkOrders(marinaId),
    staleTime: 30_000,
  });

  const loading = loadingReqs || loadingWOs;

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['marina-maintenance-requests', marinaId] });
    queryClient.invalidateQueries({ queryKey: ['marina-work-orders', marinaId] });
    queryClient.invalidateQueries({ queryKey: ['marina-counters', marinaId] });
  }

  const advanceRequest = useMutation({
    mutationFn: (r: MaintenanceRequestDto) => {
      const next: Record<string, string> = { Submitted: 'UnderReview', UnderReview: 'InProgress', InProgress: 'Completed' };
      return updateMaintenanceRequestStatus(marinaId, r.id, { status: next[r.status] ?? r.status, priority: r.priority });
    },
    onSuccess: invalidate,
  });

  const advanceWorkOrder = useMutation({
    mutationFn: (w: WorkOrderDto) => {
      const next: Record<string, string> = { Open: 'InProgress', Scheduled: 'InProgress', InProgress: 'Completed' };
      return updateWorkOrder(marinaId, w.id, { status: next[w.status] ?? w.status });
    },
    onSuccess: invalidate,
  });

  const createWO = useMutation({
    mutationFn: (requestId?: string) => createWorkOrder(marinaId, {
      title: 'New work order', description: '', priority: 'Medium',
      maintenanceRequestId: requestId,
    }),
    onSuccess: invalidate,
  });

  // Partition data into columns
  const newItems = requests.filter((r) => !['Completed', 'Declined'].includes(r.status));
  const scheduledWOs = workOrders.filter((w) => w.status === 'Open' || w.status === 'Scheduled');
  const inProgressWOs = workOrders.filter((w) => w.status === 'InProgress');
  const completedWOs = applyDoneFilter(workOrders, done);

  const columns: { id: ColFilter; label: string; count: number }[] = [
    { id: 'new',        label: 'New',         count: newItems.length },
    { id: 'scheduled',  label: 'Scheduled',   count: scheduledWOs.length },
    { id: 'inprogress', label: 'In progress', count: inProgressWOs.length },
    { id: 'done',       label: 'Completed',   count: completedWOs.length },
  ];

  const visibleColumns = col ? columns.filter((c) => c.id === col) : columns;

  // ── Header actions: view toggle ──
  const viewToggle = (
    <div className="flex items-center border border-border rounded-lg overflow-hidden">
      <button
        type="button"
        onClick={() => setView('board')}
        className={cn('flex items-center gap-1.5 px-3 h-8 text-xs font-medium',
          view === 'board' ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-muted')}
      >
        <Columns3 className="size-3.5" /> Board
      </button>
      <button
        type="button"
        onClick={() => setView('list')}
        className={cn('flex items-center gap-1.5 px-3 h-8 text-xs font-medium',
          view === 'list' ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-muted')}
      >
        <LayoutList className="size-3.5" /> List
      </button>
    </div>
  );

  return (
    <>
      <PageHeader title="Maintenance" actions={viewToggle} />
      <PageBody>
        {/* Col filter banner */}
        {col && (
          <div className="mb-4 flex items-center gap-3 px-3 py-2 bg-primary/8 border border-primary/20 rounded-lg text-sm">
            <span className="flex-1 text-foreground/80">
              Filtered to <strong>{columns.find((c) => c.id === col)?.label ?? col}</strong>
            </span>
            <Button variant="ghost" size="sm" className="h-6 text-xs" onClick={clearCol}>
              <X className="size-3 mr-1" /> Clear
            </Button>
          </div>
        )}

        {loading && (
          <div className="flex gap-3">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="flex-1 h-48 rounded-lg bg-muted animate-pulse" />
            ))}
          </div>
        )}

        {/* Board view */}
        {!loading && view === 'board' && (
          <div className="flex gap-3 overflow-x-auto pb-2">
            {visibleColumns.map(({ id, label, count }) => (
              <KanbanColumn
                key={id} id={id} label={label} count={count}
                doneFilter={id === 'done' ? done : undefined}
                onDoneFilterChange={id === 'done' ? setDone : undefined}
                onAdd={id !== 'done' ? () => createWO.mutate(undefined) : undefined}
              >
                {id === 'new' && newItems.map((r) => (
                  <RequestCard
                    key={r.id} request={r}
                    onAdvance={() => advanceRequest.mutate(r)}
                    advancing={advanceRequest.isPending}
                  />
                ))}
                {id === 'scheduled' && scheduledWOs.map((w) => (
                  <WorkOrderCard
                    key={w.id} order={w}
                    onAdvance={() => advanceWorkOrder.mutate(w)}
                    advancing={advanceWorkOrder.isPending}
                  />
                ))}
                {id === 'inprogress' && inProgressWOs.map((w) => (
                  <WorkOrderCard
                    key={w.id} order={w}
                    onAdvance={() => advanceWorkOrder.mutate(w)}
                    advancing={advanceWorkOrder.isPending}
                  />
                ))}
                {id === 'done' && completedWOs.map((w) => (
                  <WorkOrderCard key={w.id} order={w} />
                ))}
              </KanbanColumn>
            ))}
          </div>
        )}

        {/* List view */}
        {!loading && view === 'list' && (
          <div className="rounded-lg border border-border overflow-hidden">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border bg-muted/40">
                  {['Item', 'Kind', 'Status', 'Priority', 'Reported / Assigned'].map((h) => (
                    <th key={h} className="px-3 py-2.5 text-left text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {newItems.filter((_) => !col || col === 'new').map((r) => (
                  <tr key={r.id} className="hover:bg-muted/30">
                    <td className="px-3 py-2.5 font-medium">{r.title}</td>
                    <td className="px-3 py-2.5"><Badge variant="destructive" className="text-[10px]">Request</Badge></td>
                    <td className="px-3 py-2.5 text-muted-foreground">{r.status}</td>
                    <td className="px-3 py-2.5"><Badge variant={priorityVariant(r.priority)} className="text-[10px]">{r.priority}</Badge></td>
                    <td className="px-3 py-2.5 text-muted-foreground text-xs">{r.boaterName}</td>
                  </tr>
                ))}
                {scheduledWOs.filter((_) => !col || col === 'scheduled').map((w) => (
                  <tr key={w.id} className="hover:bg-muted/30">
                    <td className="px-3 py-2.5 font-medium">{w.title}</td>
                    <td className="px-3 py-2.5"><Badge variant="primary" className="text-[10px]">Work order</Badge></td>
                    <td className="px-3 py-2.5 text-muted-foreground">Scheduled</td>
                    <td className="px-3 py-2.5"><Badge variant={priorityVariant(w.priority)} className="text-[10px]">{w.priority}</Badge></td>
                    <td className="px-3 py-2.5 text-muted-foreground text-xs">{w.assignedToName ?? '—'}</td>
                  </tr>
                ))}
                {inProgressWOs.filter((_) => !col || col === 'inprogress').map((w) => (
                  <tr key={w.id} className="hover:bg-muted/30">
                    <td className="px-3 py-2.5 font-medium">{w.title}</td>
                    <td className="px-3 py-2.5"><Badge variant="primary" className="text-[10px]">Work order</Badge></td>
                    <td className="px-3 py-2.5 text-muted-foreground">In progress</td>
                    <td className="px-3 py-2.5"><Badge variant={priorityVariant(w.priority)} className="text-[10px]">{w.priority}</Badge></td>
                    <td className="px-3 py-2.5 text-muted-foreground text-xs">{w.assignedToName ?? '—'}</td>
                  </tr>
                ))}
                {completedWOs.filter((_) => !col || col === 'done').map((w) => (
                  <tr key={w.id} className="hover:bg-muted/30 opacity-70">
                    <td className="px-3 py-2.5 font-medium">{w.title}</td>
                    <td className="px-3 py-2.5"><Badge variant="primary" className="text-[10px]">Work order</Badge></td>
                    <td className="px-3 py-2.5 text-muted-foreground">Completed</td>
                    <td className="px-3 py-2.5"><Badge variant={priorityVariant(w.priority)} className="text-[10px]">{w.priority}</Badge></td>
                    <td className="px-3 py-2.5 text-muted-foreground text-xs">{w.assignedToName ?? '—'}</td>
                  </tr>
                ))}
                {newItems.length === 0 && scheduledWOs.length === 0 && inProgressWOs.length === 0 && completedWOs.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-3 py-12 text-center text-sm text-muted-foreground">
                      No maintenance items found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </PageBody>
    </>
  );
}
