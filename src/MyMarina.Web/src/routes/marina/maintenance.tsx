import { useState, useRef } from 'react';
import { useParams, useSearch, useNavigate } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { LayoutList, Columns3, Filter, Plus, X, ChevronDown, Check } from 'lucide-react';
import {
  getMarinaMaintenanceRequests, updateMaintenanceRequestStatus,
  getMarinaWorkOrders, updateWorkOrder, createWorkOrder, getMarinaStaff,
  type MaintenanceRequestDto, type WorkOrderDto, type MembershipDto,
} from '@/api/api';
import { PageHeader, PageBody } from '@/marina-workspace/PageHeader';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { useUrlState } from '@/hooks/useUrlState';
import { cn } from '@/lib/utils';

// ─── Types ────────────────────────────────────────────────────────────────────

type DoneWindow = '7d' | '30d' | 'all';
type ColFilter = 'new' | 'scheduled' | 'inprogress' | 'done';
type SelectedItem =
  | { kind: 'request'; item: MaintenanceRequestDto }
  | { kind: 'workorder'; item: WorkOrderDto };

interface DragState {
  id: string;
  kind: 'request' | 'workorder';
  fromCol: ColFilter;
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

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

function fmtDate(s: string | null | undefined) {
  if (!s) return null;
  return new Date(s).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

// ─── Initials avatar ──────────────────────────────────────────────────────────

function Initials({ name }: { name: string | null | undefined }) {
  if (!name) return null;
  const parts = name.trim().split(' ');
  const init = ((parts[0]?.[0] ?? '') + (parts[1]?.[0] ?? '')).toUpperCase();
  return (
    <span className="inline-flex items-center justify-center h-5 w-5 rounded-full bg-primary/15 text-primary text-[9px] font-semibold shrink-0">
      {init}
    </span>
  );
}

// ─── Staff selector ───────────────────────────────────────────────────────────

function StaffSelector({ marinaId, userId, name, onChange }: {
  marinaId: string;
  userId: string | null;
  name: string | null;
  onChange: (userId: string | null, name: string | null) => void;
}) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');

  const { data: staff = [] } = useQuery<MembershipDto[]>({
    queryKey: ['marina-staff', marinaId],
    queryFn: () => getMarinaStaff(marinaId),
    staleTime: 120_000,
  });

  const members = staff.filter((m) => !m.isPending);
  const filtered = members.filter((m) => {
    const fullName = `${m.userFirstName ?? ''} ${m.userLastName ?? ''}`.trim();
    const haystack = `${fullName} ${m.userEmail ?? ''}`.toLowerCase();
    return haystack.includes(search.toLowerCase());
  });

  function memberName(m: MembershipDto) {
    return `${m.userFirstName ?? ''} ${m.userLastName ?? ''}`.trim() || m.userEmail || 'Unknown';
  }

  return (
    <div className="relative">
      {/* Backdrop to close on outside click */}
      {open && <div className="fixed inset-0 z-40" onClick={() => { setOpen(false); setSearch(''); }} />}

      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="w-full text-left rounded-md border border-border bg-background px-3 py-2 text-sm flex items-center gap-2 focus:outline-none focus:ring-1 focus:ring-ring relative z-50"
      >
        {name ? (
          <>
            <Initials name={name} />
            <span className="flex-1">{name}</span>
          </>
        ) : (
          <span className="flex-1 text-muted-foreground">Unassigned</span>
        )}
        <ChevronDown className="size-3.5 text-muted-foreground shrink-0" />
      </button>

      {open && (
        <div className="absolute top-full left-0 right-0 mt-1 z-50 rounded-md border border-border bg-popover shadow-lg overflow-hidden">
          <div className="p-2 border-b border-border">
            <input
              autoFocus
              placeholder="Search staff…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full text-sm px-2.5 py-1.5 rounded border border-border bg-background focus:outline-none focus:ring-1 focus:ring-ring"
            />
          </div>
          <div className="max-h-52 overflow-y-auto">
            <button
              type="button"
              onClick={() => { onChange(null, null); setOpen(false); setSearch(''); }}
              className="w-full text-left px-3 py-2 text-sm hover:bg-muted flex items-center gap-2 text-muted-foreground"
            >
              <X className="size-3.5" /> Unassigned
            </button>
            {filtered.map((m) => {
              const n = memberName(m);
              return (
                <button
                  key={m.id}
                  type="button"
                  onClick={() => { onChange(m.userId, n); setOpen(false); setSearch(''); }}
                  className="w-full text-left px-3 py-2 text-sm hover:bg-muted flex items-center gap-2"
                >
                  <Initials name={n} />
                  <div className="flex-1 min-w-0">
                    <div>{n}</div>
                    {m.userEmail && <div className="text-xs text-muted-foreground truncate">{m.userEmail}</div>}
                  </div>
                  {userId === m.userId && <Check className="size-3.5 text-primary shrink-0" />}
                </button>
              );
            })}
            {filtered.length === 0 && (
              <p className="px-3 py-4 text-xs text-muted-foreground text-center">No staff found</p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Detail / edit sheet ──────────────────────────────────────────────────────

function initForm(selected: SelectedItem | null) {
  if (!selected) return { title: '', description: '', priority: 'Medium', status: '', assignedToUserId: '', assignedToName: '', scheduledDate: '', notes: '' };
  if (selected.kind === 'workorder') {
    const w = selected.item;
    return {
      title: w.title,
      description: w.description ?? '',
      priority: w.priority,
      status: w.status,
      assignedToUserId: w.assignedToUserId ?? '',
      assignedToName: w.assignedToName ?? '',
      scheduledDate: w.scheduledDate ? w.scheduledDate.split('T')[0] : '',
      notes: w.notes ?? '',
    };
  }
  const r = selected.item;
  return { title: r.title, description: r.description ?? '', priority: r.priority, status: r.status, assignedToUserId: '', assignedToName: '', scheduledDate: '', notes: '' };
}

function DetailSheet({ open, selected, marinaId, onClose, onSaved }: {
  open: boolean;
  selected: SelectedItem | null;
  marinaId: string;
  onClose: () => void;
  onSaved: () => void;
}) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState(() => initForm(selected));

  const updateWO = useMutation({
    mutationFn: (data: Partial<WorkOrderDto>) => updateWorkOrder(marinaId, selected!.item.id, data),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['marina-work-orders', marinaId] }); onSaved(); },
  });

  const updateReq = useMutation({
    mutationFn: (data: { status: string; priority: string }) =>
      updateMaintenanceRequestStatus(marinaId, selected!.item.id, data),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['marina-maintenance-requests', marinaId] }); onSaved(); },
  });

