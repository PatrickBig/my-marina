import { useState } from 'react';
import { useParams } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { CheckCircle, XCircle } from 'lucide-react';
import {
  getMarinaLeaseInquiries, updateLeaseInquiry, approveLeaseInquiry, declineLeaseInquiry,
  type LeaseInquiryDto, type LeaseInquiryStatus,
} from '@/api/api';
import { PageHeader, PageBody } from '@/marina-workspace/PageHeader';
import { FilterChip } from '@/components/ui/filter-chip';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
  AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { useUrlState } from '@/hooks/useUrlState';

// ─── Types ────────────────────────────────────────────────────────────────────

type StatusFilter = 'all' | LeaseInquiryStatus;

const STATUS_CHIPS: { key: StatusFilter; label: string }[] = [
  { key: 'all',         label: 'All' },
  { key: 'Pending',     label: 'Pending' },
  { key: 'UnderReview', label: 'Under Review' },
  { key: 'Approved',    label: 'Approved' },
  { key: 'Declined',    label: 'Declined' },
  { key: 'Withdrawn',   label: 'Withdrawn' },
];

const STATUS_BADGE: Record<LeaseInquiryStatus, { label: string; variant: 'warning' | 'accent' | 'success' | 'destructive' | 'neutral' }> = {
  Pending:     { label: 'Pending',      variant: 'warning' },
  UnderReview: { label: 'Under Review', variant: 'accent' },
  Approved:    { label: 'Approved',     variant: 'success' },
  Declined:    { label: 'Declined',     variant: 'destructive' },
  Withdrawn:   { label: 'Withdrawn',    variant: 'neutral' },
};

const inp = 'w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-ring';

// ─── Edit-terms dialog ────────────────────────────────────────────────────────

const termsSchema = z.object({
  agreedRateKind:      z.enum(['', 'Flat', 'PerFoot']),
  agreedBaseRate:      z.string(),
  assignmentStartDate: z.string(),
  assignmentEndDate:   z.string(),
  marinaNote:          z.string(),
  markUnderReview:     z.boolean(),
});
type TermsFormData = z.infer<typeof termsSchema>;

