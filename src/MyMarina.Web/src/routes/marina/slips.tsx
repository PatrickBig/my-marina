import { useState } from 'react';
import { useParams, useNavigate, useRouter, useSearch } from '@tanstack/react-router';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Plus, Pencil, Trash2 } from 'lucide-react';
import {
  getDocks, createDock, updateDock, deleteDock,
  getSlips, createSlip, updateSlip, deleteSlip,
  type DockDto, type SlipDto, type SlipType,
} from '@/api/api';
import { PageHeader, PageBody } from '@/marina-workspace/PageHeader';
import { Button } from '@/components/ui/button';
import { FilterChip } from '@/components/ui/filter-chip';
import { Pagination } from '@/components/ui/pagination';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
  AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { cn } from '@/lib/utils';

// ─── Constants ────────────────────────────────────────────────────────────────

const PAGE_SIZE = 10;

const SLIP_TYPE_LABELS: Record<SlipType, string> = {
  Floating: 'Floating', Fixed: 'Fixed', Mooring: 'Mooring', DryStorage: 'Dry Storage', Anchorage: 'Anchorage',
};

const SLIP_STATUS_LABELS: Record<string, string> = {
  Active: 'Active', UnderMaintenance: 'Maintenance', Inactive: 'Inactive',
};

type SlipStatus = 'all' | 'Active' | 'UnderMaintenance' | 'Inactive';

const STATUS_CHIPS: { key: SlipStatus; label: string }[] = [
  { key: 'all', label: 'All' },
  { key: 'Active', label: 'Active' },
  { key: 'UnderMaintenance', label: 'Maintenance' },
  { key: 'Inactive', label: 'Inactive' },
];

// ─── Status badge ─────────────────────────────────────────────────────────────

