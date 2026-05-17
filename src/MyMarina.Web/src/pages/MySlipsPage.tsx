import { useEffect, useState } from 'react';
import {
  getMySlipAssignments, getAssignmentAbsences, createOwnerAbsence, deleteOwnerAbsence,
  createSubletWindow, type MySlipAssignmentDto, type OwnerAbsenceDto, type CreateSubletWindowData,
} from '@/api/api';
import { NavBar } from '@/components/NavBar';

function fmt(d: string) {
  const [y, m, day] = d.split('-');
  return new Date(Number(y), Number(m) - 1, Number(day)).toLocaleDateString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
  });
}

const input = 'w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring';
const btn = 'rounded-lg bg-foreground text-white px-4 py-2 text-sm font-medium hover:bg-foreground/90 disabled:opacity-50 transition-colors';
const btnSecondary = 'rounded-lg border border-border text-foreground/80 px-4 py-2 text-sm hover:bg-muted/30 transition-colors';

// ─── Absence list + "I'm away" form ──────────────────────────────────────────

function AbsenceSection({ assignment }: { assignment: MySlipAssignmentDto }) {
  const [absences, setAbsences] = useState<OwnerAbsenceDto[]>([]);
  const [loading, setLoading]   = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [startsOn, setStartsOn] = useState('');
  const [endsOn, setEndsOn]     = useState('');
  const [notes, setNotes]       = useState('');
  const [saving, setSaving]     = useState(false);
  const [error, setError]       = useState<string | null>(null);

  useEffect(() => {
    getAssignmentAbsences(assignment.id)
      .then(setAbsences)
      .finally(() => setLoading(false));
  }, [assignment.id]);

  async function handleCreate() {
    if (!startsOn || !endsOn) { setError('Both dates are required.'); return; }
    setError(null);
    setSaving(true);
    try {
      const a = await createOwnerAbsence(assignment.id, { startsOn, endsOn, notes: notes || null });
      setAbsences((prev) => [...prev, a].sort((a, b) => a.startsOn.localeCompare(b.startsOn)));
      setShowForm(false);
      setStartsOn(''); setEndsOn(''); setNotes('');
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      setError(msg ?? 'Could not save absence.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(absenceId: string) {
    if (!window.confirm('Remove this absence?')) return;
    await deleteOwnerAbsence(assignment.id, absenceId);
    setAbsences((prev) => prev.filter((a) => a.id !== absenceId));
  }

  if (loading) return <p className="text-xs text-muted-foreground/70 mt-2">Loading absences…</p>;

  return (
    <div className="mt-3 border-t border-border/50 pt-3">
      <div className="flex justify-between items-center mb-2">
        <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Away periods</p>
        <button onClick={() => setShowForm(true)} className="text-xs text-muted-foreground hover:text-foreground underline">
          + I'll be away
        </button>
      </div>

      {absences.length === 0 && !showForm && (
        <p className="text-xs text-muted-foreground/70">No away periods recorded.</p>
      )}

      <ul className="space-y-1.5">
        {absences.map((a) => (
          <li key={a.id} className="flex justify-between items-center text-xs text-foreground/80 bg-amber-50 rounded px-2 py-1.5">
            <span>{fmt(a.startsOn)} – {fmt(a.endsOn)}{a.notes ? ` · ${a.notes}` : ''}</span>
            <button onClick={() => handleDelete(a.id)} className="text-muted-foreground/70 hover:text-red-500 ml-2">×</button>
          </li>
        ))}
      </ul>

      {showForm && (
        <div className="mt-2 space-y-2 bg-muted/30 rounded-lg p-3">
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="block text-xs font-medium text-foreground/80 mb-1">Away from</label>
              <input type="date" value={startsOn} onChange={(e) => setStartsOn(e.target.value)} className={input} />
            </div>
            <div>
              <label className="block text-xs font-medium text-foreground/80 mb-1">Back by</label>
              <input type="date" value={endsOn} onChange={(e) => setEndsOn(e.target.value)} className={input} />
            </div>
          </div>
          <input
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Notes (optional)"
            className={input}
          />
          {error && <p className="text-xs text-red-600">{error}</p>}
          <div className="flex gap-2">
            <button onClick={handleCreate} disabled={saving} className={btn}>
              {saving ? 'Saving…' : 'Save absence'}
            </button>
            <button onClick={() => { setShowForm(false); setError(null); }} className={btnSecondary}>
              Cancel
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Holder sublet form ───────────────────────────────────────────────────────

function SubletWindowForm({ assignment, onCreated }: { assignment: MySlipAssignmentDto; onCreated: () => void }) {
  const [show, setShow] = useState(false);
  const [form, setForm] = useState<CreateSubletWindowData>({
    startsAt: '', endsAt: '', instantBook: true, basePricePerNight: 0,
  });
  const [saving, setSaving] = useState(false);
  const [error, setError]   = useState<string | null>(null);

  if (!assignment.allowHolderSublet) return null;

  async function handleCreate() {
    setError(null);
    setSaving(true);
    try {
      await createSubletWindow(assignment.id, {
        ...form,
        startsAt: new Date(form.startsAt).toISOString(),
        endsAt:   new Date(form.endsAt).toISOString(),
      });
      setShow(false);
      onCreated();
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      setError(msg ?? 'Could not create listing.');
    } finally {
      setSaving(false);
    }
  }

  if (!show) {
    return (
      <div className="mt-3 border-t border-border/50 pt-3">
        <button onClick={() => setShow(true)} className="text-xs text-muted-foreground hover:text-foreground underline">
          + Create sublet listing
        </button>
      </div>
    );
  }

  return (
    <div className="mt-3 border-t border-border/50 pt-3 bg-muted/30 rounded-lg p-3 space-y-2">
      <p className="text-xs font-medium text-foreground">New sublet listing</p>
      <div className="grid grid-cols-2 gap-2">
        <div>
          <label className="block text-xs font-medium text-foreground/80 mb-1">Available from</label>
          <input type="date" value={form.startsAt} onChange={(e) => setForm({ ...form, startsAt: e.target.value })} className={input} />
        </div>
        <div>
          <label className="block text-xs font-medium text-foreground/80 mb-1">Until</label>
          <input type="date" value={form.endsAt} onChange={(e) => setForm({ ...form, endsAt: e.target.value })} className={input} />
        </div>
        <div>
          <label className="block text-xs font-medium text-foreground/80 mb-1">Price/night ($)</label>
          <input type="number" step="0.01" min="0.01"
            value={form.basePricePerNight || ''}
            onChange={(e) => setForm({ ...form, basePricePerNight: Number(e.target.value) })}
            className={input}
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-foreground/80 mb-1">Cleaning fee ($)</label>
          <input type="number" step="0.01" min="0"
            value={form.cleaningFee ?? ''}
            onChange={(e) => setForm({ ...form, cleaningFee: e.target.value ? Number(e.target.value) : null })}
            className={input}
          />
        </div>
      </div>
      <label className="flex items-center gap-2 text-xs text-foreground/80">
        <input type="checkbox" checked={form.instantBook} onChange={(e) => setForm({ ...form, instantBook: e.target.checked })} />
        Instant book
      </label>
      {error && <p className="text-xs text-red-600">{error}</p>}
      <div className="flex gap-2">
        <button onClick={handleCreate} disabled={saving} className={btn}>{saving ? 'Creating…' : 'Create listing'}</button>
        <button onClick={() => { setShow(false); setError(null); }} className={btnSecondary}>Cancel</button>
      </div>
    </div>
  );
}

// ─── Single assignment card ───────────────────────────────────────────────────

function AssignmentCard({ a }: { a: MySlipAssignmentDto }) {
  const [refreshKey, setRefreshKey] = useState(0);
  return (
    <div className="bg-card rounded-xl border border-border p-5">
      <div className="flex justify-between items-start">
        <div>
          <p className="text-sm font-semibold text-foreground">{a.marinaName}</p>
          <p className="text-xs text-muted-foreground mt-0.5">
            {a.slipName} · {a.slipType} · {a.vesselName}
          </p>
          <p className="text-xs text-muted-foreground/70 mt-0.5">
            {a.assignmentType} · {a.startDate}{a.endDate ? ` – ${a.endDate}` : ' (open-ended)'} · ${a.baseRate.toLocaleString()}/period
          </p>
        </div>
        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
          a.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-muted text-muted-foreground'
        }`}>
          {a.isActive ? 'Active' : 'Ended'}
        </span>
      </div>

      <AbsenceSection key={`absence-${refreshKey}`} assignment={a} />
      <SubletWindowForm assignment={a} onCreated={() => setRefreshKey((k) => k + 1)} />
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export function MySlipsPage() {
  const [assignments, setAssignments] = useState<MySlipAssignmentDto[]>([]);
  const [loading, setLoading]         = useState(true);

  useEffect(() => {
    getMySlipAssignments()
      .then(setAssignments)
      .finally(() => setLoading(false));
  }, []);

  if (loading) return (
    <div className="min-h-screen bg-muted/30 flex items-center justify-center text-muted-foreground/70 text-sm">Loading…</div>
  );

  return (
    <div className="min-h-screen bg-muted/30">
      <NavBar />
      <div className="max-w-5xl mx-auto py-10 px-4 space-y-6">
        <h1 className="text-xl font-semibold text-foreground">My Slips</h1>

        {assignments.length === 0 && (
          <div className="text-center py-16">
            <p className="text-muted-foreground/70 text-sm">No slip assignments found.</p>
            <p className="text-xs text-muted-foreground/70 mt-1">Your marina adds you to a slip; once accepted you'll see it here.</p>
          </div>
        )}

        {assignments.map((a) => (
          <AssignmentCard key={a.id} a={a} />
        ))}
      </div>
    </div>
  );
}
