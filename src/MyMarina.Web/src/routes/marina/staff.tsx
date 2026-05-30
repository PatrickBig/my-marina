import { useState } from 'react';
import { useParams } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { UserPlus, Trash2 } from 'lucide-react';
import { getMarinaStaff, inviteStaff, revokeStaff, type MembershipDto } from '@/api/api';
import { PageHeader, PageBody } from '@/marina-workspace/PageHeader';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
  AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from '@/components/ui/alert-dialog';

// ─── Invite dialog ────────────────────────────────────────────────────────────

function InviteDialog({
  marinaId,
  open,
  onOpenChange,
  onInvited,
}: {
  marinaId: string;
  open: boolean;
  onOpenChange: (o: boolean) => void;
  onInvited: () => void;
}) {
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<'Staff' | 'Manager'>('Staff');
  const [sending, setSending] = useState(false);

  const inp = 'w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm';

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!email.trim()) return;
    setSending(true);
    try {
      await inviteStaff(marinaId, { email: email.trim(), role });
      setEmail('');
      onOpenChange(false);
      onInvited();
    } finally {
      setSending(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader><DialogTitle>Invite staff member</DialogTitle></DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="space-y-1">
            <label className="text-sm font-medium">Email address</label>
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)}
              className={inp} placeholder="staff@example.com" autoFocus required />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium">Role</label>
            <select value={role} onChange={(e) => setRole(e.target.value as 'Staff' | 'Manager')}
              className={inp}>
              <option value="Staff">Staff</option>
              <option value="Manager">Manager</option>
            </select>
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
            <Button type="submit" disabled={sending}>{sending ? 'Sending…' : 'Send invite'}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─── Staff row ────────────────────────────────────────────────────────────────

function StaffRow({
  member,
  onRevoke,
}: {
  member: MembershipDto;
  onRevoke: () => void;
}) {
  return (
    <tr className="hover:bg-muted/20 transition-colors">
      <td className="px-4 py-3 text-sm font-medium">{member.userEmail ?? member.userId}</td>
      <td className="px-4 py-3">
        <Badge variant={member.isPending ? 'warning' : 'neutral'}>
          {member.isPending ? 'Pending' : member.role}
        </Badge>
      </td>
      <td className="px-4 py-3 text-sm text-muted-foreground">
        {member.isPending ? 'Invitation sent' : 'Active'}
      </td>
      <td className="px-4 py-3">
        <button onClick={onRevoke}
          className="p-1 rounded text-muted-foreground hover:text-destructive" aria-label="Remove staff member">
          <Trash2 className="h-3.5 w-3.5" />
        </button>
      </td>
    </tr>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export function StaffPage() {
  const { marinaId } = useParams({ strict: false }) as { marinaId: string };
  const qc = useQueryClient();
  const [inviteOpen, setInviteOpen] = useState(false);
  const [revokeTarget, setRevokeTarget] = useState<MembershipDto | null>(null);

  const { data: staff = [], isLoading } = useQuery<MembershipDto[]>({
    queryKey: ['marina-staff', marinaId],
    queryFn: () => getMarinaStaff(marinaId),
    staleTime: 30_000,
  });

  const { mutate: doRevoke } = useMutation({
    mutationFn: ({ id }: { id: string }) => revokeStaff(marinaId, id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['marina-staff', marinaId] }),
  });

  function invalidate() {
    qc.invalidateQueries({ queryKey: ['marina-staff', marinaId] });
  }

  return (
    <>
      <PageHeader
        title="Staff"
        subtitle="Manage marina staff access and roles."
        actions={<Button onClick={() => setInviteOpen(true)}><UserPlus className="h-4 w-4 mr-1" /> Invite staff</Button>}
      />
      <PageBody className="space-y-4">
        {isLoading && (
          <div className="space-y-2">
            {[1, 2, 3].map((i) => <div key={i} className="h-12 bg-muted animate-pulse rounded-lg" />)}
          </div>
        )}

        {!isLoading && staff.length === 0 && (
          <div className="py-16 text-center">
            <p className="text-sm text-muted-foreground">No staff yet. Invite someone to get started.</p>
            <Button variant="outline" className="mt-4" onClick={() => setInviteOpen(true)}>
              <UserPlus className="h-4 w-4 mr-2" /> Invite staff member
            </Button>
          </div>
        )}

        {staff.length > 0 && (
          <>
            <div className="rounded-xl border border-border overflow-hidden">
              <table className="w-full text-left">
                <thead className="bg-muted/40 text-xs text-muted-foreground uppercase tracking-wide">
                  <tr>
                    <th className="px-4 py-3">Email</th>
                    <th className="px-4 py-3">Role</th>
                    <th className="px-4 py-3">Status</th>
                    <th className="px-4 py-3" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-border/50">
                  {staff.map((m) => (
                    <StaffRow key={m.id} member={m} onRevoke={() => setRevokeTarget(m)} />
                  ))}
                </tbody>
              </table>
            </div>
            <p className="text-xs text-muted-foreground">
              Granular per-staff permissions are on the roadmap. Currently, all staff members share the same access level within their role.
            </p>
          </>
        )}
      </PageBody>

      <InviteDialog
        marinaId={marinaId}
        open={inviteOpen}
        onOpenChange={setInviteOpen}
        onInvited={invalidate}
      />

      <AlertDialog open={!!revokeTarget} onOpenChange={(o) => { if (!o) setRevokeTarget(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove {revokeTarget?.userEmail ?? revokeTarget?.userId}?</AlertDialogTitle>
            <AlertDialogDescription>
              They will lose access to this marina immediately.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => { if (revokeTarget) { doRevoke({ id: revokeTarget.id }); setRevokeTarget(null); } }}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Remove
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
