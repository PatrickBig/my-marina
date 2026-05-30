import { useState, useRef, forwardRef, useImperativeHandle } from 'react';
import { useParams, useNavigate, useSearch } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ChevronLeft, ChevronRight, PauseCircle, PlayCircle, Trash2, Eye } from 'lucide-react';
import { toast } from 'sonner';
import {
  getDocks, getSlips, getAvailabilityWindows,
  createAvailabilityWindow, updateAvailabilityWindow, setAvailabilityWindowStatus,
  type DockDto, type SlipDto, type AvailabilityWindowDto, type AvailabilityWindowStatus,
} from '@/api/api';
import { PageHeader, PageBody } from '@/marina-workspace/PageHeader';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { cn } from '@/lib/utils';

interface DateRange { start: Date; end: Date; }

// ─── Window color palette ─────────────────────────────────────────────────────

const PALETTE = [
  { openBg: 'bg-rose-100 dark:bg-rose-950/40',    pausedBg: 'bg-slate-100 dark:bg-slate-800/50',    price: 'text-rose-600 dark:text-rose-400',    dot: 'bg-rose-400',    chip: 'bg-rose-100 dark:bg-rose-950/50 text-rose-700 dark:text-rose-300' },
  { openBg: 'bg-blue-100 dark:bg-blue-950/40',    pausedBg: 'bg-slate-100 dark:bg-slate-800/50',    price: 'text-blue-600 dark:text-blue-400',    dot: 'bg-blue-400',    chip: 'bg-blue-100 dark:bg-blue-950/50 text-blue-700 dark:text-blue-300' },
  { openBg: 'bg-emerald-100 dark:bg-emerald-950/40', pausedBg: 'bg-slate-100 dark:bg-slate-800/50', price: 'text-emerald-600 dark:text-emerald-400', dot: 'bg-emerald-400', chip: 'bg-emerald-100 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300' },
  { openBg: 'bg-violet-100 dark:bg-violet-950/40',  pausedBg: 'bg-slate-100 dark:bg-slate-800/50',  price: 'text-violet-600 dark:text-violet-400',  dot: 'bg-violet-400',  chip: 'bg-violet-100 dark:bg-violet-950/50 text-violet-700 dark:text-violet-300' },
];

// ─── Utilities ────────────────────────────────────────────────────────────────

function buildCalendarWeeks(year: number, month: number): (Date | null)[][] {
  const firstDow = new Date(year, month, 1).getDay();
  const days = new Date(year, month + 1, 0).getDate();
  const cells: (Date | null)[] = [
    ...Array<null>(firstDow).fill(null),
    ...Array.from({ length: days }, (_, i) => new Date(year, month, i + 1)),
  ];
  while (cells.length % 7 !== 0) cells.push(null);
  const weeks: (Date | null)[][] = [];
  for (let i = 0; i < cells.length; i += 7) weeks.push(cells.slice(i, i + 7));
  return weeks;
}

function windowForDate(date: Date, windows: AvailabilityWindowDto[]): { w: AvailabilityWindowDto; idx: number } | null {
  for (let idx = 0; idx < windows.length; idx++) {
    const w = windows[idx];
    const s = new Date(w.startsAt); s.setHours(0, 0, 0, 0);
    const e = new Date(w.endsAt); e.setHours(23, 59, 59, 999);
    if (date >= s && date <= e) return { w, idx };
  }
  return null;
}