  function handleSave() {
    if (!selected) return;
    if (selected.kind === 'workorder') {
      updateWO.mutate({
        title: form.title,
        description: form.description,
        priority: form.priority,
        status: form.status,
        assignedToUserId: form.assignedToUserId || null,
        assignedToName: form.assignedToName || null,
        scheduledDate: form.scheduledDate || null,
        notes: form.notes || null,
      });
    } else {
      updateReq.mutate({ status: form.status, priority: form.priority });
    }
  }

  const isSaving = updateWO.isPending || updateReq.isPending;
  const isWO = selected?.kind === 'workorder';
  const req = selected?.kind === 'request' ? selected.item : null;

  const inp = 'w-full rounded-md border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-ring';
  const lbl = 'block text-xs font-medium text-muted-foreground mb-1';

  return (
    <Sheet open={open} onOpenChange={(v) => !v && onClose()}>
      <SheetContent side="right" className="w-full sm:max-w-md flex flex-col p-0">
        {selected && (
          <>
            <SheetHeader className="px-5 pt-5 pb-4 border-b border-border space-y-2">
              <div className="flex items-center gap-2">
                <Badge variant={isWO ? 'primary' : 'destructive'} className="text-[10px]">
                  {isWO ? 'Work order' : 'Request'}
                </Badge>
                {req && <span className="text-xs text-muted-foreground">from {req.boaterName}</span>}
              </div>
              <SheetTitle className="text-base leading-snug">{selected.item.title}</SheetTitle>
            </SheetHeader>

            <div className="flex-1 overflow-y-auto p-5 space-y-4">
              {isWO && (
                <div>
                  <label className={lbl}>Title</label>
                  <input className={inp} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} />
                </div>
              )}

              <div>
                <label className={lbl}>Description</label>
                <textarea rows={3} className={cn(inp, 'resize-none')} value={form.description} readOnly={!isWO}
                  onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className={lbl}>Priority</label>
                  <select className={inp} value={form.priority} onChange={(e) => setForm((f) => ({ ...f, priority: e.target.value }))}>
                    {['Urgent', 'High', 'Medium', 'Low'].map((p) => <option key={p} value={p}>{p}</option>)}
                  </select>
                </div>
                <div>
                  <label className={lbl}>Status</label>
                  <select className={inp} value={form.status} onChange={(e) => setForm((f) => ({ ...f, status: e.target.value }))}>
                    {(isWO
                      ? ['Open', 'Scheduled', 'InProgress', 'Completed', 'Cancelled']
                      : ['Submitted', 'UnderReview', 'InProgress', 'Completed', 'Declined']
                    ).map((s) => <option key={s} value={s}>{s === 'InProgress' ? 'In progress' : s === 'UnderReview' ? 'Under review' : s}</option>)}
                  </select>
                </div>
              </div>

              {isWO && (
                <>
                  <div>
                    <label className={lbl}>Assigned to</label>
                    <StaffSelector
                      marinaId={marinaId}
                      userId={form.assignedToUserId || null}
                      name={form.assignedToName || null}
                      onChange={(userId, name) => setForm((f) => ({ ...f, assignedToUserId: userId ?? '', assignedToName: name ?? '' }))}
                    />
                  </div>
                  <div>
                    <label className={lbl}>Scheduled date</label>
                    <input type="date" className={inp} value={form.scheduledDate}
                      onChange={(e) => setForm((f) => ({ ...f, scheduledDate: e.target.value }))} />
                  </div>
                  <Separator />
                  <div>
                    <label className={lbl}>Resolution notes</label>
                    <textarea rows={4} className={cn(inp, 'resize-none')} placeholder="What was done, parts used..."
                      value={form.notes} onChange={(e) => setForm((f) => ({ ...f, notes: e.target.value }))} />
                  </div>
                </>
              )}
            </div>

            <div className="px-5 py-4 border-t border-border">
              <Button className="w-full" onClick={handleSave} disabled={isSaving}>
                {isSaving ? 'Saving…' : 'Save changes'}
              </Button>
            </div>
          </>
        )}
      </SheetContent>
    </Sheet>
  );
}

