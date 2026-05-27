import { useEffect, useState } from 'react';
import { useParams } from '@tanstack/react-router';
import { NavBar } from '@/components/NavBar';
import { PricingPreviewPanel } from '@/components/PricingPreviewPanel';
import {
  getPricingPlans, createPricingPlan, updatePricingPlan, deletePricingPlan,
  setDefaultPricingPlan, bulkAssignPricingPlan, getDocks,
  type PricingPlanDto, type AmenityAddOnDto, type DockDto, type PricingPlanUpsertData,
} from '@/api/api';

// ─── Shared styles ────────────────────────────────────────────────────────────
const input = 'w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring bg-background';
const btn = 'rounded-lg bg-primary text-primary-foreground px-4 py-2 text-sm font-medium hover:bg-primary/90 disabled:opacity-50 transition-colors';
const btnSecondary = 'rounded-lg border border-border bg-card px-4 py-2 text-sm font-medium hover:bg-muted/30 transition-colors';
const btnDanger = 'rounded-lg bg-red-600 text-white px-4 py-2 text-sm font-medium hover:bg-red-700 disabled:opacity-50 transition-colors';

const RATE_KINDS = [
  { value: '', label: '— None (listing kind not supported) —' },
  { value: 'Flat', label: 'Flat (fixed amount)' },
  { value: 'PerFoot', label: 'Per foot (× slip length)' },
  { value: 'PerArea', label: 'Per area (× length × beam)' },
];

const AMENITIES: { value: AmenityAddOnDto['amenity']; label: string }[] = [
  { value: 'Covered', label: 'Covered slip' },
  { value: 'Electric30A', label: '30A electric' },
  { value: 'Electric50A', label: '50A electric' },
  { value: 'HasWater', label: 'Water hookup' },
  { value: 'HasPumpOut', label: 'Pump-out' },
];

function rateLabel(kind?: string | null, amount?: number | null, minCharge?: number | null): string {
  if (!kind || amount == null) return '—';
  const rate = kind === 'Flat' ? `$${amount}/night` : kind === 'PerFoot' ? `$${amount}/ft` : `$${amount}/ft²`;
  return minCharge != null ? `${rate} (min $${minCharge})` : rate;
}

// ─── Plan form ────────────────────────────────────────────────────────────────

interface PlanFormState {
  name: string;
  isDefault: boolean;
  transientRateKind: string;
  transientAmount: string;
  transientMinCharge: string;
  leaseRateKind: string;
  leaseAmount: string;
  leaseMinCharge: string;
  addOns: { amenity: AmenityAddOnDto['amenity']; transientAmount: string; leaseAmount: string }[];
}

function blankForm(plan?: PricingPlanDto): PlanFormState {
  return {
    name: plan?.name ?? '',
    isDefault: plan?.isDefault ?? false,
    transientRateKind: plan?.transientRateKind ?? '',
    transientAmount: plan?.transientAmount != null ? String(plan.transientAmount) : '',
    transientMinCharge: plan?.transientMinCharge != null ? String(plan.transientMinCharge) : '',
    leaseRateKind: plan?.leaseRateKind ?? '',
    leaseAmount: plan?.leaseAmount != null ? String(plan.leaseAmount) : '',
    leaseMinCharge: plan?.leaseMinCharge != null ? String(plan.leaseMinCharge) : '',
    addOns: AMENITIES.map((a) => {
      const existing = plan?.amenityAddOns.find((x) => x.amenity === a.value);
      return {
        amenity: a.value,
        transientAmount: existing?.transientAmount != null ? String(existing.transientAmount) : '',
        leaseAmount: existing?.leaseAmount != null ? String(existing.leaseAmount) : '',
      };
    }),
  };
}