function toDateKey(d: Date) {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function fmtWindowHeader(w: AvailabilityWindowDto) {
  const s = new Date(w.startsAt);
  const e = new Date(w.endsAt);
  const fmt = (d: Date) => d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  return `${fmt(s)} – ${fmt(e)}`;
}

// ─── Calendar legend ──────────────────────────────────────────────────────────

function CalendarLegend({ windows }: { windows: AvailabilityWindowDto[] }) {
  if (windows.length === 0) return null;
  return (
    <div className="flex items-center gap-3 flex-wrap px-4 py-2.5 border-b border-border/40">
      {windows.map((w, idx) => {
        const c = PALETTE[idx % PALETTE.length];
        const isPaused = w.status === 'Paused';
        const isBooked = w.status === 'FullyBooked';
        return (
          <span key={w.id} className={cn('inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium', c.chip)}>
            <span className={cn('h-2 w-2 rounded-full', isBooked ? 'bg-gray-800 dark:bg-gray-200' : c.dot)} />
            Window {idx + 1}
            {!isBooked && ` · ${isPaused ? 'paused' : 'open'} · $${w.basePricePerNight}`}
            {isBooked && ' · booked'}
          </span>
        );
      })}
      <span className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium bg-gray-900 dark:bg-gray-950 text-gray-200">
        <span className="h-2 w-2 rounded-full bg-gray-400" />
        Booked
      </span>
    </div>
  );
}

// ─── Custom calendar grid ─────────────────────────────────────────────────────

function ListingCalendar({
  year, month, windows, dragRange, selectedWindowId,
  onPrevMonth, onNextMonth,
  onDragStart, onDragMove, onDragEnd,
  onWindowClick,
}: {
  year: number;
  month: number;
  windows: AvailabilityWindowDto[];
  dragRange: DateRange | null;
  selectedWindowId: string | null;
  onPrevMonth: () => void;
  onNextMonth: () => void;
  onDragStart: (d: Date) => void;
  onDragMove: (d: Date) => void;
  onDragEnd: () => void;
  onWindowClick: (w: AvailabilityWindowDto) => void;
}) {
  const weeks = buildCalendarWeeks(year, month);
  const monthLabel = new Date(year, month, 1).toLocaleDateString('en-US', { month: 'long', year: 'numeric' });

  return (
    <div className="rounded-xl border border-border bg-card overflow-hidden">
      {/* Month nav */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-border">
        <button onClick={onPrevMonth} className="p-1 rounded hover:bg-muted transition-colors">
          <ChevronLeft className="h-4 w-4" />
        </button>
        <span className="text-sm font-semibold">{monthLabel}</span>
        <button onClick={onNextMonth} className="p-1 rounded hover:bg-muted transition-colors">
          <ChevronRight className="h-4 w-4" />
        </button>
      </div>

      {/* Legend */}
      <CalendarLegend windows={windows} />

      {/* Weekday headers */}
      <div className="grid grid-cols-7 border-b border-border/40">
        {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map((d) => (
          <div key={d} className="py-2 text-center text-xs font-medium text-muted-foreground border-r border-border/30 last:border-r-0">
            {d}
          </div>
        ))}
      </div>

      {/* Day grid — pointer handlers on the wrapper for reliable drag tracking */}
      <div
        onPointerDown={(e) => {
          const el = (e.target as Element).closest('[data-date]');
          if (el && !el.getAttribute('data-window')) {
            e.currentTarget.setPointerCapture(e.pointerId);
            onDragStart(new Date(el.getAttribute('data-date')!));
          }
        }}
        onPointerMove={(e) => {
          if (e.buttons === 0) return;
          const el = document.elementFromPoint(e.clientX, e.clientY)?.closest('[data-date]');
          if (el) onDragMove(new Date(el.getAttribute('data-date')!));
        }}
        onPointerUp={onDragEnd}
        style={{ userSelect: 'none', touchAction: 'none' }}
      >
        {weeks.map((week, wi) => (
          <div key={wi} className="grid grid-cols-7 border-b border-border/30 last:border-b-0">
            {week.map((date, di) => {
              if (!date) {
                return (
                  <div key={`empty-${wi}-${di}`}
                    className="min-h-[86px] border-r border-border/30 last:border-r-0 bg-muted/10" />
                );
              }

              const dateKey = toDateKey(date);
              const match = windowForDate(date, windows);
              const colors = match ? PALETTE[match.idx % PALETTE.length] : null;
              const isBooked = match?.w.status === 'FullyBooked';
              const isPaused = match?.w.status === 'Paused';
              const isSelected = match?.w.id === selectedWindowId;
              const inDrag = !match && dragRange && date >= dragRange.start && date <= dragRange.end;

              return (
                <div
                  key={dateKey}
                  data-date={dateKey}
                  data-window={match?.w.id ?? ''}
                  onClick={() => match && onWindowClick(match.w)}
                  className={cn(
                    'relative min-h-[86px] p-1.5 border-r border-border/30 last:border-r-0 transition-colors',
                    isBooked ? 'bg-gray-900 dark:bg-gray-950' : null,
                    !isBooked && isPaused && colors ? colors.pausedBg : null,
                    !isBooked && !isPaused && colors ? colors.openBg : null,
                    inDrag ? 'bg-primary/15 dark:bg-primary/20' : null,
                    isSelected ? 'ring-2 ring-inset ring-primary/40' : null,
                    !match && !inDrag ? 'cursor-crosshair hover:bg-muted/30' : 'cursor-pointer',
                  )}
                >
                  <span className={cn('text-sm leading-none', isBooked ? 'text-gray-400' : 'text-foreground/70')}>
                    {date.getDate()}
                  </span>
                  {match && !isBooked && (
                    <span className={cn('absolute bottom-1.5 right-1.5 text-xs font-semibold', colors!.price)}>
                      ${match.w.basePricePerNight}
                    </span>
                  )}
                  {isPaused && (
                    <span className="absolute inset-0 border-2 border-dashed border-muted-foreground/30 pointer-events-none" />
                  )}
                  {isBooked && (
                    <span className="absolute bottom-1.5 inset-x-1 text-[10px] text-gray-500 text-center">booked</span>
                  )}
                </div>
              );
            })}
          </div>
        ))}
      </div>
    </div>
  );
}

// ─── Window editor panel ──────────────────────────────────────────────────────

interface WindowEditorHandle {
  save: () => void;
}

const WindowEditorPanel = forwardRef<WindowEditorHandle, {
  marinaId: string;
  window_: AvailabilityWindowDto | null;
  windowIndex: number;
  onSaved: () => void;
  onDeleted: () => void;
}>(function WindowEditorPanel({ marinaId, window_, windowIndex, onSaved, onDeleted }, ref) {
  const queryClient = useQueryClient();

  const [form, setForm] = useState({
    price:       window_ ? String(window_.basePricePerNight) : '',
    weeklyDisc:  window_?.weeklyDiscount  != null ? String(Math.round(window_.weeklyDiscount * 100))  : '',
    monthlyDisc: window_?.monthlyDiscount != null ? String(Math.round(window_.monthlyDiscount * 100)) : '',
    cleaningFee: window_ ? String(window_.cleaningFee ?? '') : '',
    minNts:      window_ ? String(window_.minNights ?? '') : '',
    maxNts:      window_ ? String(window_.maxNights ?? '') : '',
    instantBook: window_?.instantBook ?? true,
  });

  const updateMutation = useMutation({
    mutationFn: (data: Parameters<typeof updateAvailabilityWindow>[2]) =>
      updateAvailabilityWindow(marinaId, window_!.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['marina-windows', marinaId] });
      onSaved();
    },
  });

  const statusMutation = useMutation({
    mutationFn: (status: AvailabilityWindowStatus) =>
      setAvailabilityWindowStatus(marinaId, window_!.id, status),
    onSuccess: (_data, status) => {
      queryClient.invalidateQueries({ queryKey: ['marina-windows', marinaId] });
      if (status === 'Closed') onDeleted();
      else onSaved();
    },
  });

  function handleSave() {
    if (!window_) return;
    updateMutation.mutate({
      basePricePerNight: Number(form.price),
      weeklyDiscount:    form.weeklyDisc  ? Number(form.weeklyDisc)  / 100 : null,
      monthlyDiscount:   form.monthlyDisc ? Number(form.monthlyDisc) / 100 : null,
      cleaningFee:       form.cleaningFee ? Number(form.cleaningFee) : null,
      minNights:         form.minNts      ? Number(form.minNts)      : null,
      maxNights:         form.maxNts      ? Number(form.maxNts)      : null,
      instantBook:       form.instantBook,
    });
  }

  useImperativeHandle(ref, () => ({ save: handleSave }));

  if (!window_) {
    return (
      <div className="rounded-xl border border-border bg-card p-5 text-center space-y-2">
        <p className="text-xs font-medium text-foreground/70">No window selected</p>
        <p className="text-xs text-muted-foreground leading-relaxed">
          Drag across empty dates to create a window, or click a colored range to edit it.
        </p>
      </div>
    );
  }

  const inp = 'w-full rounded-md border border-border bg-background px-2.5 py-1.5 text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-ring';
  const lbl = 'block text-[10px] font-semibold uppercase tracking-wide text-muted-foreground mb-1';

  return (
    <div className="rounded-xl border border-border bg-card overflow-hidden flex flex-col">
      {/* Header */}
      <div className="px-4 py-3 border-b border-border">
        <p className="text-[10px] font-semibold uppercase tracking-wide text-muted-foreground mb-0.5">
          Window {windowIndex + 1} · {fmtWindowHeader(window_)}
        </p>
        <p className="text-sm font-semibold">Pricing &amp; policy</p>
      </div>

      {/* Fields */}
      <div className="p-4 space-y-3 flex-1 overflow-y-auto">
        {/* Base / night */}
        <div>
          <label className={lbl}>Base / night</label>
          <div className="relative">
            <span className="absolute inset-y-0 left-2.5 flex items-center text-muted-foreground text-sm">$</span>
            <input type="number" step="0.01" min="0" className={cn(inp, 'pl-6')}
              value={form.price} onChange={(e) => setForm({ ...form, price: e.target.value })} />
          </div>
        </div>

        {/* Discounts */}
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className={lbl}>Weekly disc.</label>
            <div className="relative">
              <input type="number" step="1" min="0" max="100" placeholder="0" className={cn(inp, 'pr-6')}
                value={form.weeklyDisc} onChange={(e) => setForm({ ...form, weeklyDisc: e.target.value })} />
              <span className="absolute inset-y-0 right-2.5 flex items-center text-muted-foreground text-sm">%</span>
            </div>
          </div>
          <div>
            <label className={lbl}>Monthly disc.</label>
            <div className="relative">
              <input type="number" step="1" min="0" max="100" placeholder="0" className={cn(inp, 'pr-6')}
                value={form.monthlyDisc} onChange={(e) => setForm({ ...form, monthlyDisc: e.target.value })} />
              <span className="absolute inset-y-0 right-2.5 flex items-center text-muted-foreground text-sm">%</span>
            </div>
          </div>
        </div>

        {/* Cleaning fee */}
        <div>
          <label className={lbl}>Cleaning fee</label>
          <div className="relative">
            <span className="absolute inset-y-0 left-2.5 flex items-center text-muted-foreground text-sm">$</span>
            <input type="number" step="0.01" min="0" placeholder="0" className={cn(inp, 'pl-6')}
              value={form.cleaningFee} onChange={(e) => setForm({ ...form, cleaningFee: e.target.value })} />
          </div>
        </div>

        {/* Min / max nights */}
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className={lbl}>Min nts</label>
            <input type="number" step="1" min="1" placeholder="1" className={inp}
              value={form.minNts} onChange={(e) => setForm({ ...form, minNts: e.target.value })} />
          </div>
          <div>
            <label className={lbl}>Max nts</label>
            <input type="number" step="1" min="1" placeholder="—" className={inp}
              value={form.maxNts} onChange={(e) => setForm({ ...form, maxNts: e.target.value })} />
          </div>
        </div>

        {/* Instant book toggle */}
        <div className="flex items-center justify-between py-0.5">
          <span className="text-sm text-foreground/80">Instant book</span>
          <button
            type="button"
            role="switch"
            aria-checked={form.instantBook}
            onClick={() => setForm((f) => ({ ...f, instantBook: !f.instantBook }))}
            className={cn(
              'relative inline-flex h-5 w-9 items-center rounded-full transition-colors focus:outline-none',
              form.instantBook ? 'bg-primary' : 'bg-muted-foreground/30',
            )}
          >
            <span className={cn(
              'inline-block h-3.5 w-3.5 rounded-full bg-white shadow transition-transform',
              form.instantBook ? 'translate-x-4' : 'translate-x-0.5',
            )} />
          </button>
        </div>

        {/* Status */}
        <div className="flex items-center justify-between py-0.5">
          <span className="text-sm text-foreground/80">Status</span>
          <span className={cn(
            'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium',
            window_.status === 'Open'   ? 'bg-green-100 text-green-700 dark:bg-green-950/50 dark:text-green-300' :
            window_.status === 'Paused' ? 'bg-amber-100 text-amber-700 dark:bg-amber-950/50 dark:text-amber-300' :
            'bg-muted text-muted-foreground',
          )}>
            <span className={cn('h-1.5 w-1.5 rounded-full',
              window_.status === 'Open' ? 'bg-green-500' :
              window_.status === 'Paused' ? 'bg-amber-500' : 'bg-muted-foreground'
            )} />
            {window_.status}
          </span>
        </div>

        <Separator />

        {/* Revenue split */}
        {window_.revenueSplit.length > 0 && (
          <div>
            <p className="text-[10px] font-semibold uppercase tracking-wide text-muted-foreground mb-1.5">Revenue split</p>
            <div className="space-y-0.5">
              {window_.revenueSplit.map((e, i) => (
                <div key={i} className="flex justify-between text-xs">
                  <span className="text-foreground/80 font-medium">{e.payeeKind}</span>
                  <span className="text-muted-foreground">{Math.round(e.percent * 100)}%</span>
                </div>
              ))}
            </div>
            <p className="text-[10px] text-muted-foreground/60 mt-1">(snapshotted at booking time)</p>
          </div>
        )}

        <Separator />

        {/* Pause / Resume / Delete */}
        <div className="flex gap-2">
          {window_.status === 'Open' ? (
            <Button size="sm" variant="outline" className="flex-1"
              onClick={() => statusMutation.mutate('Paused')}
              disabled={statusMutation.isPending}>
              <PauseCircle className="h-4 w-4 mr-1" />Pause window
            </Button>
          ) : window_.status === 'Paused' ? (
            <Button size="sm" variant="outline" className="flex-1"
              onClick={() => statusMutation.mutate('Open')}
              disabled={statusMutation.isPending}>
              <PlayCircle className="h-4 w-4 mr-1" />Resume
            </Button>
          ) : null}
          <Button size="sm" variant="outline"
            className={cn('text-destructive hover:text-destructive border-destructive/30 hover:bg-destructive/10', window_.status !== 'Open' && window_.status !== 'Paused' && 'flex-1')}
            onClick={() => statusMutation.mutate('Closed')}
            disabled={statusMutation.isPending || window_.status === 'Closed'}>
            <Trash2 className="h-3.5 w-3.5 mr-1" />Delete
          </Button>
        </div>
      </div>
    </div>
  );
});