function SlipStatusBadge({ status }: { status: string }) {
  const cfg = status === 'Active'
    ? { dot: 'bg-green-500', bg: 'bg-green-50 text-green-700 dark:bg-green-950 dark:text-green-300', label: 'Active' }
    : status === 'UnderMaintenance'
    ? { dot: 'bg-amber-500', bg: 'bg-amber-50 text-amber-700 dark:bg-amber-950 dark:text-amber-300', label: 'Maintenance' }
    : { dot: 'bg-gray-400', bg: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400', label: SLIP_STATUS_LABELS[status] ?? status };

  return (
    <span className={cn('inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium', cfg.bg)}>
      <span className={cn('h-1.5 w-1.5 rounded-full', cfg.dot)} />
      {cfg.label}
    </span>
  );
}

// ─── Dock form ────────────────────────────────────────────────────────────────

const dockSchema = z.object({
  name: z.string().min(1, 'Required'),
  description: z.string().optional(),
  sortOrder: z.number().min(0),
});
type DockFormValues = z.infer<typeof dockSchema>;

function DockFormFields({
  defaultValues,
  onSubmit,
  onCancel,
  submitLabel,
}: {
  defaultValues?: Partial<DockFormValues>;
  onSubmit: (v: DockFormValues) => Promise<void>;
  onCancel: () => void;
  submitLabel: string;
}) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<DockFormValues>({
    resolver: zodResolver(dockSchema),
    defaultValues: { sortOrder: 0, ...defaultValues },
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <label className="text-sm font-medium">Dock name</label>
          <input {...register('name')} placeholder="Dock A" autoFocus
            className="w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm" />
          {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
        </div>
        <div className="space-y-1">
          <label className="text-sm font-medium">Sort order</label>
          <input {...register('sortOrder', { valueAsNumber: true })} type="number"
            className="w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm" />
        </div>
      </div>
      <div className="space-y-1">
        <label className="text-sm font-medium">Description (optional)</label>
        <input {...register('description')} placeholder="e.g. Long-term · 30A/50A, North transient dock…"
          className="w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm" />
      </div>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={onCancel}>Cancel</Button>
        <Button type="submit" disabled={isSubmitting}>{isSubmitting ? '…' : submitLabel}</Button>
      </DialogFooter>
    </form>
  );
}

// ─── Slip form ────────────────────────────────────────────────────────────────

const slipSchema = z.object({
  name: z.string().min(1, 'Required'),
  dockId: z.string().optional(),
  slipType: z.enum(['Floating', 'Fixed', 'Mooring', 'DryStorage', 'Anchorage'] as const),
  status: z.enum(['Active', 'UnderMaintenance', 'Inactive'] as const).optional(),
  maxLength: z.coerce.number().positive('Required'),
  maxBeam: z.coerce.number().positive('Required'),
  maxDraft: z.coerce.number().positive('Required'),
  hasElectric: z.boolean(),
  electric: z.coerce.number().optional(),
  hasWater: z.boolean(),
  notes: z.string().optional(),
});
type SlipFormData = z.infer<typeof slipSchema>;

function SlipFormFields({
  docks,
  defaultValues,
  onSubmit,
  onCancel,
  submitLabel,
  showStatus = false,
}: {
  docks: DockDto[];
  defaultValues?: Partial<SlipFormData>;
  onSubmit: (data: SlipFormData) => Promise<void>;
  onCancel: () => void;
  submitLabel: string;
  showStatus?: boolean;
}) {
  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<SlipFormData>({
    resolver: zodResolver(slipSchema) as never,
    defaultValues: { slipType: 'Floating', hasElectric: false, hasWater: true, status: 'Active', ...defaultValues },
  });
  const hasElectric = watch('hasElectric');
  const sel = 'w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm';
  const inp = 'w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm';

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <label className="text-sm font-medium">Slip name / number</label>
          <input {...register('name')} placeholder="A-1" autoFocus className={inp} />
          {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
        </div>
        <div className="space-y-1">
          <label className="text-sm font-medium">Dock (optional)</label>
          <select {...register('dockId')} className={sel}>
            <option value="">— Free-standing —</option>
            {docks.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
          </select>
        </div>
        <div className="space-y-1">
          <label className="text-sm font-medium">Slip type</label>
          <select {...register('slipType')} className={sel}>
            {(Object.keys(SLIP_TYPE_LABELS) as SlipType[]).map((t) => (
              <option key={t} value={t}>{SLIP_TYPE_LABELS[t]}</option>
            ))}
          </select>
        </div>
        {showStatus && (
          <div className="space-y-1">
            <label className="text-sm font-medium">Status</label>
            <select {...register('status')} className={sel}>
              {Object.entries(SLIP_STATUS_LABELS).map(([v, label]) => (
                <option key={v} value={v}>{label}</option>
              ))}
            </select>
          </div>
        )}
        <div className="space-y-1">
          <label className="text-sm font-medium">Max length (ft)</label>
          <input {...register('maxLength')} type="number" step="0.1" className={inp} />
          {errors.maxLength && <p className="text-xs text-destructive">{errors.maxLength.message}</p>}
        </div>
        <div className="space-y-1">
          <label className="text-sm font-medium">Max beam (ft)</label>
          <input {...register('maxBeam')} type="number" step="0.1" className={inp} />
          {errors.maxBeam && <p className="text-xs text-destructive">{errors.maxBeam.message}</p>}
        </div>
        <div className="space-y-1">
          <label className="text-sm font-medium">Max draft (ft)</label>
          <input {...register('maxDraft')} type="number" step="0.1" className={inp} />
          {errors.maxDraft && <p className="text-xs text-destructive">{errors.maxDraft.message}</p>}
        </div>
      </div>
      <div className="flex flex-wrap gap-4 items-center text-sm">
        <label className="flex items-center gap-2">
          <input {...register('hasWater')} type="checkbox" /> Water hookup
        </label>
        <label className="flex items-center gap-2">
          <input {...register('hasElectric')} type="checkbox" /> Electric hookup
        </label>
        {hasElectric && (
          <div className="space-y-1">
            <label className="text-sm font-medium">Amperage</label>
            <select {...register('electric')} className={sel}>
              <option value="">— Select —</option>
              <option value="30">30A</option>
              <option value="50">50A</option>
              <option value="100">100A</option>
            </select>
          </div>
        )}
      </div>
      <div className="space-y-1">
        <label className="text-sm font-medium">Notes</label>
        <textarea {...register('notes')} rows={2}
          className="w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm resize-none" />
      </div>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={onCancel}>Cancel</Button>
        <Button type="submit" disabled={isSubmitting}>{isSubmitting ? '…' : submitLabel}</Button>
      </DialogFooter>
    </form>
  );
}

// ─── Dock rail ────────────────────────────────────────────────────────────────

function DockRail({
  marinaId,
  docks,
  selectedDockId,
  allSlips,
  onSelectDock,
  onDocksChanged,
}: {
  marinaId: string;
  docks: DockDto[];
  selectedDockId: string | null;
  allSlips: SlipDto[];
  onSelectDock: (id: string | null) => void;
  onDocksChanged: () => void;
}) {
  const [createOpen, setCreateOpen] = useState(false);
  const [editDock, setEditDock] = useState<DockDto | null>(null);
  const [deleteDockTarget, setDeleteDockTarget] = useState<DockDto | null>(null);

  function slipsForDock(dockId: string | null) {
    return dockId === null ? allSlips : allSlips.filter((s) => s.dockId === dockId);
  }

  async function handleCreate(v: DockFormValues) {
    await createDock(marinaId, {
      name: v.name,
      description: v.description || null,
      sortOrder: v.sortOrder ?? docks.length,
    });
    setCreateOpen(false);
    onDocksChanged();
  }

  async function handleEdit(v: DockFormValues) {
    if (!editDock) return;
    await updateDock(marinaId, editDock.id, {
      name: v.name,
      description: v.description || null,
      sortOrder: v.sortOrder,
    });
    setEditDock(null);
    onDocksChanged();
  }

  async function handleDelete() {
    if (!deleteDockTarget) return;
    await deleteDock(marinaId, deleteDockTarget.id);
    setDeleteDockTarget(null);
    if (selectedDockId === deleteDockTarget.id) onSelectDock(null);
    onDocksChanged();
  }

  return (
    <aside className="w-60 shrink-0 space-y-1.5">
      <div className="flex items-center justify-between mb-2">
        <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Docks</span>
        <button onClick={() => setCreateOpen(true)}
          className="text-xs text-muted-foreground hover:text-foreground" aria-label="Add dock">
          <Plus className="h-3.5 w-3.5" />
        </button>
      </div>

      {/* All slips item */}
      <DockCard
        label="All slips"
        description={null}
        slips={allSlips}
        selected={selectedDockId === null}
        onSelect={() => onSelectDock(null)}
        onEdit={null}
        onDelete={null}
      />

      {/* Per-dock cards */}
      {docks.map((dock) => {
        const dSlips = slipsForDock(dock.id);
        return (
          <DockCard
            key={dock.id}
            label={dock.name}
            description={dock.description ?? null}
            slips={dSlips}
            selected={selectedDockId === dock.id}
            onSelect={() => onSelectDock(dock.id)}
            onEdit={() => setEditDock(dock)}
            onDelete={() => setDeleteDockTarget(dock)}
          />
        );
      })}

      {/* Create dock dialog */}
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent>
          <DialogHeader><DialogTitle>New dock</DialogTitle></DialogHeader>
          <DockFormFields
            defaultValues={{ sortOrder: docks.length }}
            onSubmit={handleCreate}
            onCancel={() => setCreateOpen(false)}
            submitLabel="Add dock"
          />
        </DialogContent>
      </Dialog>

      {/* Edit dock dialog */}
      <Dialog open={!!editDock} onOpenChange={(o) => { if (!o) setEditDock(null); }}>
        <DialogContent>
          <DialogHeader><DialogTitle>Edit dock</DialogTitle></DialogHeader>
          {editDock && (
            <DockFormFields
              defaultValues={{ name: editDock.name, description: editDock.description ?? '', sortOrder: editDock.sortOrder }}
              onSubmit={handleEdit}
              onCancel={() => setEditDock(null)}
              submitLabel="Save"
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Delete dock confirmation */}
      <AlertDialog open={!!deleteDockTarget} onOpenChange={(o) => { if (!o) setDeleteDockTarget(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete {deleteDockTarget?.name}?</AlertDialogTitle>
            <AlertDialogDescription>
              Slips assigned to this dock will become free-standing. This cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </aside>
  );
}

function DockCard({
  label,
  description,
  slips,
  selected,
  onSelect,
  onEdit,
  onDelete,
}: {
  label: string;
  description: string | null;
  slips: SlipDto[];
  selected: boolean;
  onSelect: () => void;
  onEdit: (() => void) | null;
  onDelete: (() => void) | null;
}) {
  const total = slips.length;
  const held = slips.filter((s) => s.currentHolder).length;
  const fillPct = total > 0 ? Math.round((held / total) * 100) : 0;

  return (
    <div className="group relative">
      <button
        onClick={onSelect}
        className={cn(
          'w-full rounded-xl border px-3 py-2.5 text-left transition-colors',
          selected
            ? 'border-primary/30 bg-primary/8 ring-1 ring-primary/20'
            : 'border-border bg-card hover:border-border/80 hover:bg-muted/40',
        )}
      >
        {/* Top row: name + capacity */}
        <div className="flex items-center justify-between gap-2 mb-0.5">
          <span className={cn('text-sm font-semibold truncate', selected ? 'text-primary' : 'text-foreground')}>
            {label}
          </span>
          {total > 0 && (
            <span className="text-xs font-medium text-muted-foreground shrink-0">
              {held}/{total}
            </span>
          )}
        </div>

        {/* Description */}
        {description && (
          <p className="text-xs text-muted-foreground truncate mb-1.5">{description}</p>
        )}

        {/* Progress bar */}
        {total > 0 && (
          <div className="h-1 rounded-full bg-muted overflow-hidden">
            <div
              className={cn('h-full rounded-full transition-all', selected ? 'bg-primary' : 'bg-foreground/30')}
              style={{ width: `${fillPct}%` }}
            />
          </div>
        )}
      </button>

      {/* Hover edit/delete */}
      {(onEdit || onDelete) && (
        <div className="absolute top-2 right-2 hidden group-hover:flex gap-0.5 z-10">
          {onEdit && (
            <button onClick={(e) => { e.stopPropagation(); onEdit(); }}
              className="p-1 rounded bg-background/80 text-muted-foreground hover:text-foreground shadow-sm" aria-label="Edit dock">
              <Pencil className="h-3 w-3" />
            </button>
          )}
          {onDelete && (
            <button onClick={(e) => { e.stopPropagation(); onDelete(); }}
              className="p-1 rounded bg-background/80 text-muted-foreground hover:text-destructive shadow-sm" aria-label="Delete dock">
              <Trash2 className="h-3 w-3" />
            </button>
          )}
        </div>
      )}
    </div>
  );
}

// ─── Slip table ───────────────────────────────────────────────────────────────

function SlipTable({
  marinaId,
  slips,
  docks,
  onSlipsChanged,
}: {
  marinaId: string;
  slips: SlipDto[];
  docks: DockDto[];
  onSlipsChanged: () => void;
}) {
  const navigate = useNavigate();
  const router = useRouter();
  const search = useSearch({ strict: false }) as { status?: string; page?: string };
  const status = (search.status as SlipStatus) || 'all';
  const page = Number(search.page ?? 1);

  const [createOpen, setCreateOpen] = useState(false);
  const [editSlip, setEditSlip] = useState<SlipDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<SlipDto | null>(null);

  function updateSearch(patch: Record<string, string | undefined>) {
    const current = search as Record<string, string | undefined>;
    navigate({ to: router.state.location.pathname, search: { ...current, ...patch } as Record<string, string>, replace: false });
  }

  const filtered = slips.filter((s) => status === 'all' || s.status === status);
  const paginated = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  const chipCounts: Record<SlipStatus, number> = {
    all: slips.length,
    Active: slips.filter((s) => s.status === 'Active').length,
    UnderMaintenance: slips.filter((s) => s.status === 'UnderMaintenance').length,
    Inactive: slips.filter((s) => s.status === 'Inactive').length,
  };

  async function handleCreate(data: SlipFormData) {
    await createSlip(marinaId, {
      name: data.name, dockId: data.dockId || null, slipType: data.slipType,
      maxLength: data.maxLength, maxBeam: data.maxBeam, maxDraft: data.maxDraft,
      hasElectric: data.hasElectric, electric: data.electric || null,
      hasWater: data.hasWater, notes: data.notes || null,
    });
    setCreateOpen(false);
    onSlipsChanged();
  }

  async function handleEdit(data: SlipFormData) {
    if (!editSlip) return;
    await updateSlip(marinaId, editSlip.id, {
      name: data.name, dockId: data.dockId || null, slipType: data.slipType,
      status: data.status,
      maxLength: data.maxLength, maxBeam: data.maxBeam, maxDraft: data.maxDraft,
      hasElectric: data.hasElectric, electric: data.electric || null,
      hasWater: data.hasWater, notes: data.notes || null,
    });
    setEditSlip(null);
    onSlipsChanged();
  }

  async function handleDelete() {
    if (!deleteTarget) return;
    await deleteSlip(marinaId, deleteTarget.id);
    setDeleteTarget(null);
    onSlipsChanged();
  }

  return (
    <div className="flex-1 min-w-0 space-y-4">
      {/* Header row */}
      <div className="flex items-center justify-between">
        <div className="flex gap-2 flex-wrap">
          {STATUS_CHIPS.map(({ key, label }) => {
            const n = chipCounts[key];
            return (
              <FilterChip
                key={key}
                active={status === key}
                count={n > 0 ? n : undefined}
                onClick={() => updateSearch({ status: key === 'all' ? undefined : key, page: undefined })}
              >
                {label}
              </FilterChip>
            );
          })}
        </div>
        <Button size="sm" onClick={() => setCreateOpen(true)}>
          <Plus className="h-4 w-4 mr-1" /> Add slip
        </Button>
      </div>

      {/* Table */}
      {paginated.length === 0 ? (
        <p className="text-sm text-muted-foreground py-10 text-center">No slips found.</p>
      ) : (
        <div className="rounded-xl border border-border overflow-hidden">
          <table className="w-full text-left">
            <thead className="bg-muted/40 text-xs text-muted-foreground uppercase tracking-wide">
              <tr>
                <th className="px-4 py-3">Slip</th>
                <th className="px-4 py-3">Type</th>
                <th className="px-4 py-3">Max L×B×D</th>
                <th className="px-4 py-3">Power</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Assignment</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-border/50">
              {paginated.map((slip) => (
                <tr key={slip.id} className="hover:bg-muted/20 transition-colors">
                  <td className="px-4 py-3 font-medium text-sm">{slip.name}</td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">{SLIP_TYPE_LABELS[slip.slipType as SlipType] ?? slip.slipType}</td>
                  <td className="px-4 py-3 text-sm text-muted-foreground whitespace-nowrap">
                    {slip.maxLength}′ × {slip.maxBeam}′ × {slip.maxDraft}′
                  </td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">
                    {slip.hasElectric && slip.electric ? `${slip.electric}A` : '—'}
                  </td>
                  <td className="px-4 py-3">
                    <SlipStatusBadge status={slip.status} />
                  </td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">
                    {slip.currentHolder
                      ? <span className="text-foreground">{slip.currentHolder}</span>
                      : <span className="text-muted-foreground/60">— vacant</span>}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2 justify-end">
                      <button onClick={() => setEditSlip(slip)}
                        className="p-1 rounded text-muted-foreground hover:text-foreground" aria-label="Edit slip">
                        <Pencil className="h-3.5 w-3.5" />
                      </button>
                      <button onClick={() => setDeleteTarget(slip)}
                        className="p-1 rounded text-muted-foreground hover:text-destructive" aria-label="Delete slip">
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {filtered.length > PAGE_SIZE && (
        <Pagination
          page={page}
          total={filtered.length}
          pageSize={PAGE_SIZE}
          onChange={(p) => updateSearch({ page: String(p) })}
        />
      )}

      {/* Create slip dialog */}
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader><DialogTitle>New slip</DialogTitle></DialogHeader>
          <SlipFormFields
            docks={docks}
            onSubmit={handleCreate}
            onCancel={() => setCreateOpen(false)}
            submitLabel="Add slip"
          />
        </DialogContent>
      </Dialog>

      {/* Edit slip dialog */}
      <Dialog open={!!editSlip} onOpenChange={(o) => { if (!o) setEditSlip(null); }}>
        <DialogContent className="max-w-2xl">
          <DialogHeader><DialogTitle>Edit {editSlip?.name}</DialogTitle></DialogHeader>
          {editSlip && (
            <SlipFormFields
              docks={docks}
              showStatus
              defaultValues={{
                name: editSlip.name,
                dockId: editSlip.dockId ?? '',
                slipType: editSlip.slipType as SlipType,
                status: editSlip.status as 'Active' | 'UnderMaintenance' | 'Inactive',
                maxLength: editSlip.maxLength,
                maxBeam: editSlip.maxBeam,
                maxDraft: editSlip.maxDraft,
                hasElectric: editSlip.hasElectric,
                electric: editSlip.electric ?? undefined,
                hasWater: editSlip.hasWater,
                notes: editSlip.notes ?? '',
              }}
              onSubmit={handleEdit}
              onCancel={() => setEditSlip(null)}
              submitLabel="Save"
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Delete slip confirmation */}
      <AlertDialog open={!!deleteTarget} onOpenChange={(o) => { if (!o) setDeleteTarget(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete slip {deleteTarget?.name}?</AlertDialogTitle>
            <AlertDialogDescription>
              This cannot be undone. Any listings or assignments for this slip will also be affected.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export function SlipsPage() {
  const { marinaId } = useParams({ strict: false }) as { marinaId: string };
  const navigate = useNavigate();
  const router = useRouter();
  const search = useSearch({ strict: false }) as { dock?: string };
  const selectedDockId = search.dock ?? null;

  const qc = useQueryClient();

  const { data: docks = [] } = useQuery<DockDto[]>({
    queryKey: ['marina-docks', marinaId],
    queryFn: () => getDocks(marinaId),
    staleTime: 30_000,
  });

  const { data: allSlips = [] } = useQuery<SlipDto[]>({
    queryKey: ['marina-slips', marinaId],
    queryFn: () => getSlips(marinaId),
    staleTime: 30_000,
  });

  function invalidate() {
    qc.invalidateQueries({ queryKey: ['marina-docks', marinaId] });
    qc.invalidateQueries({ queryKey: ['marina-slips', marinaId] });
  }

  function setDock(id: string | null) {
    const current = search as Record<string, string | undefined>;
    navigate({
      to: router.state.location.pathname,
      search: { ...current, dock: id ?? undefined, page: undefined } as unknown as Record<string, string>,
      replace: false,
    });
  }

  const visibleSlips = selectedDockId === null
    ? allSlips
    : allSlips.filter((s) => s.dockId === selectedDockId);

  const heldCount = allSlips.filter((s) => s.currentHolder).length;
  const subtitle = `${docks.length} dock${docks.length !== 1 ? 's' : ''} · ${allSlips.length} slips · ${heldCount} currently held`;

  return (
    <>
      <PageHeader title="Slips & Docks" subtitle={subtitle} />
      <PageBody>
        <div className="flex gap-6">
          <DockRail
            marinaId={marinaId}
            docks={docks}
            selectedDockId={selectedDockId}
            allSlips={allSlips}
            onSelectDock={setDock}
            onDocksChanged={invalidate}
          />
          <SlipTable
            marinaId={marinaId}
            slips={visibleSlips}
            docks={docks}
            onSlipsChanged={invalidate}
          />
        </div>
      </PageBody>
    </>
  );
}