// ─── New work order dialog ────────────────────────────────────────────────────

function NewWorkOrderDialog({ open, marinaId, onClose, onCreated }: {
  open: boolean;
  marinaId: string;
  onClose: () => void;
  onCreated: () => void;
}) {
  const [form, setForm] = useState({ title: '', description: '', priority: 'Medium', scheduledDate: '' });

  const mutation = useMutation({
    mutationFn: () => createWorkOrder(marinaId, {
      title: form.title,
      description: form.description,
      priority: form.priority,
      scheduledDate: form.scheduledDate || undefined,
    }),
    onSuccess: () => {
      setForm({ title: '', description: '', priority: 'Medium', scheduledDate: '' });
      onCreated();
    },
  });

  const inp = 'w-full rounded-md border border-border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-ring';
  const lbl = 'block text-xs font-medium text-muted-foreground mb-1';

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>New work order</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <div>
            <label className={lbl}>Title *</label>
            <input className={inp} value={form.title} placeholder="e.g. Replace dock B cleat"
              onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} />
          </div>
          <div>
            <label className={lbl}>Description</label>
            <textarea rows={3} className={cn(inp, 'resize-none')} value={form.description}
              onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className={lbl}>Priority</label>
              <select className={inp} value={form.priority} onChange={(e) => setForm((f) => ({ ...f, priority: e.target.value }))}>
                {['Urgent', 'High', 'Medium', 'Low'].map((p) => <option key={p} value={p}>{p}</option>)}
              </select>
            </div>
            <div>
              <label className={lbl}>Scheduled date</label>
              <input type="date" className={inp} value={form.scheduledDate}
                onChange={(e) => setForm((f) => ({ ...f, scheduledDate: e.target.value }))} />
            </div>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button onClick={() => mutation.mutate()} disabled={!form.title.trim() || mutation.isPending}>
            {mutation.isPending ? 'Creating…' : 'Create'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── Cards ────────────────────────────────────────────────────────────────────

function RequestCard({ request, onOpen, isDragging }: {
  request: MaintenanceRequestDto;
  onOpen: () => void;
  isDragging: boolean;
}) {
  return (
    <div
      onClick={onOpen}
      className={cn(
        'rounded-lg bg-card border border-border p-3 text-sm space-y-2.5 cursor-pointer select-none',
        'hover:border-border/80 hover:shadow-sm transition-all',
        isDragging && 'opacity-40 ring-2 ring-primary/30',
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <span className="text-[10px] font-semibold bg-destructive/10 text-destructive px-1.5 py-0.5 rounded">Request</span>
      </div>
      <div className="font-medium leading-snug">{request.title}</div>
      <div className="text-xs text-muted-foreground">
        {request.boaterName} · {fmtDate(request.submittedAt)}
      </div>
      <div className="flex items-center justify-between gap-2">
        <span className="text-xs text-muted-foreground/60">Unassigned</span>
        <Badge variant={priorityVariant(request.priority)} className="text-[10px]">{request.priority}</Badge>
      </div>
    </div>
  );
}

function WorkOrderCard({ order, onOpen, isDragging }: {
  order: WorkOrderDto;
  onOpen: () => void;
  isDragging: boolean;
}) {
  const isCompleted = order.status === 'Completed';
  return (
    <div
      onClick={onOpen}
      className={cn(
        'rounded-lg bg-card border border-border p-3 text-sm space-y-2.5 cursor-pointer select-none',
        'hover:border-border/80 hover:shadow-sm transition-all',
        isCompleted && 'opacity-75',
        isDragging && 'opacity-40 ring-2 ring-primary/30',
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <span className="text-[10px] font-semibold bg-primary/10 text-primary px-1.5 py-0.5 rounded">Work order</span>
      </div>
      <div className="font-medium leading-snug">{order.title}</div>
      {(order.assignedToName || order.scheduledDate || order.completedAt) && (
        <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
          {order.assignedToName && <Initials name={order.assignedToName} />}
          <span>
            {order.assignedToName ?? 'Unassigned'}
            {order.scheduledDate && !isCompleted && ` · ${fmtDate(order.scheduledDate)}`}
            {isCompleted && order.completedAt && ` · ${fmtDate(order.completedAt)}`}
          </span>
        </div>
      )}
      {!order.assignedToName && !order.scheduledDate && (
        <div className="text-xs text-muted-foreground/60">Unassigned</div>
      )}
      {isCompleted && order.notes && (
        <div className="text-xs bg-emerald-50 dark:bg-emerald-950/30 text-emerald-700 dark:text-emerald-300 rounded px-2 py-1.5 line-clamp-2">
          ✓ {order.notes}
        </div>
      )}
      <div className="flex items-center justify-between gap-2">
        <span />
        <Badge variant={priorityVariant(order.priority)} className="text-[10px]">{order.priority}</Badge>
      </div>
    </div>
  );
}

// ─── Kanban column ────────────────────────────────────────────────────────────

const COL_TONES: Record<ColFilter, string> = {
  new:        'text-destructive',
  scheduled:  'text-foreground',
  inprogress: 'text-primary',
  done:       'text-emerald-600 dark:text-emerald-400',
};

const COL_DOT: Record<ColFilter, string> = {
  new:        'bg-destructive',
  scheduled:  'bg-foreground/50',
  inprogress: 'bg-primary',
  done:       'bg-emerald-500',
};

const COL_BG: Record<ColFilter, string> = {
  new:        'bg-destructive/5',
  scheduled:  'bg-muted/40',
  inprogress: 'bg-primary/5',
  done:       'bg-emerald-50/50 dark:bg-emerald-950/20',
};

function KanbanColumn({
  id, label, count, dragOver,
  onDragOver, onDragLeave, onDrop,
  doneFilter, onDoneFilterChange, children,
}: {
  id: ColFilter;
  label: string;
  count: number;
  dragOver: boolean;
  onDragOver: (e: React.DragEvent) => void;
  onDragLeave: () => void;
  onDrop: (e: React.DragEvent) => void;
  doneFilter?: DoneWindow;
  onDoneFilterChange?: (v: DoneWindow) => void;
  children: React.ReactNode;
}) {
  return (
    <div
      onDragOver={onDragOver}
      onDragLeave={onDragLeave}
      onDrop={onDrop}
      className={cn(
        'flex-1 min-w-[200px] rounded-xl p-3 flex flex-col gap-2 transition-colors',
        COL_BG[id],
        dragOver && 'ring-2 ring-primary/40 bg-primary/8',
      )}
    >
      {/* Column header */}
      <div className="flex items-center justify-between px-0.5 pb-1">
        <div className={cn('text-xs font-semibold flex items-center gap-1.5', COL_TONES[id])}>
          <span className={cn('size-1.5 rounded-full', COL_DOT[id])} />
          {label}
          <span className="text-muted-foreground font-normal ml-0.5">{count}</span>
        </div>
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
      </div>

      {/* Cards */}
      <div className="flex flex-col gap-2 overflow-y-auto max-h-[calc(100vh-260px)]">
        {count === 0
          ? <p className="text-xs text-muted-foreground/50 text-center py-6">No items</p>
          : children}
      </div>
    </div>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

const PRIORITIES = ['Urgent', 'High', 'Medium', 'Low'] as const;

export function MaintenancePage() {
  const { marinaId = '' } = useParams({ strict: false }) as { marinaId: string };
  const navigate = useNavigate();
  const search = useSearch({ strict: false }) as { view?: string; done?: string; col?: string };
  const queryClient = useQueryClient();

  const [view, setView] = useUrlState<'board' | 'list'>('view', 'board');
  const [done, setDone] = useUrlState<DoneWindow>('done', '7d');
  const col = (search.col ?? null) as ColFilter | null;

  const [selected, setSelected] = useState<SelectedItem | null>(null);
  const [newWOOpen, setNewWOOpen] = useState(false);
  const [showFilter, setShowFilter] = useState(false);
  const [filterPriorities, setFilterPriorities] = useState<string[]>([]);
  const [dragOverCol, setDragOverCol] = useState<ColFilter | null>(null);
  const dragItem = useRef<DragState | null>(null);

  function clearCol() {
    navigate({
      to: '/marina/$marinaId/maintenance',
      params: { marinaId },
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

  const convertToWO = useMutation({
    mutationFn: (r: MaintenanceRequestDto) =>
      createWorkOrder(marinaId, { title: r.title, description: r.description, priority: r.priority, maintenanceRequestId: r.id }),
    onSuccess: invalidate,
  });

  const completeRequest = useMutation({
    mutationFn: (r: MaintenanceRequestDto) =>
      updateMaintenanceRequestStatus(marinaId, r.id, { status: 'Completed', priority: r.priority }),
    onSuccess: invalidate,
  });

  const moveWO = useMutation({
    mutationFn: ({ wo, status }: { wo: WorkOrderDto; status: string }) =>
      updateWorkOrder(marinaId, wo.id, { ...wo, status }),
    onSuccess: invalidate,
  });

  // ── Drag and drop ──────────────────────────────────────────────────────────

  function onCardDragStart(state: DragState) {
    dragItem.current = state;
  }

  function onCardDragEnd() {
    dragItem.current = null;
    setDragOverCol(null);
  }

  function onColDragOver(e: React.DragEvent, colId: ColFilter) {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    setDragOverCol(colId);
  }

  function onColDrop(_e: React.DragEvent, toCol: ColFilter) {
    _e.preventDefault();
    setDragOverCol(null);
    const drag = dragItem.current;
    if (!drag || drag.fromCol === toCol) { dragItem.current = null; return; }
    dragItem.current = null;

    if (drag.kind === 'request') {
      const req = requests.find((r) => r.id === drag.id);
      if (!req) return;
      if (toCol === 'scheduled') convertToWO.mutate(req);
      else if (toCol === 'done') completeRequest.mutate(req);
      return;
    }

    if (toCol === 'new') return;
    const wo = workOrders.find((w) => w.id === drag.id);
    if (!wo) return;
    const statusMap: Record<Exclude<ColFilter, 'new'>, string> = {
      scheduled: 'Open', inprogress: 'InProgress', done: 'Completed',
    };
    moveWO.mutate({ wo, status: statusMap[toCol] });
  }

  // ── Filter ─────────────────────────────────────────────────────────────────

  function togglePriority(p: string) {
    setFilterPriorities((prev) =>
      prev.includes(p) ? prev.filter((x) => x !== p) : [...prev, p]
    );
  }

  function applyPriorityFilter<T extends { priority: string }>(items: T[]): T[] {
    if (filterPriorities.length === 0) return items;
    return items.filter((item) => filterPriorities.includes(item.priority));
  }

  // ── Partition data ─────────────────────────────────────────────────────────

  const rawNew        = requests.filter((r) => !['Completed', 'Declined'].includes(r.status));
  const rawScheduled  = workOrders.filter((w) => w.status === 'Open' || w.status === 'Scheduled');
  const rawInProgress = workOrders.filter((w) => w.status === 'InProgress');
  const rawDone       = applyDoneFilter(workOrders, done);

  const newItems        = applyPriorityFilter(rawNew);
  const scheduledWOs    = applyPriorityFilter(rawScheduled);
  const inProgressWOs   = applyPriorityFilter(rawInProgress);
  const completedWOs    = applyPriorityFilter(rawDone);

  const columns: { id: ColFilter; label: string; count: number }[] = [
    { id: 'new',        label: 'New',         count: newItems.length },
    { id: 'scheduled',  label: 'Scheduled',   count: scheduledWOs.length },
    { id: 'inprogress', label: 'In progress', count: inProgressWOs.length },
    { id: 'done',       label: 'Completed',   count: completedWOs.length },
  ];

  const visibleColumns = col ? columns.filter((c) => c.id === col) : columns;

  const subtitle = [
    rawNew.length > 0 && `${rawNew.length} new request${rawNew.length !== 1 ? 's' : ''}`,
    rawInProgress.length > 0 && `${rawInProgress.length} open work order${rawInProgress.length !== 1 ? 's' : ''}`,
  ].filter(Boolean).join(' · ') || undefined;

  // ── Header actions ─────────────────────────────────────────────────────────

  const headerActions = (
    <div className="flex items-center gap-2">
      {/* View toggle */}
      <div className="flex items-center border border-border rounded-lg overflow-hidden">
        <button type="button" onClick={() => setView('board')}
          className={cn('flex items-center gap-1.5 px-3 h-8 text-xs font-medium',
            view === 'board' ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-muted')}>
          <Columns3 className="size-3.5" /> Board
        </button>
        <button type="button" onClick={() => setView('list')}
          className={cn('flex items-center gap-1.5 px-3 h-8 text-xs font-medium',
            view === 'list' ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-muted')}>
          <LayoutList className="size-3.5" /> List
        </button>
      </div>

      <Button size="sm" variant="outline"
        onClick={() => setShowFilter((v) => !v)}
        className={cn(showFilter && 'bg-primary/10 border-primary/30 text-primary')}>
        <Filter className="size-3.5 mr-1.5" />
        Filter
        {filterPriorities.length > 0 && (
          <span className="ml-1.5 text-[10px] font-semibold bg-primary text-primary-foreground rounded-full h-4 w-4 flex items-center justify-center">
            {filterPriorities.length}
          </span>
        )}
      </Button>

      <Button size="sm" onClick={() => setNewWOOpen(true)}>
        <Plus className="size-3.5 mr-1.5" /> New work order
      </Button>
    </div>
  );

  return (
    <>
      <PageHeader title="Maintenance" subtitle={subtitle} actions={headerActions} />
      <PageBody>
        {/* Filter bar */}
        {showFilter && (
          <div className="mb-4 flex items-center gap-2 flex-wrap p-3 rounded-lg border border-border bg-muted/30">
            <span className="text-xs font-medium text-muted-foreground mr-1">Priority:</span>
            {PRIORITIES.map((p) => (
              <button
                key={p}
                type="button"
                onClick={() => togglePriority(p)}
                className={cn(
                  'text-xs px-2.5 py-1 rounded-full border transition-colors',
                  filterPriorities.includes(p)
                    ? 'bg-primary text-primary-foreground border-primary'
                    : 'border-border text-foreground/70 hover:bg-muted',
                )}
              >
                {p}
              </button>
            ))}
            {filterPriorities.length > 0 && (
              <button type="button" onClick={() => setFilterPriorities([])}
                className="ml-auto text-xs text-muted-foreground hover:text-foreground flex items-center gap-1">
                <X className="size-3" /> Clear
              </button>
            )}
          </div>
        )}

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
            {[1, 2, 3, 4].map((i) => <div key={i} className="flex-1 h-48 rounded-xl bg-muted animate-pulse" />)}
          </div>
        )}

        {/* Board view */}
        {!loading && view === 'board' && (
          <div className="flex gap-3 overflow-x-auto pb-2">
            {visibleColumns.map(({ id, label, count }) => (
              <KanbanColumn
                key={id} id={id} label={label} count={count}
                dragOver={dragOverCol === id}
                onDragOver={(e) => onColDragOver(e, id)}
                onDragLeave={() => setDragOverCol(null)}
                onDrop={(e) => onColDrop(e, id)}
                doneFilter={id === 'done' ? done : undefined}
                onDoneFilterChange={id === 'done' ? setDone : undefined}
              >
                {id === 'new' && newItems.map((r) => (
                  <div key={r.id} draggable
                    onDragStart={() => onCardDragStart({ id: r.id, kind: 'request', fromCol: 'new' })}
                    onDragEnd={onCardDragEnd}>
                    <RequestCard request={r} isDragging={dragItem.current?.id === r.id}
                      onOpen={() => setSelected({ kind: 'request', item: r })} />
                  </div>
                ))}
                {id === 'scheduled' && scheduledWOs.map((w) => (
                  <div key={w.id} draggable
                    onDragStart={() => onCardDragStart({ id: w.id, kind: 'workorder', fromCol: 'scheduled' })}
                    onDragEnd={onCardDragEnd}>
                    <WorkOrderCard order={w} isDragging={dragItem.current?.id === w.id}
                      onOpen={() => setSelected({ kind: 'workorder', item: w })} />
                  </div>
                ))}
                {id === 'inprogress' && inProgressWOs.map((w) => (
                  <div key={w.id} draggable
                    onDragStart={() => onCardDragStart({ id: w.id, kind: 'workorder', fromCol: 'inprogress' })}
                    onDragEnd={onCardDragEnd}>
                    <WorkOrderCard order={w} isDragging={dragItem.current?.id === w.id}
                      onOpen={() => setSelected({ kind: 'workorder', item: w })} />
                  </div>
                ))}
                {id === 'done' && completedWOs.map((w) => (
                  <div key={w.id} draggable
                    onDragStart={() => onCardDragStart({ id: w.id, kind: 'workorder', fromCol: 'done' })}
                    onDragEnd={onCardDragEnd}>
                    <WorkOrderCard order={w} isDragging={dragItem.current?.id === w.id}
                      onOpen={() => setSelected({ kind: 'workorder', item: w })} />
                  </div>
                ))}
              </KanbanColumn>
            ))}
          </div>
        )}

        {/* List view */}
        {!loading && view === 'list' && (
          <div className="rounded-xl border border-border overflow-hidden">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border bg-muted/40">
                  {['Item', 'Kind', 'Status', 'Priority', 'Assigned to'].map((h) => (
                    <th key={h} className="px-3 py-2.5 text-left text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {newItems.filter(() => !col || col === 'new').map((r) => (
                  <tr key={r.id} className="hover:bg-muted/30 cursor-pointer"
                    onClick={() => setSelected({ kind: 'request', item: r })}>
                    <td className="px-3 py-2.5 font-medium">{r.title}</td>
                    <td className="px-3 py-2.5"><Badge variant="destructive" className="text-[10px]">Request</Badge></td>
                    <td className="px-3 py-2.5 text-muted-foreground text-xs">{r.status}</td>
                    <td className="px-3 py-2.5"><Badge variant={priorityVariant(r.priority)} className="text-[10px]">{r.priority}</Badge></td>
                    <td className="px-3 py-2.5 text-muted-foreground text-xs">{r.boaterName}</td>
                  </tr>
                ))}
                {scheduledWOs.filter(() => !col || col === 'scheduled').map((w) => (
                  <tr key={w.id} className="hover:bg-muted/30 cursor-pointer"
                    onClick={() => setSelected({ kind: 'workorder', item: w })}>
                    <td className="px-3 py-2.5 font-medium">{w.title}</td>
                    <td className="px-3 py-2.5"><Badge variant="primary" className="text-[10px]">Work order</Badge></td>
                    <td className="px-3 py-2.5 text-muted-foreground text-xs">Scheduled</td>
                    <td className="px-3 py-2.5"><Badge variant={priorityVariant(w.priority)} className="text-[10px]">{w.priority}</Badge></td>
                    <td className="px-3 py-2.5 text-xs">
                      {w.assignedToName
                        ? <span className="flex items-center gap-1.5 text-muted-foreground"><Initials name={w.assignedToName} />{w.assignedToName}</span>
                        : <span className="text-muted-foreground/50">—</span>}
                    </td>
                  </tr>
                ))}
                {inProgressWOs.filter(() => !col || col === 'inprogress').map((w) => (
                  <tr key={w.id} className="hover:bg-muted/30 cursor-pointer"
                    onClick={() => setSelected({ kind: 'workorder', item: w })}>
                    <td className="px-3 py-2.5 font-medium">{w.title}</td>
                    <td className="px-3 py-2.5"><Badge variant="primary" className="text-[10px]">Work order</Badge></td>
                    <td className="px-3 py-2.5 text-muted-foreground text-xs">In progress</td>
                    <td className="px-3 py-2.5"><Badge variant={priorityVariant(w.priority)} className="text-[10px]">{w.priority}</Badge></td>
                    <td className="px-3 py-2.5 text-xs">
                      {w.assignedToName
                        ? <span className="flex items-center gap-1.5 text-muted-foreground"><Initials name={w.assignedToName} />{w.assignedToName}</span>
                        : <span className="text-muted-foreground/50">—</span>}
                    </td>
                  </tr>
                ))}
                {completedWOs.filter(() => !col || col === 'done').map((w) => (
                  <tr key={w.id} className="hover:bg-muted/30 cursor-pointer opacity-70"
                    onClick={() => setSelected({ kind: 'workorder', item: w })}>
                    <td className="px-3 py-2.5 font-medium">{w.title}</td>
                    <td className="px-3 py-2.5"><Badge variant="primary" className="text-[10px]">Work order</Badge></td>
                    <td className="px-3 py-2.5 text-muted-foreground text-xs">Completed</td>
                    <td className="px-3 py-2.5"><Badge variant={priorityVariant(w.priority)} className="text-[10px]">{w.priority}</Badge></td>
                    <td className="px-3 py-2.5 text-xs">
                      {w.assignedToName
                        ? <span className="flex items-center gap-1.5 text-muted-foreground"><Initials name={w.assignedToName} />{w.assignedToName}</span>
                        : <span className="text-muted-foreground/50">—</span>}
                    </td>
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

      {/* Detail sheet — keyed so form resets when item changes */}
      <DetailSheet
        key={selected?.item.id ?? 'empty'}
        open={!!selected}
        selected={selected}
        marinaId={marinaId}
        onClose={() => setSelected(null)}
        onSaved={() => { setSelected(null); invalidate(); }}
      />

      {/* New work order dialog */}
      <NewWorkOrderDialog
        open={newWOOpen}
        marinaId={marinaId}
        onClose={() => setNewWOOpen(false)}
        onCreated={() => { setNewWOOpen(false); invalidate(); }}
      />
    </>
  );
}