function formToPayload(f: PlanFormState): PricingPlanUpsertData {
  return {
    name: f.name,
    isDefault: f.isDefault,
    transientRateKind: f.transientRateKind || null,
    transientAmount: f.transientRateKind && f.transientAmount !== '' ? parseFloat(f.transientAmount) : null,
    transientMinCharge: f.transientRateKind && f.transientMinCharge !== '' ? parseFloat(f.transientMinCharge) : null,
    leaseRateKind: f.leaseRateKind || null,
    leaseAmount: f.leaseRateKind && f.leaseAmount !== '' ? parseFloat(f.leaseAmount) : null,
    leaseMinCharge: f.leaseRateKind && f.leaseMinCharge !== '' ? parseFloat(f.leaseMinCharge) : null,
    amenityAddOns: f.addOns
      .filter((a) => a.transientAmount !== '' || a.leaseAmount !== '')
      .map((a) => ({
        amenity: a.amenity,
        transientAmount: a.transientAmount !== '' ? parseFloat(a.transientAmount) : null,
        leaseAmount: a.leaseAmount !== '' ? parseFloat(a.leaseAmount) : null,
      })),
  };
}

function PlanForm({
  initial, onSave, onCancel, saving,
}: {
  initial?: PricingPlanDto;
  onSave: (data: PricingPlanUpsertData) => Promise<void>;
  onCancel: () => void;
  saving: boolean;
}) {
  const [f, setF] = useState<PlanFormState>(() => blankForm(initial));
  const upd = (patch: Partial<PlanFormState>) => setF((prev) => ({ ...prev, ...patch }));

  function updateAddOn(amenity: AmenityAddOnDto['amenity'], field: 'transientAmount' | 'leaseAmount', value: string) {
    setF((prev) => ({
      ...prev,
      addOns: prev.addOns.map((a) => a.amenity === amenity ? { ...a, [field]: value } : a),
    }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!f.name.trim()) return;
    await onSave(formToPayload(f));
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="sm:col-span-2">
          <label className="block text-sm font-medium text-foreground mb-1">Plan name <span className="text-red-500">*</span></label>
          <input value={f.name} onChange={(e) => upd({ name: e.target.value })} required className={input} placeholder="Standard" />
        </div>

        <label className="sm:col-span-2 flex items-center gap-2 cursor-pointer text-sm text-foreground">
          <input type="checkbox" checked={f.isDefault} onChange={(e) => upd({ isDefault: e.target.checked })} className="rounded" />
          <span>Set as default plan <span className="text-muted-foreground text-xs">(applied to slips with no explicit plan)</span></span>
        </label>
      </div>

      {/* Transient rates */}
      <div className="rounded-xl border border-border p-4 space-y-3">
        <h3 className="text-sm font-semibold text-foreground">Transient (nightly) rate</h3>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div>
            <label className="block text-xs font-medium text-foreground/80 mb-1">Rate kind</label>
            <select value={f.transientRateKind} onChange={(e) => upd({ transientRateKind: e.target.value })} className={`${input} bg-card`}>
              {RATE_KINDS.map((k) => <option key={k.value} value={k.value}>{k.label}</option>)}
            </select>
          </div>
          {f.transientRateKind && (
            <>
              <div>
                <label className="block text-xs font-medium text-foreground/80 mb-1">
                  Amount ($/{f.transientRateKind === 'Flat' ? 'night' : f.transientRateKind === 'PerFoot' ? 'ft' : 'ft²'})
                </label>
                <input type="number" step="0.01" min="0" value={f.transientAmount}
                  onChange={(e) => upd({ transientAmount: e.target.value })}
                  required={!!f.transientRateKind} className={input} placeholder="0.00" />
              </div>
              <div>
                <label className="block text-xs font-medium text-foreground/80 mb-1">Min charge ($, optional)</label>
                <input type="number" step="0.01" min="0" value={f.transientMinCharge}
                  onChange={(e) => upd({ transientMinCharge: e.target.value })} className={input} placeholder="—" />
              </div>
            </>
          )}
        </div>
      </div>

      {/* Lease rates */}
      <div className="rounded-xl border border-border p-4 space-y-3">
        <h3 className="text-sm font-semibold text-foreground">Lease rate</h3>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div>
            <label className="block text-xs font-medium text-foreground/80 mb-1">Rate kind</label>
            <select value={f.leaseRateKind} onChange={(e) => upd({ leaseRateKind: e.target.value })} className={`${input} bg-card`}>
              {RATE_KINDS.map((k) => <option key={k.value} value={k.value}>{k.label}</option>)}
            </select>
          </div>
          {f.leaseRateKind && (
            <>
              <div>
                <label className="block text-xs font-medium text-foreground/80 mb-1">
                  Amount ($/{f.leaseRateKind === 'Flat' ? 'mo' : f.leaseRateKind === 'PerFoot' ? 'ft' : 'ft²'})
                </label>
                <input type="number" step="0.01" min="0" value={f.leaseAmount}
                  onChange={(e) => upd({ leaseAmount: e.target.value })}
                  required={!!f.leaseRateKind} className={input} placeholder="0.00" />
              </div>
              <div>
                <label className="block text-xs font-medium text-foreground/80 mb-1">Min charge ($, optional)</label>
                <input type="number" step="0.01" min="0" value={f.leaseMinCharge}
                  onChange={(e) => upd({ leaseMinCharge: e.target.value })} className={input} placeholder="—" />
              </div>
            </>
          )}
        </div>
      </div>

      {/* Amenity add-ons */}
      <div className="rounded-xl border border-border p-4 space-y-3">
        <div>
          <h3 className="text-sm font-semibold text-foreground">Amenity add-ons <span className="text-muted-foreground font-normal text-xs">(optional surcharges)</span></h3>
          <p className="text-xs text-muted-foreground mt-0.5">Extra amount added when a slip has this amenity. Leave blank if not applicable.</p>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-xs text-muted-foreground border-b border-border">
                <th className="text-left py-1.5 pr-4 font-medium">Amenity</th>
                <th className="text-left py-1.5 pr-4 font-medium w-32">Transient add-on ($)</th>
                <th className="text-left py-1.5 font-medium w-32">Lease add-on ($)</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/50">
              {f.addOns.map((addOn) => {
                const amenity = AMENITIES.find((a) => a.value === addOn.amenity)!;
                return (
                  <tr key={addOn.amenity}>
                    <td className="py-2 pr-4 text-sm text-foreground">{amenity.label}</td>
                    <td className="py-2 pr-4">
                      <input type="number" step="0.01" min="0" value={addOn.transientAmount}
                        onChange={(e) => updateAddOn(addOn.amenity, 'transientAmount', e.target.value)}
                        className="w-28 rounded-lg border border-border px-2 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-ring"
                        placeholder="—" />
                    </td>
                    <td className="py-2">
                      <input type="number" step="0.01" min="0" value={addOn.leaseAmount}
                        onChange={(e) => updateAddOn(addOn.amenity, 'leaseAmount', e.target.value)}
                        className="w-28 rounded-lg border border-border px-2 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-ring"
                        placeholder="—" />
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      <PricingPreviewPanel
        transientRateKind={f.transientRateKind}
        transientAmount={f.transientAmount}
        transientMinCharge={f.transientMinCharge}
        leaseRateKind={f.leaseRateKind}
        leaseAmount={f.leaseAmount}
        leaseMinCharge={f.leaseMinCharge}
        amenityAddOns={f.addOns}
        defaultExpanded={true}
      />

      <div className="flex justify-end gap-3 pt-2">
        <button type="button" onClick={onCancel} className={btnSecondary}>Cancel</button>
        <button type="submit" disabled={saving} className={btn}>
          {saving ? 'Saving…' : initial ? 'Update plan' : 'Create plan'}
        </button>
      </div>
    </form>
  );
}

// ─── Bulk assign dialog ───────────────────────────────────────────────────────

function BulkAssignDialog({
  marinaId, plans, targetPlanId, onClose,
}: {
  marinaId: string;
  plans: PricingPlanDto[];
  targetPlanId: string;
  onClose: () => void;
}) {
  const [docks, setDocks] = useState<DockDto[]>([]);
  const [dockId, setDockId] = useState('');
  const [minLength, setMinLength] = useState('');
  const [maxLength, setMaxLength] = useState('');
  const [minBeam, setMinBeam] = useState('');
  const [maxBeam, setMaxBeam] = useState('');
  const [currentPlanId, setCurrentPlanId] = useState('');
  const [amenities, setAmenities] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { getDocks(marinaId).then(setDocks); }, [marinaId]);

  function toggleAmenity(a: string) {
    setAmenities((prev) => prev.includes(a) ? prev.filter((x) => x !== a) : [...prev, a]);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    setResult(null);
    try {
      const res = await bulkAssignPricingPlan(marinaId, {
        pricingPlanId: targetPlanId,
        dockId: dockId || null,
        minLength: minLength !== '' ? parseFloat(minLength) : null,
        maxLength: maxLength !== '' ? parseFloat(maxLength) : null,
        minBeam: minBeam !== '' ? parseFloat(minBeam) : null,
        maxBeam: maxBeam !== '' ? parseFloat(maxBeam) : null,
        amenities: amenities.length > 0 ? amenities : null,
        currentPlanId: currentPlanId || null,
      });
      setResult(`${res.assignedCount} slip${res.assignedCount !== 1 ? 's' : ''} updated.`);
    } catch {
      setError('Bulk assign failed. Please try again.');
    } finally {
      setSubmitting(false);
    }
  }

  const targetPlan = plans.find((p) => p.id === targetPlanId);

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-card rounded-2xl border border-border shadow-xl w-full max-w-lg p-6 space-y-4">
        <div className="flex justify-between items-start">
          <div>
            <h2 className="text-base font-semibold text-foreground">Bulk assign to "{targetPlan?.name}"</h2>
            <p className="text-xs text-muted-foreground mt-0.5">Apply filters to choose which slips to assign. Leave blank to match all slips.</p>
          </div>
          <button type="button" onClick={onClose} className="text-muted-foreground hover:text-foreground text-xl leading-none">×</button>
        </div>

        {result ? (
          <div className="rounded-xl bg-emerald-50 border border-emerald-200 px-4 py-3 text-sm text-emerald-800">
            {result}
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs font-medium text-foreground/80 mb-1">Dock (optional)</label>
              <select value={dockId} onChange={(e) => setDockId(e.target.value)} className={`${input} bg-card`}>
                <option value="">All docks</option>
                {docks.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
              </select>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-foreground/80 mb-1">Min length (ft)</label>
                <input type="number" step="0.5" min="0" value={minLength} onChange={(e) => setMinLength(e.target.value)} className={input} placeholder="—" />
              </div>
              <div>
                <label className="block text-xs font-medium text-foreground/80 mb-1">Max length (ft)</label>
                <input type="number" step="0.5" min="0" value={maxLength} onChange={(e) => setMaxLength(e.target.value)} className={input} placeholder="—" />
              </div>
              <div>
                <label className="block text-xs font-medium text-foreground/80 mb-1">Min beam (ft)</label>
                <input type="number" step="0.5" min="0" value={minBeam} onChange={(e) => setMinBeam(e.target.value)} className={input} placeholder="—" />
              </div>
              <div>
                <label className="block text-xs font-medium text-foreground/80 mb-1">Max beam (ft)</label>
                <input type="number" step="0.5" min="0" value={maxBeam} onChange={(e) => setMaxBeam(e.target.value)} className={input} placeholder="—" />
              </div>
            </div>

            <div>
              <label className="block text-xs font-medium text-foreground/80 mb-1">Amenities (slip must have all selected)</label>
              <div className="flex flex-wrap gap-2 mt-1">
                {AMENITIES.map((a) => (
                  <button
                    key={a.value}
                    type="button"
                    onClick={() => toggleAmenity(a.value)}
                    className={`px-2.5 py-1 rounded-full text-xs font-medium border transition-colors ${
                      amenities.includes(a.value)
                        ? 'bg-primary text-primary-foreground border-primary'
                        : 'border-border text-foreground/70 hover:bg-muted/30'
                    }`}
                  >
                    {a.label}
                  </button>
                ))}
              </div>
            </div>

            <div>
              <label className="block text-xs font-medium text-foreground/80 mb-1">Only replace slips currently using</label>
              <select value={currentPlanId} onChange={(e) => setCurrentPlanId(e.target.value)} className={`${input} bg-card`}>
                <option value="">Any plan (or no plan)</option>
                <option value="__unassigned__">Unassigned (using default plan)</option>
                {plans.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
              </select>
            </div>

            {error && <p className="text-sm text-red-600">{error}</p>}

            <div className="flex justify-end gap-3 pt-1">
              <button type="button" onClick={onClose} className={btnSecondary}>Cancel</button>
              <button type="submit" disabled={submitting} className={btn}>
                {submitting ? 'Assigning…' : 'Apply bulk assign'}
              </button>
            </div>
          </form>
        )}

        {result && (
          <button type="button" onClick={onClose} className={btn}>
            Done
          </button>
        )}
      </div>
    </div>
  );
}

// ─── Plan card ────────────────────────────────────────────────────────────────

function PlanCard({
  plan, marinaId, plans,
  onSetDefault, onEdit, onDelete,
}: {
  plan: PricingPlanDto;
  marinaId: string;
  plans: PricingPlanDto[];
  onSetDefault: () => void;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const [bulkTarget, setBulkTarget] = useState<string | null>(null);

  return (
    <>
      <div className="rounded-xl border border-border bg-card p-5 space-y-3">
        <div className="flex items-start justify-between gap-3">
          <div className="space-y-0.5">
            <div className="flex items-center gap-2">
              <span className="text-base font-semibold text-foreground">{plan.name}</span>
              {plan.isDefault && (
                <span className="rounded-full bg-primary/10 text-primary text-xs font-medium px-2 py-0.5">Default</span>
              )}
            </div>
            <p className="text-xs text-muted-foreground">
              Transient: {rateLabel(plan.transientRateKind, plan.transientAmount, plan.transientMinCharge)}
              {' · '}
              Lease: {rateLabel(plan.leaseRateKind, plan.leaseAmount, plan.leaseMinCharge)}
            </p>
            {plan.amenityAddOns.length > 0 && (
              <p className="text-xs text-muted-foreground/70">
                Add-ons: {plan.amenityAddOns.map((a) => {
                  const label = AMENITIES.find((x) => x.value === a.amenity)?.label ?? a.amenity;
                  const parts = [];
                  if (a.transientAmount != null) parts.push(`T:+$${a.transientAmount}`);
                  if (a.leaseAmount != null) parts.push(`L:+$${a.leaseAmount}`);
                  return `${label} (${parts.join(', ')})`;
                }).join(' · ')}
              </p>
            )}
          </div>
          <div className="flex gap-2 shrink-0 flex-wrap justify-end">
            {!plan.isDefault && (
              <button type="button" onClick={onSetDefault} className={btnSecondary}>
                Set default
              </button>
            )}
            <button type="button" onClick={() => setBulkTarget(plan.id)} className={btnSecondary}>
              Bulk assign
            </button>
            <button type="button" onClick={onEdit} className={btnSecondary}>
              Edit
            </button>
            <button
              type="button"
              onClick={onDelete}
              disabled={plan.isDefault}
              title={plan.isDefault ? 'Cannot delete the default plan' : 'Delete plan'}
              className={plan.isDefault ? 'rounded-lg border border-border px-4 py-2 text-sm text-muted-foreground/40 cursor-not-allowed' : btnDanger}
            >
              Delete
            </button>
          </div>
        </div>
      </div>

      {bulkTarget && (
        <BulkAssignDialog
          marinaId={marinaId}
          plans={plans}
          targetPlanId={bulkTarget}
          onClose={() => setBulkTarget(null)}
        />
      )}
    </>
  );
}

// ─── Page shell ───────────────────────────────────────────────────────────────

export function PricingPlansPage() {
  const { marinaId = '' } = useParams({ strict: false });

  const [plans, setPlans] = useState<PricingPlanDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingPlan, setEditingPlan] = useState<PricingPlanDto | null | 'new'>('new' as const);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const isCreating = editingPlan === 'new';
  const isEditing = editingPlan !== null && editingPlan !== 'new';

  function reload() {
    return getPricingPlans(marinaId).then(setPlans);
  }

  useEffect(() => {
    reload().catch(() => setError('Could not load pricing plans.'))
      .finally(() => setLoading(false));
  }, [marinaId]);

  useEffect(() => {
    if (plans.length > 0) setEditingPlan(null);
  }, [plans.length]);

  async function handleSave(data: PricingPlanUpsertData) {
    setSaving(true);
    setFormError(null);
    try {
      if (isEditing) {
        await updatePricingPlan(marinaId, (editingPlan as PricingPlanDto).id, data);
      } else {
        await createPricingPlan(marinaId, data);
      }
      await reload();
      setEditingPlan(null);
    } catch {
      setFormError('Could not save plan. Please try again.');
    } finally {
      setSaving(false);
    }
  }

  async function handleSetDefault(planId: string) {
    try {
      await setDefaultPricingPlan(marinaId, planId);
      await reload();
    } catch {
      alert('Could not set default plan.');
    }
  }

  async function handleDelete(plan: PricingPlanDto) {
    if (!window.confirm(`Delete "${plan.name}"? Slips using this plan will fall back to the default plan.`)) return;
    try {
      await deletePricingPlan(marinaId, plan.id);
      await reload();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : '';
      alert(msg.includes('409') || msg.includes('Conflict')
        ? 'Cannot delete the default plan. Set another plan as default first.'
        : 'Could not delete plan.');
    }
  }

  return (
    <div className="min-h-screen bg-background">
      <NavBar />
      <div className="max-w-3xl mx-auto py-10 px-4 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <a href={`/marina/${marinaId}`} className="text-xs text-muted-foreground hover:text-foreground underline">
              ← Back to dashboard
            </a>
            <h1 className="text-2xl font-semibold text-foreground mt-1">Pricing plans</h1>
            <p className="text-sm text-muted-foreground mt-0.5">
              Define rate bundles for your slips. The default plan applies to any slip without an explicit assignment.
            </p>
          </div>
          {!isCreating && (
            <button
              type="button"
              onClick={() => setEditingPlan('new')}
              className={btn}
            >
              + New plan
            </button>
          )}
        </div>

        {loading && <p className="text-sm text-muted-foreground/70">Loading…</p>}
        {error && <p className="text-sm text-red-600">{error}</p>}

        {/* Create / Edit form */}
        {editingPlan !== null && (
          <div className="bg-card rounded-2xl border border-border shadow-sm p-6">
            <h2 className="text-base font-semibold text-foreground mb-4">
              {isCreating ? 'Create plan' : `Edit "${(editingPlan as PricingPlanDto).name}"`}
            </h2>
            {formError && <p className="text-sm text-red-600 mb-4">{formError}</p>}
            <PlanForm
              initial={isEditing ? (editingPlan as PricingPlanDto) : undefined}
              onSave={handleSave}
              onCancel={() => setEditingPlan(null)}
              saving={saving}
            />
          </div>
        )}

        {/* Plan list */}
        {!loading && plans.length === 0 && editingPlan === null && (
          <div className="rounded-2xl border border-dashed border-border p-10 text-center">
            <p className="text-sm text-muted-foreground">No pricing plans yet.</p>
            <button type="button" onClick={() => setEditingPlan('new')} className="mt-3 text-sm text-primary hover:underline">
              Create your first plan →
            </button>
          </div>
        )}

        <div className="space-y-3">
          {plans.map((plan) => (
            <PlanCard
              key={plan.id}
              plan={plan}
              marinaId={marinaId}
              plans={plans}
              onSetDefault={() => handleSetDefault(plan.id)}
              onEdit={() => setEditingPlan(plan)}
              onDelete={() => handleDelete(plan)}
            />
          ))}
        </div>
      </div>
    </div>
  );
}