// ─── Slip calendar view ───────────────────────────────────────────────────────

function SlipCalendarView({
  marinaId,
  slipId,
  slipName,
  onBack,
}: {
  marinaId: string;
  slipId: string;
  slipName: string;
  onBack: () => void;
}) {
  const navigate = useNavigate();
  const search = useSearch({ strict: false }) as { slipId?: string; slipName?: string; windowId?: string };
  const queryClient = useQueryClient();

  const today = new Date();
  const [year, setYear] = useState(today.getFullYear());
  const [month, setMonth] = useState(today.getMonth());

  const { data: rawWindows = [] } = useQuery<AvailabilityWindowDto[]>({
    queryKey: ['marina-windows', marinaId, slipId],
    queryFn: () => getAvailabilityWindows(marinaId, { slipId }),
    staleTime: 30_000,
  });
  const windows = rawWindows.filter(w => w.status !== 'Closed');

  // Drag state via refs — avoids stale closure issues with useCallback/useCallback deps
  const isDragging = useRef(false);
  const dragStartDate = useRef<Date | null>(null);
  const dragEndDate = useRef<Date | null>(null);
  const [dragRange, setDragRange] = useState<DateRange | null>(null);

  const windowsRef = useRef(windows);
  windowsRef.current = windows;

  const createMutation = useMutation({
    mutationFn: (range: DateRange) =>
      createAvailabilityWindow(marinaId, {
        slipId,
        listedByKind: 'Owner',
        listedByMarinaId: marinaId,
        startsAt: range.start.toISOString().split('T')[0],
        endsAt: range.end.toISOString().split('T')[0],
        basePricePerNight: 0,
        instantBook: true,
      }),
    onSuccess: (created) => {
      queryClient.invalidateQueries({ queryKey: ['marina-windows', marinaId, slipId] });
      navigate({
        to: '/marina/$marinaId/listings',
        params: { marinaId },
        search: { slipId, slipName, windowId: created.id },
        replace: false,
      });
    },
  });

  const editorRef = useRef<WindowEditorHandle>(null);

  const selectedWindowId = search.windowId ?? null;
  const selectedWindow = windows.find((w) => w.id === selectedWindowId) ?? null;
  const selectedWindowIndex = selectedWindow ? windows.indexOf(selectedWindow) : 0;

  function ordered(a: Date, b: Date): DateRange {
    return a <= b ? { start: a, end: b } : { start: b, end: a };
  }

  function handleDragStart(date: Date) {
    isDragging.current = true;
    dragStartDate.current = date;
    dragEndDate.current = date;
    setDragRange({ start: date, end: date });
  }

  function handleDragMove(date: Date) {
    if (!isDragging.current || !dragStartDate.current) return;
    dragEndDate.current = date;
    setDragRange(ordered(dragStartDate.current, date));
  }

  function handleDragEnd() {
    if (!isDragging.current || !dragStartDate.current || !dragEndDate.current) {
      isDragging.current = false;
      setDragRange(null);
      return;
    }
    isDragging.current = false;
    const range = ordered(dragStartDate.current, dragEndDate.current);
    dragStartDate.current = null;
    dragEndDate.current = null;
    setDragRange(null);

    const overlaps = windowsRef.current.some((w) => {
      const wStart = new Date(w.startsAt);
      const wEnd = new Date(w.endsAt);
      return range.start <= wEnd && range.end >= wStart;
    });
    if (overlaps) {
      toast.error('Selected dates overlap an existing availability window.');
      return;
    }
    createMutation.mutate(range);
  }

  function selectWindow(w: AvailabilityWindowDto) {
    navigate({
      to: '/marina/$marinaId/listings',
      params: { marinaId },
      search: { slipId, slipName, windowId: w.id },
      replace: false,
    });
  }

  function clearWindow() {
    navigate({
      to: '/marina/$marinaId/listings',
      params: { marinaId },
      search: { slipId, slipName, windowId: undefined },
      replace: true,
    });
  }

  function prevMonth() {
    if (month === 0) { setYear(y => y - 1); setMonth(11); }
    else setMonth(m => m - 1);
  }
  function nextMonth() {
    if (month === 11) { setYear(y => y + 1); setMonth(0); }
    else setMonth(m => m + 1);
  }

  return (
    <>
      <PageHeader
        title={`Listing · ${slipName}`}
        subtitle="Drag a date range on the calendar to set price & policy. Multiple windows allowed; no overlap."
        actions={
          <div className="flex items-center gap-2">
            <Button size="sm" variant="outline" onClick={onBack}>
              <ChevronLeft className="h-4 w-4 mr-1" />All slips
            </Button>
            <Button size="sm" variant="outline">
              <Eye className="h-4 w-4 mr-1" />Preview as boater
            </Button>
            {selectedWindow && (
              <Button size="sm" onClick={() => editorRef.current?.save()}>
                Save changes
              </Button>
            )}
          </div>
        }
      />
      <PageBody>
        <div className="flex gap-5 items-start">
          {/* Calendar */}
          <div className="flex-1 min-w-0">
            <ListingCalendar
              year={year}
              month={month}
              windows={windows}
              dragRange={dragRange}
              selectedWindowId={selectedWindowId}
              onPrevMonth={prevMonth}
              onNextMonth={nextMonth}
              onDragStart={handleDragStart}
              onDragMove={handleDragMove}
              onDragEnd={handleDragEnd}
              onWindowClick={selectWindow}
            />
            {windows.length > 0 && (
              <p className="text-xs text-muted-foreground mt-2">
                {windows.length} availability window{windows.length !== 1 ? 's' : ''} shown. Drag across new dates to add another — overlap is prevented automatically.
              </p>
            )}
            {windows.length === 0 && (
              <p className="text-xs text-muted-foreground mt-3">
                No availability windows yet. Drag across a date range to create one.
              </p>
            )}
          </div>

          {/* Editor panel */}
          <div className="w-72 shrink-0">
            <WindowEditorPanel
              key={selectedWindowId ?? 'empty'}
              ref={editorRef}
              marinaId={marinaId}
              window_={selectedWindow}
              windowIndex={selectedWindowIndex}
              onSaved={() => {}}
              onDeleted={clearWindow}
            />
          </div>
        </div>
      </PageBody>
    </>
  );
}

