import { useState, useEffect } from 'react';
import { useParams, useNavigate, useRouter, useSearch } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Link2Off, AlertTriangle } from 'lucide-react';
import {
  getSlipAssignments, createSlipAssignment, updateSlipAssignment, endSlipAssignment,
  getSlips, getBillingAccounts, getVesselRecords,
  type SlipAssignmentDto, type AssignmentType, type SlipDto,
  type BillingAccountDto, type VesselRecordDto, type CreateSlipAssignmentData,
} from '@/api/api';
import { PageHeader, PageBody } from '@/marina-workspace/PageHeader';
import { FilterChip } from '@/components/ui/filter-chip';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { Pagination } from '@/components/ui/pagination';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';
import { cn } from '@/lib/utils';

// ─── Types / constants ────────────────────────────────────────────────────────

type AssignType = 'all' | AssignmentType;

const TYPE_CHIPS: { key: AssignType; label: string }[] = [
  { key: 'all',       label: 'All' },
  { key: 'Annual',    label: 'Annual' },
  { key: 'Seasonal',  label: 'Seasonal' },
  { key: 'Monthly',   label: 'Monthly' },
  { key: 'Transient', label: 'Transient' },
];

const TYPE_BADGE_VARIANT: Record<AssignmentType, string> = {
  Annual:    'primary',
  Seasonal:  'accent',
  Monthly:   'warning',
  Transient: 'neutral',
};

const PAGE_SIZE = 8;

// ─── Helpers ─────────────────────────────────────────────────────────────────