function EditTermsDialog({
  inq,
  onClose,
  onSaved,
}: {
  inq: LeaseInquiryDto;
  onClose: () => void;
  onSaved: (updated: LeaseInquiryDto) => void;
}) {
  const { register, handleSubmit, formState: { isSubmitting } } = useForm<TermsFormData>({
    resolver: zodResolver(termsSchema),
    defaultValues: {
      agreedRateKind:      (inq.agreedRateKind ?? '') as TermsFormData['agreedRateKind'],
      agreedBaseRate:      inq.agreedBaseRate != null ? String(inq.agreedBaseRate) : '',
      assignmentStartDate: inq.assignmentStartDate ?? '',
      assignmentEndDate:   inq.assignmentEndDate ?? '',
      marinaNote:          inq.marinaNote ?? '',
      markUnderReview:     inq.status === 'UnderReview',
    },
  });

  async function onSubmit(data: TermsFormData) {
    const updated = await updateLeaseInquiry(inq.marinaId, inq.id, {
      agreedRateKind:      data.agreedRateKind || null,
      agreedBaseRate:      data.agreedBaseRate ? Number(data.agreedBaseRate) : null,
      assignmentStartDate: data.assignmentStartDate || null,
      assignmentEndDate:   data.assignmentEndDate || null,
      marinaNote:          data.marinaNote || null,
      markUnderReview:     data.markUnderReview,
    });
    onSaved(updated);
    onClose();
  }

  return (
    <Dialog open onOpenChange={(o) => { if (!o) onClose(); }}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Set terms — {inq.slipName}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-sm font-medium">Rate kind</label>
              <select {...register('agreedRateKind')} className={inp}>
                <option value="">— Not set —</option>
                <option value="Flat">Flat (fixed amount)</option>
                <option value="PerFoot">Per foot (× slip length)</option>
              </select>
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium">Base rate ($)</label>
              <input {...register('agreedBaseRate')} type="number" step="0.01" min="0"
                placeholder="0.00" className={inp} />
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium">Assignment start</label>
              <input {...register('assignmentStartDate')} type="date" className={inp} />
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium">Assignment end (optional)</label>
              <input {...register('assignmentEndDate')} type="date" className={inp} />
            </div>
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium">Marina note (internal)</label>
            <textarea {...register('marinaNote')} rows={2}
              placeholder="Internal notes, not shared with the boater"
              className={`${inp} resize-none`} />
          </div>
          <label className="flex items-center gap-2 text-sm text-foreground/80 cursor-pointer">
            <input {...register('markUnderReview')} type="checkbox" className="rounded" />
            Mark as under review
          </label>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>Cancel</Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Saving…' : 'Save terms'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─── Inquiry card ─────────────────────────────────────────────────────────────

function InquiryCard({
  inq,
  onEdit,
  onApprove,
  onDecline,
}: {
  inq: LeaseInquiryDto;
  onEdit: (inq: LeaseInquiryDto) => void;
  onApprove: (inq: LeaseInquiryDto) => void;
  onDecline: (inq: LeaseInquiryDto) => void;
}) {
  const { label, variant } = STATUS_BADGE[inq.status];
  const isActionable = inq.status === 'Pending' || inq.status === 'UnderReview';

  return (
    <div className="rounded-xl border border-border bg-card p-5 space-y-3">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2 flex-wrap">
            <p className="text-sm font-semibold text-foreground">{inq.slipName}</p>
            <Badge variant={variant} dot>{label}</Badge>
          </div>
          <p className="text-sm text-muted-foreground mt-0.5">
            {inq.requestingUserName}
            <span className="text-muted-foreground/60"> · {inq.requestingUserEmail}</span>
          </p>
          <p className="text-xs text-muted-foreground mt-1">
            {inq.desiredTerm} lease
            {inq.desiredStartDate && ` · Start ${inq.desiredStartDate}`}
            {inq.vesselName && ` · ${inq.vesselName}`}
          </p>
          {inq.message && (
            <p className="text-xs text-muted-foreground/70 mt-1 italic">"{inq.message}"</p>
          )}
        </div>
        <p className="text-xs text-muted-foreground/50 shrink-0">
          {new Date(inq.createdAt).toLocaleDateString()}
        </p>
      </div>

      {/* Agreed terms summary */}
      {inq.agreedBaseRate != null && (
        <div className="text-xs text-primary bg-primary/5 rounded-lg px-3 py-2">
          Agreed terms: {inq.agreedRateKind === 'PerFoot'
            ? `$${inq.agreedBaseRate}/ft`
            : `$${inq.agreedBaseRate}`} / {inq.desiredTerm}
          {inq.assignmentStartDate && ` · From ${inq.assignmentStartDate}`}
          {inq.assignmentEndDate && ` to ${inq.assignmentEndDate}`}
        </div>
      )}

      {/* Marina note */}
      {inq.marinaNote && (
        <p className="text-xs text-muted-foreground bg-muted/30 rounded-lg px-3 py-2">
          Note: {inq.marinaNote}
        </p>
      )}

      {/* Closed state history */}
      {inq.status === 'Approved' && (
        <p className="text-xs text-success">
          Approved by {inq.approvedByUserName ?? 'marina'}
          {inq.approvedAt && ` · ${new Date(inq.approvedAt).toLocaleDateString()}`}
          {inq.slipAssignmentId && ' · Assignment created'}
        </p>
      )}
      {inq.status === 'Declined' && (
        <p className="text-xs text-destructive">
          Declined by {inq.declinedByUserName ?? 'marina'}
          {inq.declinedAt && ` · ${new Date(inq.declinedAt).toLocaleDateString()}`}
        </p>
      )}

      {/* Action buttons */}
      {isActionable && (
        <div className="flex gap-2 flex-wrap pt-1 border-t border-border/50">
          <Button size="sm" variant="outline" onClick={() => onEdit(inq)}>
            Set terms
          </Button>
          <Button size="sm" variant="default" onClick={() => onApprove(inq)}>
            <CheckCircle className="h-3.5 w-3.5 mr-1.5" />
            Approve
          </Button>
          <Button size="sm" variant="destructive" onClick={() => onDecline(inq)}>
            <XCircle className="h-3.5 w-3.5 mr-1.5" />
            Decline
          </Button>
        </div>
      )}
    </div>
  );
}

// ─── Decline dialog ───────────────────────────────────────────────────────────

function DeclineDialog({
  inq,
  onClose,
  onDeclined,
}: {
  inq: LeaseInquiryDto;
  onClose: () => void;
  onDeclined: (updated: LeaseInquiryDto) => void;
}) {
  const [reason, setReason] = useState('');
  const [busy, setBusy] = useState(false);

  async function handleConfirm() {
    setBusy(true);
    try {
      const updated = await declineLeaseInquiry(inq.marinaId, inq.id, reason || undefined);
      onDeclined(updated);
      onClose();
    } finally {
      setBusy(false);
    }
  }

  return (
    <AlertDialog open onOpenChange={(o) => { if (!o) onClose(); }}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Decline inquiry?</AlertDialogTitle>
          <AlertDialogDescription>
            {inq.requestingUserName}'s {inq.desiredTerm.toLowerCase()} lease request for {inq.slipName} will be declined.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <div className="px-6 pb-2">
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Reason (optional — shared with the boater)"
            rows={2}
            className="w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>
        <AlertDialogFooter>
          <AlertDialogCancel onClick={onClose}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={busy}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {busy ? 'Declining…' : 'Decline'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

// ─── Inquiries page ───────────────────────────────────────────────────────────

export function InquiriesPage() {
  const { marinaId } = useParams({ strict: false }) as { marinaId: string };
  const [statusFilter, setStatusFilter] = useUrlState<StatusFilter>('status', 'all');
  const [editingInq, setEditingInq] = useState<LeaseInquiryDto | null>(null);
  const [approvingInq, setApprovingInq] = useState<LeaseInquiryDto | null>(null);
  const [decliningInq, setDecliningInq] = useState<LeaseInquiryDto | null>(null);

  const queryClient = useQueryClient();

  const { data: inquiries = [], isLoading } = useQuery<LeaseInquiryDto[]>({
    queryKey: ['marina-inquiries', marinaId, statusFilter],
    queryFn: () => getMarinaLeaseInquiries(marinaId, statusFilter !== 'all' ? { status: statusFilter } : undefined),
    staleTime: 30_000,
  });

  const approveM = useMutation({
    mutationFn: (inq: LeaseInquiryDto) => approveLeaseInquiry(inq.marinaId, inq.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['marina-inquiries', marinaId] });
      queryClient.invalidateQueries({ queryKey: ['marina-counters', marinaId] });
      setApprovingInq(null);
    },
  });

  function updateInList(updated: LeaseInquiryDto) {
    queryClient.setQueryData<LeaseInquiryDto[]>(
      ['marina-inquiries', marinaId, statusFilter],
      (prev) => prev?.map((i) => i.id === updated.id ? updated : i) ?? [updated],
    );
    queryClient.invalidateQueries({ queryKey: ['marina-counters', marinaId] });
  }

  const actionable = inquiries.filter((i) => i.status === 'Pending' || i.status === 'UnderReview');
  const history    = inquiries.filter((i) => i.status !== 'Pending' && i.status !== 'UnderReview');
  const showBoth   = statusFilter === 'all';

  const chipCounts: Partial<Record<StatusFilter, number>> = {
    Pending:     inquiries.filter((i) => i.status === 'Pending').length,
    UnderReview: inquiries.filter((i) => i.status === 'UnderReview').length,
  };

  return (
    <>
      <PageHeader
        title="Lease Inquiries"
        subtitle="Seasonal, annual, and monthly lease requests from boaters"
      />
      <PageBody>
        {/* Filter chips */}
        <div className="flex gap-2 flex-wrap mb-5">
          {STATUS_CHIPS.map(({ key, label }) => (
            <FilterChip
              key={key}
              active={statusFilter === key}
              count={chipCounts[key] && chipCounts[key]! > 0 ? chipCounts[key] : undefined}
              onClick={() => setStatusFilter(key)}
            >
              {label}
            </FilterChip>
          ))}
        </div>

        {isLoading && (
          <p className="text-sm text-muted-foreground">Loading…</p>
        )}

        {!isLoading && inquiries.length === 0 && (
          <div className="text-center py-16">
            <p className="text-muted-foreground text-sm">No inquiries found.</p>
            <p className="text-muted-foreground/60 text-xs mt-1">
              Lease inquiries submitted by boaters will appear here.
            </p>
          </div>
        )}

        {/* When viewing all statuses, split into two labeled sections */}
        {!isLoading && showBoth && actionable.length > 0 && (
          <div className="space-y-3 mb-8">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Awaiting action</p>
            {actionable.map((inq) => (
              <InquiryCard key={inq.id} inq={inq}
                onEdit={setEditingInq} onApprove={setApprovingInq} onDecline={setDecliningInq} />
            ))}
          </div>
        )}
        {!isLoading && showBoth && history.length > 0 && (
          <div className="space-y-3">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">History</p>
            {history.map((inq) => (
              <InquiryCard key={inq.id} inq={inq}
                onEdit={setEditingInq} onApprove={setApprovingInq} onDecline={setDecliningInq} />
            ))}
          </div>
        )}

        {/* When filtered to a specific status, show flat list */}
        {!isLoading && !showBoth && inquiries.length > 0 && (
          <div className="space-y-3">
            {inquiries.map((inq) => (
              <InquiryCard key={inq.id} inq={inq}
                onEdit={setEditingInq} onApprove={setApprovingInq} onDecline={setDecliningInq} />
            ))}
          </div>
        )}
      </PageBody>

      {/* Edit terms dialog */}
      {editingInq && (
        <EditTermsDialog
          inq={editingInq}
          onClose={() => setEditingInq(null)}
          onSaved={(updated) => { updateInList(updated); setEditingInq(null); }}
        />
      )}

      {/* Approve confirmation */}
      {approvingInq && (
        <AlertDialog open onOpenChange={(o) => { if (!o) setApprovingInq(null); }}>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Approve inquiry?</AlertDialogTitle>
              <AlertDialogDescription>
                This will approve {approvingInq.requestingUserName}'s {approvingInq.desiredTerm.toLowerCase()} lease
                request for {approvingInq.slipName} and automatically create a slip assignment.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel onClick={() => setApprovingInq(null)}>Cancel</AlertDialogCancel>
              <AlertDialogAction
                onClick={() => approveM.mutate(approvingInq)}
                disabled={approveM.isPending}
              >
                {approveM.isPending ? 'Approving…' : 'Approve'}
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      )}

      {/* Decline dialog */}
      {decliningInq && (
        <DeclineDialog
          inq={decliningInq}
          onClose={() => setDecliningInq(null)}
          onDeclined={(updated) => { updateInList(updated); setDecliningInq(null); }}
        />
      )}
    </>
  );
}
