import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  getMarina, updateMarina, getDocks, createDock, deleteDock,
  getSlips, createSlip, deleteSlip, getMarinaStaff, inviteStaff, revokeStaff,
  getBillingAccounts, getVesselRecords, getSlipAssignments, createSlipAssignment, endSlipAssignment,
  type MarinaDto, type DockDto, type SlipDto, type MembershipDto, type SlipType,
  type BillingAccountDto, type VesselRecordDto, type SlipAssignmentDto, type AssignmentType,
} from '@/api/api';
import { NavBar } from '@/components/NavBar';

// ─── helpers ─────────────────────────────────────────────────────────────────

function getMarinaIdFromPath(): string | null {
  const m = window.location.pathname.match(/^\/marina\/([^/]+)/);
  return m ? m[1] : null;
}

// ─── Marina info panel ───────────────────────────────────────────────────────

const infoSchema = z.object({
  name: z.string().min(2, 'Required'),
  addressStreet: z.string().optional(),
  addressCity: z.string().optional(),
  addressState: z.string().optional(),
  addressZip: z.string().optional(),
  addressCountry: z.string().optional(),
  phoneNumber: z.string().optional(),
  email: z.string().email('Invalid email').optional().or(z.literal('')),
  website: z.string().optional(),
  description: z.string().optional(),
});
type InfoFormData = z.infer<typeof infoSchema>;