function fmtDate(d: string) {
  return new Date(d + 'T00:00:00').toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function daysUntil(d: string) {
  return Math.ceil((new Date(d + 'T00:00:00').getTime() - Date.now()) / 86400000);
}

// ─── Create/Edit dialog ───────────────────────────────────────────────────────

function AssignmentDialog({
  marinaId,
  editing,
  onClose,
  onSaved,
}: {
  marinaId: string;
  editing: SlipAssignmentDto | null;
  onClose: () => void;
  onSaved: (a: SlipAssignmentDto) => void;
}) {
  const { data: slips = [] } = useQuery<SlipDto[]>({
    queryKey: ['marina-slips', marinaId],
    queryFn: () => getSlips(marinaId),
    staleTime: 60_000,
  });
  const { data: accounts = [] } = useQuery<BillingAccountDto[]>({
    queryKey: ['marina-accounts', marinaId],
    queryFn: () => getBillingAccounts(marinaId),
    staleTime: 60_000,
  });

  const [form, setForm] = useState({
    slipId: editing?.slipId ?? '',
    billingAccountId: editing?.billingAccountId ?? '',
    vesselId: editing?.vesselId ?? '',
    assignmentType: (editing?.assignmentType ?? 'Annual') as AssignmentType,
    startDate: editing?.startDate ?? '',
    endDate: editing?.endDate ?? '',
    baseRate: editing ? String(editing.baseRate) : '',
    allowOwnerSubletWhenAway: editing?.allowOwnerSubletWhenAway ?? false,
    allowHolderSublet: editing?.allowHolderSublet ?? false,
  });

  const [vessels, setVessels] = useState<VesselRecordDto[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!form.billingAccountId) { setVessels([]); return; }
    getVesselRecords(marinaId, form.billingAccountId).then(setVessels);
  }, [marinaId, form.billingAccountId]);

  const createMutation = useMutation({
    mutationFn: (data: CreateSlipAssignmentData) => createSlipAssignment(marinaId, data),
    onSuccess: (saved) => { onSaved(saved); onClose(); },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      setError(msg ?? 'Could not create assignment.');
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: Partial<CreateSlipAssignmentData>) =>
      updateSlipAssignment(marinaId, editing!.id, data),
    onSuccess: (saved) => { onSaved(saved); onClose(); },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      setError(msg ?? 'Could not update assignment.');
    },
  });

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.slipId || !form.billingAccountId || !form.vesselId || !form.startDate || !form.baseRate) {
      setError('All required fields must be filled.');
      return;
    }
    setError(null);
    const payload: CreateSlipAssignmentData = {
      slipId: form.slipId,
      billingAccountId: form.billingAccountId,
      vesselId: form.vesselId,
      assignmentType: form.assignmentType,
      startDate: form.startDate,
      endDate: form.endDate || null,
      baseRate: Number(form.baseRate),
      allowOwnerSubletWhenAway: form.allowOwnerSubletWhenAway,
      allowHolderSublet: form.allowHolderSublet,
      ownerSubletShareToHolder: 0,
      holderSubletShareToOwner: 0,
    };
    if (editing) {
      updateMutation.mutate({ endDate: payload.endDate, baseRate: payload.baseRate });
    } else {
      createMutation.mutate(payload);
    }
  }

  const isPending = createMutation.isPending || updateMutation.isPending;
  const sel = 'w-full rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring';
  const inp = 'w-full rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:ring-2 focus:ring-ring';

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>{editing ? 'Edit assignment' : 'New slip assignment'}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          {!editing && (
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs text-muted-foreground mb-1">Slip *</label>
                <select
                  value={form.slipId}
                  onChange={(e) => setForm({ ...form, slipId: e.target.value })}
                  className={sel}
                  required
                >
                  <option value="">— select slip —</option>
                  {slips.map((s) => (
                    <option key={s.id} value={s.id}>{s.name} ({s.maxLength}′)</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs text-muted-foreground mb-1">Type *</label>
                <select
                  value={form.assignmentType}
                  onChange={(e) => setForm({ ...form, assignmentType: e.target.value as AssignmentType })}
                  className={sel}
                >
                  {(['Annual', 'Seasonal', 'Monthly', 'Transient'] as AssignmentType[]).map((t) => (
                    <option key={t} value={t}>{t}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs text-muted-foreground mb-1">Billing account *</label>
                <select
                  value={form.billingAccountId}
                  onChange={(e) => setForm({ ...form, billingAccountId: e.target.value, vesselId: '' })}
                  className={sel}
                  required
                >
                  <option value="">— select account —</option>
                  {accounts.map((a) => (
                    <option key={a.id} value={a.id}>{a.displayName}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs text-muted-foreground mb-1">Vessel *</label>
                <select
                  value={form.vesselId}
                  onChange={(e) => setForm({ ...form, vesselId: e.target.value })}
                  className={sel}
                  disabled={!form.billingAccountId}
                  required
                >
                  <option value="">— select vessel —</option>
                  {vessels.map((v) => (
                    <option key={v.vesselId} value={v.vesselId}>
                      {v.vesselName}{v.vesselIsGhost ? ' (unregistered)' : ''}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          )}

          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className="block text-xs text-muted-foreground mb-1">Start date *</label>
              <input
                type="date"
                value={form.startDate}
                onChange={(e) => setForm({ ...form, startDate: e.target.value })}
                className={inp}
                disabled={!!editing}
                required
              />
            </div>
            <div>
              <label className="block text-xs text-muted-foreground mb-1">End date</label>
              <input
                type="date"
                value={form.endDate}
                onChange={(e) => setForm({ ...form, endDate: e.target.value })}
                className={inp}
              />
            </div>
            <div>
              <label className="block text-xs text-muted-foreground mb-1">Base rate ($/period) *</label>
              <input
                type="number"
                step="0.01"
                min="0"
                value={form.baseRate}
                onChange={(e) => setForm({ ...form, baseRate: e.target.value })}
                className={inp}
                required
              />
            </div>
          </div>

          {!editing && (
            <div className="flex gap-4 text-sm">
              <label className="flex items-center gap-2 text-foreground/80 cursor-pointer">
                <input
                  type="checkbox"
                  checked={form.allowHolderSublet}
                  onChange={(e) => setForm({ ...form, allowHolderSublet: e.target.checked })}
                />
                Holder may sublet
              </label>
              <label className="flex items-center gap-2 text-foreground/80 cursor-pointer">
                <input
                  type="checkbox"
                  checked={form.allowOwnerSubletWhenAway}
                  onChange={(e) => setForm({ ...form, allowOwnerSubletWhenAway: e.target.checked })}
                />
                Marina may sublet when away
              </label>
            </div>
          )}

          {error && <p className="text-xs text-red-600">{error}</p>}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>Cancel</Button>
            <Button type="submit" disabled={isPending}>
              {isPending ? 'Saving…' : (editing ? 'Save changes' : 'Assign slip')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─── Assignment row ───────────────────────────────────────────────────────────

function AssignmentRow({
  assignment,
  onEdit,
  onEnd,
}: {
  assignment: SlipAssignmentDto;
  onEdit: () => void;
  onEnd: () => void;
}) {
  const endingSoon = assignment.endDate ? daysUntil(assignment.endDate) <= 30 : false;

  return (
    <tr className="border-b border-border/50 hover:bg-muted/20 transition-colors">
      <td className="px-4 py-3 text-sm font-medium text-foreground">{assignment.slipName}</td>
      <td className="px-4 py-3 text-sm text-muted-foreground">{assignment.billingAccountDisplayName}</td>
      <td className="px-4 py-3 text-sm text-muted-foreground">{assignment.vesselName}</td>
      <td className="px-4 py-3">
        <Badge variant={TYPE_BADGE_VARIANT[assignment.assignmentType] as 'primary' | 'accent' | 'warning' | 'neutral'}>
          {assignment.assignmentType}
        </Badge>
      </td>
      <td className="px-4 py-3 text-sm text-muted-foreground whitespace-nowrap">
        {fmtDate(assignment.startDate)}
      </td>
      <td className="px-4 py-3 text-sm whitespace-nowrap">
        {assignment.endDate ? (
          <span className={cn(endingSoon ? 'text-amber-600 font-medium' : 'text-muted-foreground')}>
            {fmtDate(assignment.endDate)}
            {endingSoon && <AlertTriangle className="inline h-3.5 w-3.5 ml-1" />}
          </span>
        ) : (
          <span className="text-muted-foreground/60">Open-ended</span>
        )}
      </td>
      <td className="px-4 py-3 text-sm text-muted-foreground text-right">
        ${assignment.baseRate.toLocaleString()}<span className="text-xs">/period</span>
      </td>
      <td className="px-4 py-3">
        <div className="flex justify-end gap-3">
          <button onClick={onEdit} className="text-xs text-muted-foreground hover:text-foreground">Edit</button>
          <button onClick={onEnd} className="text-xs text-muted-foreground hover:text-red-500">
            <Link2Off className="h-3.5 w-3.5" />
          </button>
        </div>
      </td>
    </tr>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export function AssignmentsPage() {
  const { marinaId } = useParams({ strict: false }) as { marinaId: string };
  const navigate = useNavigate();
  const router = useRouter();
  const queryClient = useQueryClient();

  const search = useSearch({ strict: false }) as { type?: string; endingSoon?: string; page?: string };
  const typeFilter = (search.type as AssignType) ?? 'all';
  const endingSoon = search.endingSoon === 'true';
  const page = Math.max(1, parseInt(search.page ?? '1', 10) || 1);

  const [dialogAssignment, setDialogAssignment] = useState<SlipAssignmentDto | 'new' | null>(null);

  function updateSearch(patch: Record<string, string | undefined>) {
    const current = { ...search } as Record<string, string | undefined>;
    for (const [k, v] of Object.entries(patch)) {
      if (v == null) delete current[k];
      else current[k] = v;
    }
    navigate({ to: router.state.location.pathname, search: current as Record<string, string>, replace: false });
  }

  const { data: assignments = [], isLoading } = useQuery<SlipAssignmentDto[]>({
    queryKey: ['marina-assignments', marinaId],
    queryFn: () => getSlipAssignments(marinaId, { activeOnly: true }),
    staleTime: 30_000,
  });

  const endMutation = useMutation({
    mutationFn: (id: string) => endSlipAssignment(marinaId, id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['marina-assignments', marinaId] });
      queryClient.invalidateQueries({ queryKey: ['marina-counters', marinaId] });
    },
  });

  function handleEnd(id: string) {
    if (!window.confirm('End this assignment today?')) return;
    endMutation.mutate(id);
  }

  function handleSaved(saved: SlipAssignmentDto) {
    queryClient.invalidateQueries({ queryKey: ['marina-assignments', marinaId] });
    queryClient.invalidateQueries({ queryKey: ['marina-counters', marinaId] });
    void saved;
  }

  // Filter
  const filtered = assignments.filter((a) => {
    if (typeFilter !== 'all' && a.assignmentType !== typeFilter) return false;
    if (endingSoon && (!a.endDate || daysUntil(a.endDate) > 30)) return false;
    return true;
  });

  const pageItems = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  const typeCounts = TYPE_CHIPS.reduce((acc, { key }) => {
    if (key === 'all') return acc;
    acc[key] = assignments.filter((a) => a.assignmentType === key).length;
    return acc;
  }, {} as Record<string, number>);

  return (
    <>
      <PageHeader
        title="Assignments"
        actions={
          <Button size="sm" onClick={() => setDialogAssignment('new')}>
            <Plus className="h-4 w-4 mr-1.5" />
            New assignment
          </Button>
        }
      />
      <PageBody>
        {/* Filter chips */}
        <div className="flex flex-wrap items-center gap-2 mb-4">
          {TYPE_CHIPS.map(({ key, label }) => (
            <FilterChip
              key={key}
              active={typeFilter === key}
              count={key !== 'all' && typeCounts[key] > 0 ? typeCounts[key] : undefined}
              onClick={() => updateSearch({ type: key === 'all' ? undefined : key, page: undefined })}
            >
              {label}
            </FilterChip>
          ))}
          <Separator orientation="vertical" className="h-6" />
          <FilterChip
            active={endingSoon}
            onClick={() => updateSearch({ endingSoon: endingSoon ? undefined : 'true', page: undefined })}
          >
            Ending soon
          </FilterChip>
        </div>

        {/* Table */}
        {isLoading ? (
          <div className="space-y-2">
            {[1, 2, 3].map((i) => <div key={i} className="h-12 bg-muted animate-pulse rounded" />)}
          </div>
        ) : filtered.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-12">
            <p className="text-sm text-muted-foreground">No assignments found</p>
          </div>
        ) : (
          <div className="rounded-xl border border-border overflow-hidden">
            <table className="w-full text-left">
              <thead>
                <tr className="bg-muted/50 border-b border-border">
                  <th className="px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide">Slip</th>
                  <th className="px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide">Account</th>
                  <th className="px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide">Vessel</th>
                  <th className="px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide">Type</th>
                  <th className="px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide">Start</th>
                  <th className="px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide">End</th>
                  <th className="px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide text-right">Rate</th>
                  <th className="px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {pageItems.map((a) => (
                  <AssignmentRow
                    key={a.id}
                    assignment={a}
                    onEdit={() => setDialogAssignment(a)}
                    onEnd={() => handleEnd(a.id)}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}

        {filtered.length > PAGE_SIZE && (
          <div className="mt-4">
            <Pagination
              page={page}
              onChange={(p) => updateSearch({ page: p === 1 ? undefined : String(p) })}
              total={filtered.length}
              pageSize={PAGE_SIZE}
            />
          </div>
        )}
      </PageBody>

      {/* Create / edit dialog */}
      {dialogAssignment !== null && (
        <AssignmentDialog
          marinaId={marinaId}
          editing={dialogAssignment === 'new' ? null : dialogAssignment}
          onClose={() => setDialogAssignment(null)}
          onSaved={handleSaved}
        />
      )}
    </>
  );
}