// ─── Slip picker table ────────────────────────────────────────────────────────

function listingStatus(slipId: string, windows: AvailabilityWindowDto[]): {
  label: 'Listed' | 'Paused' | 'Not listed';
  openCount: number;
} {
  const sw = windows.filter((w) => w.slipId === slipId);
  const openCount = sw.filter((w) => w.status === 'Open').length;
  const pausedCount = sw.filter((w) => w.status === 'Paused').length;
  const label = openCount > 0 ? 'Listed' : pausedCount > 0 ? 'Paused' : 'Not listed';
  return { label, openCount };
}

function ListingStatusBadge({ status }: { status: 'Listed' | 'Paused' | 'Not listed' }) {
  const styles = {
    Listed: 'bg-green-100 text-green-700 dark:bg-green-950/50 dark:text-green-300',
    Paused: 'bg-amber-100 text-amber-700 dark:bg-amber-950/50 dark:text-amber-300',
    'Not listed': 'bg-muted text-muted-foreground',
  } as const;
  return (
    <span className={cn('inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium', styles[status])}>
      {status}
    </span>
  );
}

function SlipPickerTable({
  marinaId,
  onSelect,
}: {
  marinaId: string;
  onSelect: (slip: SlipDto) => void;
}) {
  const { data: slips = [], isLoading } = useQuery<SlipDto[]>({
    queryKey: ['marina-slips', marinaId],
    queryFn: () => getSlips(marinaId),
    staleTime: 60_000,
  });

  const { data: docks = [] } = useQuery<DockDto[]>({
    queryKey: ['marina-docks', marinaId],
    queryFn: () => getDocks(marinaId),
    staleTime: 60_000,
  });

  const { data: allWindows = [] } = useQuery<AvailabilityWindowDto[]>({
    queryKey: ['marina-windows', marinaId],
    queryFn: () => getAvailabilityWindows(marinaId),
    staleTime: 60_000,
  });

  const dockName = (dockId: string | null | undefined) =>
    dockId ? (docks.find((d) => d.id === dockId)?.name ?? dockId) : '—';

  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2, 3].map((i) => <div key={i} className="h-12 bg-muted animate-pulse rounded-lg" />)}
      </div>
    );
  }

  if (slips.length === 0) {
    return (
      <p className="text-sm text-muted-foreground py-8 text-center">
        No slips found. Add slips in the Slips &amp; Docks section first.
      </p>
    );
  }

  return (
    <div className="rounded-xl border border-border overflow-hidden">
      <table className="w-full text-left">
        <thead>
          <tr className="bg-muted/50 border-b border-border">
            <th className="px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide">Slip</th>
            <th className="px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide">Dock</th>
            <th className="px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide">Dimensions</th>
            <th className="px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide">Status</th>
            <th className="px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide">Open windows</th>
            <th className="px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide text-right">Action</th>
          </tr>
        </thead>
        <tbody>
          {slips.map((slip) => {
            const { label, openCount } = listingStatus(slip.id, allWindows);
            return (
              <tr key={slip.id} className="border-b border-border/50 hover:bg-muted/20 transition-colors">
                <td className="px-4 py-3 text-sm font-medium">{slip.name}</td>
                <td className="px-4 py-3 text-sm text-muted-foreground">{dockName(slip.dockId)}</td>
                <td className="px-4 py-3 text-sm text-muted-foreground">{slip.maxLength}′ × {slip.maxBeam}′</td>
                <td className="px-4 py-3"><ListingStatusBadge status={label} /></td>
                <td className="px-4 py-3 text-sm text-muted-foreground">{openCount > 0 ? openCount : '—'}</td>
                <td className="px-4 py-3 text-right">
                  <Button size="sm" variant="outline" onClick={() => onSelect(slip)}>
                    <Eye className="h-4 w-4 mr-1.5" />Manage
                  </Button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export function ListingsPage() {
  const { marinaId } = useParams({ strict: false }) as { marinaId: string };
  const navigate = useNavigate();
  const search = useSearch({ strict: false }) as { slipId?: string; slipName?: string };

  function selectSlip(slip: SlipDto) {
    navigate({
      to: '/marina/$marinaId/listings',
      params: { marinaId },
      search: { slipId: slip.id, slipName: slip.name, windowId: undefined },
    });
  }

  function clearSlip() {
    navigate({
      to: '/marina/$marinaId/listings',
      params: { marinaId },
      search: { slipId: undefined, slipName: undefined, windowId: undefined },
    });
  }

  if (search.slipId) {
    return (
      <SlipCalendarView
        marinaId={marinaId}
        slipId={search.slipId}
        slipName={search.slipName ?? 'Slip'}
        onBack={clearSlip}
      />
    );
  }

  return (
    <>
      <PageHeader title="Listings" subtitle="Manage transient availability windows and nightly pricing for each slip." />
      <PageBody>
        <SlipPickerTable marinaId={marinaId} onSelect={selectSlip} />
      </PageBody>
    </>
  );
}