function MarinaInfoPanel({ marina, onSaved }: { marina: MarinaDto; onSaved: (m: MarinaDto) => void }) {
  const [editing, setEditing] = useState(false);
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<InfoFormData>({
    resolver: zodResolver(infoSchema),
    defaultValues: {
      name: marina.name,
      addressStreet: marina.addressStreet ?? '',
      addressCity: marina.addressCity ?? '',
      addressState: marina.addressState ?? '',
      addressZip: marina.addressZip ?? '',
      addressCountry: marina.addressCountry ?? '',
      phoneNumber: marina.phoneNumber ?? '',
      email: marina.email ?? '',
      website: marina.website ?? '',
      description: marina.description ?? '',
    },
  });

  async function onSubmit(data: InfoFormData) {
    const updated = await updateMarina(marina.id, {
      name: data.name,
      addressStreet: data.addressStreet || null,
      addressCity: data.addressCity || null,
      addressState: data.addressState || null,
      addressZip: data.addressZip || null,
      addressCountry: data.addressCountry || null,
      phoneNumber: data.phoneNumber || null,
      email: data.email || null,
      website: data.website || null,
      description: data.description || null,
    });
    onSaved(updated);
    setEditing(false);
  }

  if (!editing) {
    return (
      <div className="bg-white rounded-xl border border-slate-200 p-6">
        <div className="flex justify-between items-start">
          <div>
            <h2 className="text-lg font-semibold text-slate-800">{marina.name}</h2>
            <p className="text-sm text-slate-500 mt-0.5">{marina.marinaType} · {marina.timeZoneId}</p>
            {marina.addressCity && (
              <p className="text-sm text-slate-500 mt-1">
                {[marina.addressStreet, marina.addressCity, marina.addressState, marina.addressZip].filter(Boolean).join(', ')}
              </p>
            )}
            {marina.description && <p className="text-sm text-slate-600 mt-2">{marina.description}</p>}
          </div>
          <button onClick={() => setEditing(true)} className="text-sm text-slate-500 hover:text-slate-800 underline">
            Edit
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-xl border border-slate-200 p-6">
      <h2 className="text-base font-semibold text-slate-800 mb-4">Edit marina info</h2>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <Field label="Marina name" error={errors.name?.message}>
          <input {...register('name')} className={input} />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Street">
            <input {...register('addressStreet')} className={input} />
          </Field>
          <Field label="City">
            <input {...register('addressCity')} className={input} />
          </Field>
          <Field label="State">
            <input {...register('addressState')} className={input} />
          </Field>
          <Field label="Zip">
            <input {...register('addressZip')} className={input} />
          </Field>
        </div>
        <Field label="Phone">
          <input {...register('phoneNumber')} className={input} />
        </Field>
        <Field label="Contact email" error={errors.email?.message}>
          <input {...register('email')} type="email" className={input} />
        </Field>
        <Field label="Website">
          <input {...register('website')} className={input} />
        </Field>
        <Field label="Description">
          <textarea {...register('description')} rows={3} className={`${input} resize-none`} />
        </Field>
        <div className="flex gap-2 pt-1">
          <button type="submit" disabled={isSubmitting} className={btn}>
            {isSubmitting ? 'Saving…' : 'Save'}
          </button>
          <button type="button" onClick={() => { setEditing(false); reset(); }} className={btnSecondary}>
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}

// ─── Docks panel ─────────────────────────────────────────────────────────────

function DocksPanel({ marinaId }: { marinaId: string }) {
  const [docks, setDocks] = useState<DockDto[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [newName, setNewName] = useState('');

  useEffect(() => { getDocks(marinaId).then(setDocks); }, [marinaId]);

  async function handleCreate() {
    if (!newName.trim()) return;
    const dock = await createDock(marinaId, { name: newName.trim(), sortOrder: docks.length });
    setDocks((d) => [...d, dock]);
    setNewName('');
    setShowForm(false);
  }

  async function handleDelete(dockId: string) {
    if (!window.confirm('Delete this dock? Slips assigned to it will become free-standing.')) return;
    await deleteDock(marinaId, dockId);
    setDocks((d) => d.filter((x) => x.id !== dockId));
  }

  return (
    <div className="bg-white rounded-xl border border-slate-200 p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-base font-semibold text-slate-800">Docks</h2>
        <button onClick={() => setShowForm(true)} className="text-sm text-slate-500 hover:text-slate-800 underline">
          + Add dock
        </button>
      </div>

      {docks.length === 0 && !showForm && (
        <p className="text-sm text-slate-400">No docks yet. Slips can also be free-standing.</p>
      )}

      <ul className="divide-y divide-slate-100">
        {docks.map((dock) => (
          <li key={dock.id} className="py-2.5 flex justify-between items-center">
            <span className="text-sm text-slate-700">{dock.name}</span>
            <button
              onClick={() => handleDelete(dock.id)}
              className="text-xs text-slate-400 hover:text-red-500"
            >
              Remove
            </button>
          </li>
        ))}
      </ul>

      {showForm && (
        <div className="mt-3 flex gap-2">
          <input
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            placeholder="Dock name"
            className={`${input} flex-1`}
            onKeyDown={(e) => e.key === 'Enter' && handleCreate()}
            autoFocus
          />
          <button onClick={handleCreate} className={btn}>Add</button>
          <button onClick={() => { setShowForm(false); setNewName(''); }} className={btnSecondary}>Cancel</button>
        </div>
      )}
    </div>
  );
}

// ─── Slips panel ─────────────────────────────────────────────────────────────

const slipSchema = z.object({
  name: z.string().min(1, 'Required'),
  slipType: z.enum(['Floating', 'Fixed', 'Mooring', 'DryStorage', 'Anchorage'] as const),
  maxLength: z.coerce.number().positive(),
  maxBeam: z.coerce.number().positive(),
  maxDraft: z.coerce.number().positive(),
  hasElectric: z.boolean(),
  hasWater: z.boolean(),
  notes: z.string().optional(),
});
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type SlipFormData = z.infer<typeof slipSchema>;

const SLIP_TYPE_LABELS: Record<SlipType, string> = {
  Floating: 'Floating', Fixed: 'Fixed', Mooring: 'Mooring', DryStorage: 'Dry Storage', Anchorage: 'Anchorage',
};

function SlipsPanel({ marinaId }: { marinaId: string }) {
  const [slips, setSlips] = useState<SlipDto[]>([]);
  const [showForm, setShowForm] = useState(false);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<SlipFormData>({
    resolver: zodResolver(slipSchema) as any,
    defaultValues: { slipType: 'Floating', hasElectric: false, hasWater: true },
  });

  useEffect(() => { getSlips(marinaId).then(setSlips); }, [marinaId]);

  async function onSubmit(data: SlipFormData) {
    const slip = await createSlip(marinaId, {
      name: data.name, slipType: data.slipType,
      maxLength: data.maxLength, maxBeam: data.maxBeam, maxDraft: data.maxDraft,
      hasElectric: data.hasElectric, hasWater: data.hasWater,
      notes: data.notes || null,
    });
    setSlips((s) => [...s, slip]);
    reset();
    setShowForm(false);
  }

  async function handleDelete(slipId: string) {
    if (!window.confirm('Delete this slip?')) return;
    await deleteSlip(marinaId, slipId);
    setSlips((s) => s.filter((x) => x.id !== slipId));
  }

  return (
    <div className="bg-white rounded-xl border border-slate-200 p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-base font-semibold text-slate-800">Slips</h2>
        <button onClick={() => setShowForm(true)} className="text-sm text-slate-500 hover:text-slate-800 underline">
          + Add slip
        </button>
      </div>

      {slips.length === 0 && !showForm && (
        <p className="text-sm text-slate-400">No slips yet.</p>
      )}

      <ul className="divide-y divide-slate-100">
        {slips.map((slip) => (
          <li key={slip.id} className="py-2.5 flex justify-between items-center">
            <div>
              <span className="text-sm font-medium text-slate-700">{slip.name}</span>
              <span className="ml-2 text-xs text-slate-400">{slip.slipType} · {slip.maxLength}′ L · {slip.maxBeam}′ B</span>
              {slip.hasWater && <span className="ml-1 text-xs text-slate-400">· Water</span>}
              {slip.hasElectric && <span className="ml-1 text-xs text-slate-400">· Electric {slip.electric}A</span>}
            </div>
            <button onClick={() => handleDelete(slip.id)} className="text-xs text-slate-400 hover:text-red-500">
              Remove
            </button>
          </li>
        ))}
      </ul>

      {showForm && (
        <form onSubmit={handleSubmit(onSubmit)} className="mt-4 space-y-3 border-t border-slate-100 pt-4">
          <h3 className="text-sm font-medium text-slate-700">New slip</h3>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Slip name / number" error={errors.name?.message}>
              <input {...register('name')} placeholder="A-1" className={input} />
            </Field>
            <Field label="Type">
              <select {...register('slipType')} className={`${input} bg-white`}>
                {(Object.keys(SLIP_TYPE_LABELS) as SlipType[]).map((t) => (
                  <option key={t} value={t}>{SLIP_TYPE_LABELS[t]}</option>
                ))}
              </select>
            </Field>
            <Field label="Max length (ft)" error={errors.maxLength?.message}>
              <input {...register('maxLength')} type="number" step="0.1" className={input} />
            </Field>
            <Field label="Max beam (ft)" error={errors.maxBeam?.message}>
              <input {...register('maxBeam')} type="number" step="0.1" className={input} />
            </Field>
            <Field label="Max draft (ft)" error={errors.maxDraft?.message}>
              <input {...register('maxDraft')} type="number" step="0.1" className={input} />
            </Field>
          </div>
          <div className="flex gap-4 text-sm">
            <label className="flex items-center gap-2 text-slate-600">
              <input {...register('hasWater')} type="checkbox" /> Water
            </label>
            <label className="flex items-center gap-2 text-slate-600">
              <input {...register('hasElectric')} type="checkbox" /> Electric
            </label>
          </div>
          <Field label="Notes">
            <textarea {...register('notes')} rows={2} className={`${input} resize-none`} />
          </Field>
          <div className="flex gap-2">
            <button type="submit" disabled={isSubmitting} className={btn}>
              {isSubmitting ? 'Adding…' : 'Add slip'}
            </button>
            <button type="button" onClick={() => { setShowForm(false); reset(); }} className={btnSecondary}>
              Cancel
            </button>
          </div>
        </form>
      )}
    </div>
  );
}

// ─── Assignments panel ───────────────────────────────────────────────────────

const ASSIGNMENT_TYPES: AssignmentType[] = ['Transient', 'Monthly', 'Seasonal', 'Annual'];

const assignmentSchema = z.object({
  slipId:                  z.string().min(1, 'Required'),
  billingAccountId:        z.string().min(1, 'Required'),
  vesselId:                z.string().min(1, 'Required'),
  assignmentType:          z.enum(['Transient', 'Monthly', 'Seasonal', 'Annual'] as const),
  startDate:               z.string().min(1, 'Required'),
  endDate:                 z.string().optional(),
  baseRate:                z.coerce.number().min(0, 'Must be ≥ 0'),
  allowOwnerSubletWhenAway: z.boolean(),
  allowHolderSublet:       z.boolean(),
});
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AssignmentFormData = z.infer<typeof assignmentSchema>;

function AssignmentsPanel({ marinaId }: { marinaId: string }) {
  const [assignments, setAssignments] = useState<SlipAssignmentDto[]>([]);
  const [slips, setSlips] = useState<SlipDto[]>([]);
  const [accounts, setAccounts] = useState<BillingAccountDto[]>([]);
  const [vesselRecords, setVesselRecords] = useState<VesselRecordDto[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const { register, handleSubmit, reset, watch, formState: { errors, isSubmitting } } = useForm<AssignmentFormData>({
    resolver: zodResolver(assignmentSchema) as any,
    defaultValues: {
      assignmentType: 'Annual',
      baseRate: 0,
      allowOwnerSubletWhenAway: false,
      allowHolderSublet: false,
    },
  });

  const selectedAccountId = watch('billingAccountId');

  useEffect(() => {
    Promise.all([
      getSlipAssignments(marinaId, { activeOnly: true }),
      getSlips(marinaId),
      getBillingAccounts(marinaId),
    ]).then(([a, s, b]) => { setAssignments(a); setSlips(s); setAccounts(b); });
  }, [marinaId]);

  // When billing account changes, load vessel records for that account
  useEffect(() => {
    if (!selectedAccountId) { setVesselRecords([]); return; }
    getVesselRecords(marinaId, selectedAccountId).then(setVesselRecords);
  }, [marinaId, selectedAccountId]);

  async function onSubmit(data: AssignmentFormData) {
    setError(null);
    try {
      const item = await createSlipAssignment(marinaId, {
        slipId:                   data.slipId,
        billingAccountId:         data.billingAccountId,
        vesselId:                 data.vesselId,
        assignmentType:           data.assignmentType,
        startDate:                data.startDate,
        endDate:                  data.endDate || null,
        baseRate:                 data.baseRate,
        allowOwnerSubletWhenAway: data.allowOwnerSubletWhenAway,
        allowHolderSublet:        data.allowHolderSublet,
        ownerSubletShareToHolder: 0,
        holderSubletShareToOwner: 0,
      });
      setAssignments((prev) => [item, ...prev]);
      reset();
      setShowForm(false);
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      setError(msg ?? 'Could not create assignment. Check for conflicts.');
    }
  }

  async function handleEnd(id: string) {
    if (!window.confirm('End this assignment today?')) return;
    const updated = await endSlipAssignment(marinaId, id);
    setAssignments((prev) => prev.map((a) => a.id === id ? updated : a).filter((a) => a.isActive));
  }

  return (
    <div className="bg-white rounded-xl border border-slate-200 p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-base font-semibold text-slate-800">Slip Assignments</h2>
        <button onClick={() => setShowForm(true)} className="text-sm text-slate-500 hover:text-slate-800 underline">
          + New assignment
        </button>
      </div>

      {assignments.length === 0 && !showForm && (
        <p className="text-sm text-slate-400">No active assignments.</p>
      )}

      <ul className="divide-y divide-slate-100">
        {assignments.map((a) => (
          <li key={a.id} className="py-3 flex justify-between items-start">
            <div>
              <p className="text-sm font-medium text-slate-800">
                {a.slipName}
                <span className="ml-2 text-xs text-slate-400 font-normal">{a.assignmentType}</span>
              </p>
              <p className="text-xs text-slate-500 mt-0.5">
                {a.billingAccountDisplayName} · {a.vesselName}
              </p>
              <p className="text-xs text-slate-400 mt-0.5">
                {a.startDate} – {a.endDate ?? 'open-ended'} · ${a.baseRate.toLocaleString()}/period
              </p>
            </div>
            <button onClick={() => handleEnd(a.id)} className="text-xs text-slate-400 hover:text-red-500 mt-1">
              End
            </button>
          </li>
        ))}
      </ul>

      {showForm && (
        <form onSubmit={handleSubmit(onSubmit)} className="mt-4 space-y-3 border-t border-slate-100 pt-4">
          <h3 className="text-sm font-medium text-slate-700">New assignment</h3>

          <div className="grid grid-cols-2 gap-3">
            <Field label="Slip" error={errors.slipId?.message}>
              <select {...register('slipId')} className={`${input} bg-white`}>
                <option value="">— select slip —</option>
                {slips.map((s) => (
                  <option key={s.id} value={s.id}>{s.name} ({s.maxLength}′)</option>
                ))}
              </select>
            </Field>

            <Field label="Assignment type">
              <select {...register('assignmentType')} className={`${input} bg-white`}>
                {ASSIGNMENT_TYPES.map((t) => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>
            </Field>

            <Field label="Billing account" error={errors.billingAccountId?.message}>
              <select {...register('billingAccountId')} className={`${input} bg-white`}>
                <option value="">— select account —</option>
                {accounts.map((a) => (
                  <option key={a.id} value={a.id}>{a.displayName}</option>
                ))}
              </select>
            </Field>

            <Field label="Vessel" error={errors.vesselId?.message}>
              <select {...register('vesselId')} className={`${input} bg-white`} disabled={!selectedAccountId}>
                <option value="">— select vessel —</option>
                {vesselRecords.map((r) => (
                  <option key={r.vesselId} value={r.vesselId}>{r.vesselName}{r.vesselIsGhost ? ' (unregistered)' : ''}</option>
                ))}
              </select>
            </Field>

            <Field label="Start date" error={errors.startDate?.message}>
              <input {...register('startDate')} type="date" className={input} />
            </Field>

            <Field label="End date (leave blank for open-ended)">
              <input {...register('endDate')} type="date" className={input} />
            </Field>

            <Field label="Base rate ($)" error={errors.baseRate?.message}>
              <input {...register('baseRate')} type="number" step="0.01" min="0" className={input} />
            </Field>
          </div>

          <div className="flex gap-4 text-sm">
            <label className="flex items-center gap-2 text-slate-600">
              <input {...register('allowHolderSublet')} type="checkbox" /> Holder may sublet
            </label>
            <label className="flex items-center gap-2 text-slate-600">
              <input {...register('allowOwnerSubletWhenAway')} type="checkbox" /> Marina may sublet when away
            </label>
          </div>

          {error && <p className="text-sm text-red-600">{error}</p>}

          <div className="flex gap-2">
            <button type="submit" disabled={isSubmitting} className={btn}>
              {isSubmitting ? 'Saving…' : 'Assign slip'}
            </button>
            <button type="button" onClick={() => { setShowForm(false); reset(); setError(null); }} className={btnSecondary}>
              Cancel
            </button>
          </div>
        </form>
      )}
    </div>
  );
}

// ─── Staff panel ─────────────────────────────────────────────────────────────

function StaffPanel({ marinaId }: { marinaId: string }) {
  const [staff, setStaff] = useState<MembershipDto[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState<'Staff' | 'Manager'>('Staff');
  const [loading, setLoading] = useState(false);

  useEffect(() => { getMarinaStaff(marinaId).then(setStaff); }, [marinaId]);

  async function handleInvite() {
    if (!inviteEmail.trim()) return;
    setLoading(true);
    try {
      const m = await inviteStaff(marinaId, { email: inviteEmail.trim(), role: inviteRole });
      setStaff((s) => [...s, m]);
      setInviteEmail('');
      setShowForm(false);
    } finally {
      setLoading(false);
    }
  }

  async function handleRevoke(membershipId: string) {
    if (!window.confirm('Remove this staff member?')) return;
    await revokeStaff(marinaId, membershipId);
    setStaff((s) => s.filter((x) => x.id !== membershipId));
  }

  return (
    <div className="bg-white rounded-xl border border-slate-200 p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-base font-semibold text-slate-800">Staff</h2>
        <button onClick={() => setShowForm(true)} className="text-sm text-slate-500 hover:text-slate-800 underline">
          + Invite
        </button>
      </div>

      {staff.length === 0 && !showForm && (
        <p className="text-sm text-slate-400">No staff yet.</p>
      )}

      <ul className="divide-y divide-slate-100">
        {staff.map((m) => (
          <li key={m.id} className="py-2.5 flex justify-between items-center">
            <div>
              <span className="text-sm text-slate-700">{m.userEmail ?? m.userId}</span>
              <span className={`ml-2 text-xs px-1.5 py-0.5 rounded-full ${
                m.isPending
                  ? 'bg-amber-50 text-amber-700'
                  : 'bg-slate-100 text-slate-500'
              }`}>
                {m.isPending ? 'Pending' : m.role}
              </span>
            </div>
            <button onClick={() => handleRevoke(m.id)} className="text-xs text-slate-400 hover:text-red-500">
              Remove
            </button>
          </li>
        ))}
      </ul>

      {showForm && (
        <div className="mt-3 space-y-2 border-t border-slate-100 pt-3">
          <div className="flex gap-2">
            <input
              value={inviteEmail}
              onChange={(e) => setInviteEmail(e.target.value)}
              type="email"
              placeholder="staff@example.com"
              className={`${input} flex-1`}
            />
            <select
              value={inviteRole}
              onChange={(e) => setInviteRole(e.target.value as 'Staff' | 'Manager')}
              className={`${input} w-28 bg-white`}
            >
              <option value="Staff">Staff</option>
              <option value="Manager">Manager</option>
            </select>
          </div>
          <div className="flex gap-2">
            <button onClick={handleInvite} disabled={loading} className={btn}>
              {loading ? 'Sending…' : 'Send invite'}
            </button>
            <button onClick={() => { setShowForm(false); setInviteEmail(''); }} className={btnSecondary}>
              Cancel
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export function MarinaDashboardPage() {
  const marinaId = getMarinaIdFromPath();
  const [marina, setMarina] = useState<MarinaDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!marinaId) { setError('Marina not found.'); setLoading(false); return; }
    getMarina(marinaId)
      .then(setMarina)
      .catch(() => setError('Could not load marina.'))
      .finally(() => setLoading(false));
  }, [marinaId]);

  if (loading) return <div className="min-h-screen bg-slate-50 flex items-center justify-center text-slate-400 text-sm">Loading…</div>;
  if (error || !marina || !marinaId) return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center">
      <p className="text-slate-500 text-sm">{error ?? 'Marina not found.'}</p>
    </div>
  );

  return (
    <div className="min-h-screen bg-slate-50">
      <NavBar />
      <div className="max-w-4xl mx-auto py-10 px-4 space-y-6">
        <MarinaInfoPanel marina={marina} onSaved={setMarina} />
        <DocksPanel marinaId={marinaId} />
        <SlipsPanel marinaId={marinaId} />
        <AssignmentsPanel marinaId={marinaId} />
        <StaffPanel marinaId={marinaId} />
      </div>
    </div>
  );
}

// ─── shared style tokens ─────────────────────────────────────────────────────

const input = 'w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400';
const btn = 'rounded-lg bg-slate-800 text-white px-4 py-2 text-sm font-medium hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors';
const btnSecondary = 'rounded-lg border border-slate-300 text-slate-600 px-4 py-2 text-sm hover:bg-slate-50 transition-colors';

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-xs font-medium text-slate-600 mb-1">{label}</label>
      {children}
      {error && <p className="mt-0.5 text-xs text-red-600">{error}</p>}
    </div>
  );
}
