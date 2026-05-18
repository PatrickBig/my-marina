import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  getMarina, updateMarina, getDocks, createDock, updateDock, deleteDock,
  getSlips, createSlip, updateSlip, deleteSlip, getMarinaStaff, inviteStaff, revokeStaff,
  getBillingAccounts, createBillingAccount, updateBillingAccount,
  getBillingAccountMembers, inviteAccountMember, removeAccountMember,
  getVesselRecords, createVesselRecord, getSlipAssignments, createSlipAssignment, endSlipAssignment,
  getAvailabilityWindows, createAvailabilityWindow, setAvailabilityWindowStatus,
  getMarinaReservations, approveReservation, declineReservation, markNoShow,
  getMarinaAbsences,
  getMarinaInvoices, createInvoice, sendInvoice, voidInvoice, recordPayment,
  getMarinaMaintenanceRequests, updateMaintenanceRequestStatus, getMarinaWorkOrders, createWorkOrder, updateWorkOrder,
  getMarinaAnnouncements, createAnnouncement, publishAnnouncement, deleteAnnouncement,
  getMarinaLeaseInquiries, updateLeaseInquiry, approveLeaseInquiry, declineLeaseInquiry,
  updateSlipAssignment,
  type MarinaDto, type DockDto, type SlipDto, type MembershipDto, type SlipType,
  type BillingAccountDto, type BillingAccountMemberDto, type VesselRecordDto, type SlipAssignmentDto, type AssignmentType,
  type AvailabilityWindowDto, type AvailabilityWindowStatus, type ReservationDto,
  type OwnerAbsenceDto, type InvoiceSummaryDto,
  type MaintenanceRequestDto, type WorkOrderDto, type AnnouncementDto,
  type LeaseInquiryDto, type RateKind, type LeaseTerm,
} from '@/api/api';
import { NavBar } from '@/components/NavBar';
import { usePhotoUpload, type MarinaPhotoDto } from '@/hooks/usePhotoUpload';
import { CropUploadModal } from '@/components/CropUploadModal';
import { PhotoCard } from '@/components/PhotoCard';

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
  latitude: z.preprocess((v) => (v === '' || v == null ? undefined : Number(v)), z.number().min(-90).max(90).optional()),
  longitude: z.preprocess((v) => (v === '' || v == null ? undefined : Number(v)), z.number().min(-180).max(180).optional()),
  phoneNumber: z.string().optional(),
  email: z.string().email('Invalid email').optional().or(z.literal('')),
  website: z.string().optional(),
  description: z.string().optional(),
});
type InfoFormData = z.infer<typeof infoSchema>;

async function geocodeAddress(parts: { street?: string; city?: string; state?: string; zip?: string; country?: string }): Promise<{ lat: number; lon: number } | null> {
  const q = [parts.street, parts.city, parts.state, parts.zip, parts.country || 'US'].filter(Boolean).join(', ');
  try {
    const res = await fetch(`https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(q)}&format=json&limit=1`, { headers: { 'Accept-Language': 'en' } });
    const data = await res.json();
    if (!data.length) return null;
    return { lat: parseFloat(data[0].lat), lon: parseFloat(data[0].lon) };
  } catch { return null; }
}

function MarinaInfoPanel({ marina, onSaved }: { marina: MarinaDto; onSaved: (m: MarinaDto) => void }) {
  const [editing, setEditing] = useState(false);
  const [geocoding, setGeocoding] = useState(false);
  const [geocodeError, setGeocodeError] = useState<string | null>(null);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const { register, handleSubmit, reset, setValue, getValues, formState: { errors, isSubmitting } } = useForm<InfoFormData>({
    resolver: zodResolver(infoSchema) as any,
    defaultValues: {
      name: marina.name,
      addressStreet: marina.addressStreet ?? '',
      addressCity: marina.addressCity ?? '',
      addressState: marina.addressState ?? '',
      addressZip: marina.addressZip ?? '',
      addressCountry: marina.addressCountry ?? '',
      latitude: marina.latitude ?? undefined,
      longitude: marina.longitude ?? undefined,
      phoneNumber: marina.phoneNumber ?? '',
      email: marina.email ?? '',
      website: marina.website ?? '',
      description: marina.description ?? '',
    },
  });

  async function handleGeocode() {
    setGeocodeError(null);
    setGeocoding(true);
    const vals = getValues();
    const coords = await geocodeAddress({
      street: vals.addressStreet, city: vals.addressCity,
      state: vals.addressState, zip: vals.addressZip, country: vals.addressCountry,
    });
    setGeocoding(false);
    if (!coords) {
      setGeocodeError('Address not found. Try adding more detail (street, city, state).');
      return;
    }
    setValue('latitude', coords.lat as unknown as undefined);
    setValue('longitude', coords.lon as unknown as undefined);
  }

  async function onSubmit(data: InfoFormData) {
    const updated = await updateMarina(marina.id, {
      name: data.name,
      addressStreet: data.addressStreet || null,
      addressCity: data.addressCity || null,
      addressState: data.addressState || null,
      addressZip: data.addressZip || null,
      addressCountry: data.addressCountry || null,
      latitude: data.latitude ?? null,
      longitude: data.longitude ?? null,
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
      <div className="bg-card rounded-xl border border-border p-6">
        <div className="flex justify-between items-start">
          <div>
            <h2 className="text-lg font-semibold text-foreground">{marina.name}</h2>
            <p className="text-sm text-muted-foreground mt-0.5">{marina.marinaType} · {marina.timeZoneId}</p>
            {marina.addressCity && (
              <p className="text-sm text-muted-foreground mt-1">
                {[marina.addressStreet, marina.addressCity, marina.addressState, marina.addressZip].filter(Boolean).join(', ')}
              </p>
            )}
            {marina.latitude != null && marina.longitude != null ? (
              <p className="text-sm text-muted-foreground mt-1">
                📍 {marina.latitude.toFixed(5)}, {marina.longitude.toFixed(5)}
              </p>
            ) : (
              <p className="text-sm text-amber-600 mt-1">⚠ No location set — slips won't appear in search</p>
            )}
            {marina.description && <p className="text-sm text-foreground/80 mt-2">{marina.description}</p>}
          </div>
          <button onClick={() => setEditing(true)} className="text-sm text-muted-foreground hover:text-foreground underline">
            Edit
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-card rounded-xl border border-border p-6">
      <h2 className="text-base font-semibold text-foreground mb-4">Edit marina info</h2>
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
          <Field label="Latitude" error={errors.latitude?.message}>
            <input {...register('latitude')} type="number" step="0.00001" placeholder="e.g. 25.77427" className={input} />
          </Field>
          <Field label="Longitude" error={errors.longitude?.message}>
            <input {...register('longitude')} type="number" step="0.00001" placeholder="e.g. -80.19366" className={input} />
          </Field>
        </div>
        <div className="flex items-center gap-3 -mt-1">
          <button
            type="button"
            onClick={handleGeocode}
            disabled={geocoding}
            className="text-xs text-blue-600 hover:text-blue-800 underline disabled:opacity-50"
          >
            {geocoding ? 'Looking up…' : '📍 Auto-fill coordinates from address above'}
          </button>
          {geocodeError && <span className="text-xs text-red-600">{geocodeError}</span>}
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

type DockFormValues = { name: string; description: string; sortOrder: string };

function DockForm({ defaultValues, onSubmit, onCancel, submitLabel }: {
  defaultValues?: Partial<DockFormValues>;
  onSubmit: (v: DockFormValues) => Promise<void>;
  onCancel: () => void;
  submitLabel: string;
}) {
  const [name, setName] = useState(defaultValues?.name ?? '');
  const [description, setDescription] = useState(defaultValues?.description ?? '');
  const [sortOrder, setSortOrder] = useState(defaultValues?.sortOrder ?? '0');
  const [saving, setSaving] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) return;
    setSaving(true);
    try { await onSubmit({ name: name.trim(), description, sortOrder }); }
    finally { setSaving(false); }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-2">
      <div className="grid grid-cols-2 gap-2">
        <Field label="Dock name">
          <input value={name} onChange={(e) => setName(e.target.value)}
            placeholder="Dock A" className={input} required autoFocus />
        </Field>
        <Field label="Sort order">
          <input type="number" value={sortOrder} onChange={(e) => setSortOrder(e.target.value)}
            className={input} />
        </Field>
      </div>
      <Field label="Description (optional)">
        <input value={description} onChange={(e) => setDescription(e.target.value)}
          placeholder="e.g. North dock, fuel dock, transient docks…" className={input} />
      </Field>
      <div className="flex gap-2 pt-1">
        <button type="submit" disabled={saving} className={btn}>{saving ? '…' : submitLabel}</button>
        <button type="button" onClick={onCancel} className={btnSecondary}>Cancel</button>
      </div>
    </form>
  );
}

function DocksPanel({ marinaId }: { marinaId: string }) {
  const [docks, setDocks] = useState<DockDto[]>([]);
  const [showCreate, setShowCreate] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  useEffect(() => { getDocks(marinaId).then(setDocks); }, [marinaId]);

  async function handleCreate(v: DockFormValues) {
    const dock = await createDock(marinaId, {
      name: v.name,
      description: v.description || null,
      sortOrder: Number(v.sortOrder) || docks.length,
    });
    setDocks((d) => [...d, dock]);
    setShowCreate(false);
  }

  async function handleEdit(dockId: string, v: DockFormValues) {
    const updated = await updateDock(marinaId, dockId, {
      name: v.name,
      description: v.description || null,
      sortOrder: Number(v.sortOrder),
    });
    setDocks((d) => d.map((x) => x.id === dockId ? updated : x));
    setEditingId(null);
  }

  async function handleDelete(dockId: string) {
    if (!window.confirm('Delete this dock? Slips assigned to it will become free-standing.')) return;
    await deleteDock(marinaId, dockId);
    setDocks((d) => d.filter((x) => x.id !== dockId));
  }

  return (
    <div className="bg-card rounded-xl border border-border p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-base font-semibold text-foreground">Docks</h2>
        <button onClick={() => setShowCreate(true)} className="text-sm text-muted-foreground hover:text-foreground underline">
          + Add dock
        </button>
      </div>

      {docks.length === 0 && !showCreate && (
        <p className="text-sm text-muted-foreground/70">No docks yet. Slips can also be free-standing.</p>
      )}

      <ul className="divide-y divide-border/50">
        {docks.map((dock) => (
          <li key={dock.id} className="py-3">
            {editingId === dock.id ? (
              <DockForm
                defaultValues={{
                  name: dock.name,
                  description: dock.description ?? '',
                  sortOrder: String(dock.sortOrder),
                }}
                onSubmit={(v) => handleEdit(dock.id, v)}
                onCancel={() => setEditingId(null)}
                submitLabel="Save"
              />
            ) : (
              <div className="flex justify-between items-center">
                <div>
                  <span className="text-sm font-medium text-foreground">{dock.name}</span>
                  {dock.description && (
                    <span className="ml-2 text-xs text-muted-foreground/70">{dock.description}</span>
                  )}
                  <span className="ml-2 text-xs text-muted-foreground/50">#{dock.sortOrder}</span>
                </div>
                <div className="flex gap-3">
                  <button onClick={() => setEditingId(dock.id)} className="text-xs text-muted-foreground/70 hover:text-foreground">Edit</button>
                  <button onClick={() => handleDelete(dock.id)} className="text-xs text-muted-foreground/70 hover:text-red-500">Remove</button>
                </div>
              </div>
            )}
          </li>
        ))}
      </ul>

      {showCreate && (
        <div className="mt-4 border-t border-border/50 pt-4">
          <p className="text-xs font-medium text-muted-foreground mb-2">New dock</p>
          <DockForm
            defaultValues={{ sortOrder: String(docks.length) }}
            onSubmit={handleCreate}
            onCancel={() => setShowCreate(false)}
            submitLabel="Add dock"
          />
        </div>
      )}
    </div>
  );
}

// ─── Slips panel ─────────────────────────────────────────────────────────────

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
  // Pricing defaults
  defaultTransientRateKind: z.string().optional(),
  defaultTransientBaseRate: z.preprocess((v) => v === '' || v == null ? undefined : Number(v), z.number().nonnegative().optional()),
  defaultTransientMinCharge: z.preprocess((v) => v === '' || v == null ? undefined : Number(v), z.number().nonnegative().optional()),
  clearTransientRate: z.boolean().optional(),
  defaultLeaseRateKind: z.string().optional(),
  defaultLeaseBaseRate: z.preprocess((v) => v === '' || v == null ? undefined : Number(v), z.number().nonnegative().optional()),
  defaultLeaseTerm: z.string().optional(),
  clearLeaseRate: z.boolean().optional(),
});
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type SlipFormData = z.infer<typeof slipSchema>;

const SLIP_TYPE_LABELS: Record<SlipType, string> = {
  Floating: 'Floating', Fixed: 'Fixed', Mooring: 'Mooring', DryStorage: 'Dry Storage', Anchorage: 'Anchorage',
};

const SLIP_STATUS_LABELS: Record<string, string> = {
  Active: 'Active', UnderMaintenance: 'Under Maintenance', Inactive: 'Inactive',
};

function SlipForm({
  docks, defaultValues, onSubmit, onCancel, submitLabel, showStatus = false,
}: {
  docks: DockDto[];
  defaultValues?: Partial<SlipFormData>;
  onSubmit: (data: SlipFormData) => Promise<void>;
  onCancel: () => void;
  submitLabel: string;
  showStatus?: boolean;
}) {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<SlipFormData>({
    resolver: zodResolver(slipSchema) as any,
    defaultValues: { slipType: 'Floating', hasElectric: false, hasWater: true, status: 'Active', ...defaultValues },
  });
  const hasElectric = watch('hasElectric');
  const clearTransient = watch('clearTransientRate');
  const clearLease = watch('clearLeaseRate');
  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
      <div className="grid grid-cols-2 gap-3">
        <Field label="Slip name / number" error={errors.name?.message}>
          <input {...register('name')} placeholder="A-1" className={input} />
        </Field>
        <Field label="Dock (optional)">
          <select {...register('dockId')} className={`${input} bg-card`}>
            <option value="">— Free-standing —</option>
            {docks.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
          </select>
        </Field>
        <Field label="Slip type">
          <select {...register('slipType')} className={`${input} bg-card`}>
            {(Object.keys(SLIP_TYPE_LABELS) as SlipType[]).map((t) => (
              <option key={t} value={t}>{SLIP_TYPE_LABELS[t]}</option>
            ))}
          </select>
        </Field>
        {showStatus && (
          <Field label="Status">
            <select {...register('status')} className={`${input} bg-card`}>
              {Object.entries(SLIP_STATUS_LABELS).map(([v, label]) => (
                <option key={v} value={v}>{label}</option>
              ))}
            </select>
          </Field>
        )}
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
      <div className="flex flex-wrap gap-4 items-center text-sm">
        <label className="flex items-center gap-2 text-foreground/80">
          <input {...register('hasWater')} type="checkbox" /> Water hookup
        </label>
        <label className="flex items-center gap-2 text-foreground/80">
          <input {...register('hasElectric')} type="checkbox" /> Electric hookup
        </label>
        {hasElectric && (
          <Field label="Amperage">
            <select {...register('electric')} className={`${input} bg-card`}>
              <option value="">— Select —</option>
              <option value="30">30A</option>
              <option value="50">50A</option>
              <option value="100">100A</option>
            </select>
          </Field>
        )}
      </div>
      <Field label="Notes">
        <textarea {...register('notes')} rows={2} className={`${input} resize-none`} />
      </Field>

      {/* Transient pricing */}
      <div className="border border-border/50 rounded-lg p-3 space-y-2">
        <div className="flex items-center justify-between">
          <p className="text-xs font-medium text-foreground/80">Transient default rate</p>
          <label className="flex items-center gap-1 text-xs text-muted-foreground">
            <input {...register('clearTransientRate')} type="checkbox" />
            Clear rate
          </label>
        </div>
        {!clearTransient && (
          <div className="grid grid-cols-3 gap-2">
            <Field label="Rate kind">
              <select {...register('defaultTransientRateKind')} className={`${input} bg-card`}>
                <option value="">— none —</option>
                <option value="Flat">Flat ($/night)</option>
                <option value="PerFoot">Per foot ($/ft/night)</option>
              </select>
            </Field>
            <Field label="Base rate ($)">
              <input {...register('defaultTransientBaseRate')} type="number" step="0.01" className={input} />
            </Field>
            <Field label="Min charge ($)">
              <input {...register('defaultTransientMinCharge')} type="number" step="0.01" className={input} />
            </Field>
          </div>
        )}
      </div>

      {/* Lease pricing */}
      <div className="border border-border/50 rounded-lg p-3 space-y-2">
        <div className="flex items-center justify-between">
          <p className="text-xs font-medium text-foreground/80">Lease default rate</p>
          <label className="flex items-center gap-1 text-xs text-muted-foreground">
            <input {...register('clearLeaseRate')} type="checkbox" />
            Clear rate
          </label>
        </div>
        {!clearLease && (
          <div className="grid grid-cols-3 gap-2">
            <Field label="Rate kind">
              <select {...register('defaultLeaseRateKind')} className={`${input} bg-card`}>
                <option value="">— none —</option>
                <option value="Flat">Flat</option>
                <option value="PerFoot">Per foot</option>
              </select>
            </Field>
            <Field label="Base rate ($)">
              <input {...register('defaultLeaseBaseRate')} type="number" step="0.01" className={input} />
            </Field>
            <Field label="Default term">
              <select {...register('defaultLeaseTerm')} className={`${input} bg-card`}>
                <option value="">— any —</option>
                <option value="Monthly">Monthly</option>
                <option value="Seasonal">Seasonal</option>
                <option value="Annual">Annual</option>
              </select>
            </Field>
          </div>
        )}
      </div>

      <div className="flex gap-2">
        <button type="submit" disabled={isSubmitting} className={btn}>
          {isSubmitting ? '…' : submitLabel}
        </button>
        <button type="button" onClick={onCancel} className={btnSecondary}>Cancel</button>
      </div>
    </form>
  );
}

function SlipsPanel({ marinaId }: { marinaId: string }) {
  const [slips, setSlips] = useState<SlipDto[]>([]);
  const [docks, setDocks] = useState<DockDto[]>([]);
  const [showCreate, setShowCreate] = useState(false);
  const [editingSlipId, setEditingSlipId] = useState<string | null>(null);

  useEffect(() => {
    getSlips(marinaId).then(setSlips);
    getDocks(marinaId).then(setDocks);
  }, [marinaId]);

  async function handleCreate(data: SlipFormData) {
    let slip = await createSlip(marinaId, {
      name: data.name, dockId: data.dockId || null, slipType: data.slipType,
      maxLength: data.maxLength, maxBeam: data.maxBeam, maxDraft: data.maxDraft,
      hasElectric: data.hasElectric, electric: data.electric || null,
      hasWater: data.hasWater, notes: data.notes || null,
    });
    if (data.defaultTransientRateKind || data.defaultLeaseRateKind) {
      slip = await updateSlip(marinaId, slip.id, {
        defaultTransientRateKind: (data.defaultTransientRateKind as RateKind) || null,
        defaultTransientBaseRate: data.defaultTransientBaseRate ?? null,
        defaultTransientMinCharge: data.defaultTransientMinCharge ?? null,
        clearTransientRate: false,
        defaultLeaseRateKind: (data.defaultLeaseRateKind as RateKind) || null,
        defaultLeaseBaseRate: data.defaultLeaseBaseRate ?? null,
        defaultLeaseTerm: (data.defaultLeaseTerm as LeaseTerm) || null,
        clearLeaseRate: false,
      });
    }
    setSlips((s) => [...s, slip]);
    setShowCreate(false);
  }

  async function handleEdit(slipId: string, data: SlipFormData) {
    const updated = await updateSlip(marinaId, slipId, {
      name: data.name, dockId: data.dockId || null, slipType: data.slipType,
      status: data.status,
      maxLength: data.maxLength, maxBeam: data.maxBeam, maxDraft: data.maxDraft,
      hasElectric: data.hasElectric, electric: data.electric || null,
      hasWater: data.hasWater, notes: data.notes || null,
      defaultTransientRateKind: (data.defaultTransientRateKind as RateKind) || null,
      defaultTransientBaseRate: data.defaultTransientBaseRate ?? null,
      defaultTransientMinCharge: data.defaultTransientMinCharge ?? null,
      clearTransientRate: !!data.clearTransientRate,
      defaultLeaseRateKind: (data.defaultLeaseRateKind as RateKind) || null,
      defaultLeaseBaseRate: data.defaultLeaseBaseRate ?? null,
      defaultLeaseTerm: (data.defaultLeaseTerm as LeaseTerm) || null,
      clearLeaseRate: !!data.clearLeaseRate,
    });
    setSlips((s) => s.map((x) => x.id === slipId ? updated : x));
    setEditingSlipId(null);
  }

  async function handleDelete(slipId: string) {
    if (!window.confirm('Delete this slip?')) return;
    await deleteSlip(marinaId, slipId);
    setSlips((s) => s.filter((x) => x.id !== slipId));
  }

  return (
    <div className="bg-card rounded-xl border border-border p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-base font-semibold text-foreground">Slips</h2>
        <button onClick={() => setShowCreate(true)} className="text-sm text-muted-foreground hover:text-foreground underline">
          + Add slip
        </button>
      </div>

      {slips.length === 0 && !showCreate && (
        <p className="text-sm text-muted-foreground/70">No slips yet.</p>
      )}

      <ul className="divide-y divide-border/50">
        {slips.map((slip) => (
          <li key={slip.id} className="py-3">
            {editingSlipId === slip.id ? (
              <div>
                <p className="text-xs font-medium text-muted-foreground mb-2">Editing {slip.name}</p>
                <SlipForm
                  docks={docks}
                  showStatus
                  defaultValues={{
                    name: slip.name,
                    dockId: slip.dockId ?? '',
                    slipType: slip.slipType as SlipType,
                    status: slip.status as 'Active' | 'UnderMaintenance' | 'Inactive',
                    maxLength: slip.maxLength,
                    maxBeam: slip.maxBeam,
                    maxDraft: slip.maxDraft,
                    hasElectric: slip.hasElectric,
                    electric: slip.electric ?? undefined,
                    hasWater: slip.hasWater,
                    notes: slip.notes ?? '',
                    defaultTransientRateKind: slip.defaultTransientRateKind ?? '',
                    defaultTransientBaseRate: slip.defaultTransientBaseRate ?? undefined,
                    defaultTransientMinCharge: slip.defaultTransientMinCharge ?? undefined,
                    clearTransientRate: false,
                    defaultLeaseRateKind: slip.defaultLeaseRateKind ?? '',
                    defaultLeaseBaseRate: slip.defaultLeaseBaseRate ?? undefined,
                    defaultLeaseTerm: slip.defaultLeaseTerm ?? '',
                    clearLeaseRate: false,
                  }}
                  onSubmit={(data) => handleEdit(slip.id, data)}
                  onCancel={() => setEditingSlipId(null)}
                  submitLabel="Save"
                />
              </div>
            ) : (
              <div className="flex justify-between items-center">
                <div>
                  <span className="text-sm font-medium text-foreground">{slip.name}</span>
                  {slip.dockId && (
                    <span className="ml-2 text-xs text-muted-foreground/70">
                      {docks.find((d) => d.id === slip.dockId)?.name ?? 'Dock'}
                    </span>
                  )}
                  <span className="ml-2 text-xs text-muted-foreground/70">{slip.slipType} · {slip.maxLength}′ L · {slip.maxBeam}′ B · {slip.maxDraft}′ D</span>
                  {slip.hasWater && <span className="ml-1 text-xs text-muted-foreground/70">· Water</span>}
                  {slip.hasElectric && <span className="ml-1 text-xs text-muted-foreground/70">· Electric {slip.electric}A</span>}
                  {slip.defaultTransientBaseRate != null && (
                    <span className="ml-1 text-xs text-blue-500">
                      · T: ${slip.defaultTransientBaseRate}{slip.defaultTransientRateKind === 'PerFoot' ? '/ft' : ''}/night
                    </span>
                  )}
                  {slip.defaultLeaseBaseRate != null && (
                    <span className="ml-1 text-xs text-purple-500">
                      · L: ${slip.defaultLeaseBaseRate}{slip.defaultLeaseRateKind === 'PerFoot' ? '/ft' : ''}/{slip.defaultLeaseTerm ?? 'lease'}
                    </span>
                  )}
                </div>
                <div className="flex gap-3 shrink-0 ml-3">
                  <button onClick={() => setEditingSlipId(slip.id)} className="text-xs text-muted-foreground/70 hover:text-foreground">
                    Edit
                  </button>
                  <button onClick={() => handleDelete(slip.id)} className="text-xs text-muted-foreground/70 hover:text-red-500">
                    Remove
                  </button>
                </div>
              </div>
            )}
          </li>
        ))}
      </ul>

      {showCreate && (
        <div className="mt-4 border-t border-border/50 pt-4">
          <h3 className="text-sm font-medium text-foreground mb-3">New slip</h3>
          <SlipForm
            docks={docks}
            onSubmit={handleCreate}
            onCancel={() => setShowCreate(false)}
            submitLabel="Add slip"
          />
        </div>
      )}
    </div>
  );
}

// ─── Billing Accounts panel ──────────────────────────────────────────────────

const accountSchema = z.object({
  displayName: z.string().min(1, 'Required'),
  billingEmail: z.string().email('Invalid email'),
  billingPhone: z.string().optional(),
  emergencyContactName: z.string().optional(),
  emergencyContactPhone: z.string().optional(),
});
type AccountFormData = z.infer<typeof accountSchema>;

function BillingAccountsPanel({ marinaId }: { marinaId: string }) {
  const [accounts, setAccounts] = useState<BillingAccountDto[]>([]);
  const [expanded, setExpanded] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<AccountFormData>({
    resolver: zodResolver(accountSchema) as any,
  });

  useEffect(() => { getBillingAccounts(marinaId).then(setAccounts); }, [marinaId]);

  async function onCreate(data: AccountFormData) {
    const acct = await createBillingAccount(marinaId, {
      displayName: data.displayName,
      billingEmail: data.billingEmail,
      billingPhone: data.billingPhone || null,
      emergencyContactName: data.emergencyContactName || null,
      emergencyContactPhone: data.emergencyContactPhone || null,
    });
    setAccounts((prev) => [...prev, acct]);
    reset();
    setShowCreate(false);
  }

  return (
    <div className="bg-card rounded-xl border border-border p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-base font-semibold text-foreground">Billing Accounts</h2>
        <button onClick={() => setShowCreate((v) => !v)} className="text-sm text-muted-foreground hover:text-foreground underline">
          {showCreate ? 'Cancel' : '+ Add account'}
        </button>
      </div>

      {showCreate && (
        <form onSubmit={handleSubmit(onCreate)} className="mb-4 p-4 bg-muted/30 rounded-lg space-y-3">
          <h3 className="text-sm font-medium text-foreground">New billing account</h3>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Account name" error={errors.displayName?.message}>
              <input {...register('displayName')} placeholder="John Smith" className={input} />
            </Field>
            <Field label="Billing email" error={errors.billingEmail?.message}>
              <input {...register('billingEmail')} type="email" className={input} />
            </Field>
            <Field label="Phone">
              <input {...register('billingPhone')} className={input} />
            </Field>
            <Field label="Emergency contact name">
              <input {...register('emergencyContactName')} className={input} />
            </Field>
            <Field label="Emergency contact phone">
              <input {...register('emergencyContactPhone')} className={input} />
            </Field>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={isSubmitting} className={btn}>
              {isSubmitting ? 'Creating…' : 'Create account'}
            </button>
            <button type="button" onClick={() => { setShowCreate(false); reset(); }} className={btnSecondary}>Cancel</button>
          </div>
        </form>
      )}

      {accounts.length === 0 && !showCreate && (
        <p className="text-sm text-muted-foreground/70">No billing accounts yet.</p>
      )}

      <ul className="divide-y divide-border/50">
        {accounts.map((acct) => (
          <li key={acct.id}>
            <div
              className="py-3 flex justify-between items-center cursor-pointer hover:bg-muted/30 -mx-2 px-2 rounded"
              onClick={() => setExpanded((v) => (v === acct.id ? null : acct.id))}
            >
              <div>
                <span className="text-sm font-medium text-foreground">{acct.displayName}</span>
                <span className="ml-2 text-xs text-muted-foreground/70">{acct.billingEmail}</span>
                {!acct.isActive && (
                  <span className="ml-2 text-xs text-red-500">Inactive</span>
                )}
              </div>
              <span className="text-xs text-muted-foreground/70">{expanded === acct.id ? '▲' : '▼'}</span>
            </div>
            {expanded === acct.id && (
              <BillingAccountDetail marinaId={marinaId} account={acct} onUpdated={(updated) =>
                setAccounts((prev) => prev.map((a) => a.id === updated.id ? updated : a))
              } />
            )}
          </li>
        ))}
      </ul>
    </div>
  );
}

function BillingAccountDetail({
  marinaId, account, onUpdated,
}: {
  marinaId: string;
  account: BillingAccountDto;
  onUpdated: (a: BillingAccountDto) => void;
}) {
  const [members, setMembers] = useState<BillingAccountMemberDto[]>([]);
  const [vessels, setVessels] = useState<VesselRecordDto[]>([]);
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviting, setInviting] = useState(false);
  const [editing, setEditing] = useState(false);
  const [showAddVessel, setShowAddVessel] = useState(false);
  const [vesselName, setVesselName] = useState('');
  const [vesselMake, setVesselMake] = useState('');
  const [vesselLength, setVesselLength] = useState('');
  const [vesselBeam, setVesselBeam] = useState('');
  const [vesselDraft, setVesselDraft] = useState('');
  const [claimEmail, setClaimEmail] = useState('');
  const [addingVessel, setAddingVessel] = useState(false);
  const [editName, setEditName] = useState(account.displayName);
  const [editEmail, setEditEmail] = useState(account.billingEmail);
  const [editPhone, setEditPhone] = useState(account.billingPhone ?? '');
  const [editEmergencyName, setEditEmergencyName] = useState(account.emergencyContactName ?? '');
  const [editEmergencyPhone, setEditEmergencyPhone] = useState(account.emergencyContactPhone ?? '');

  useEffect(() => {
    getBillingAccountMembers(marinaId, account.id).then(setMembers);
    getVesselRecords(marinaId, account.id).then(setVessels);
  }, [marinaId, account.id]);

  async function handleInvite(e: React.FormEvent) {
    e.preventDefault();
    if (!inviteEmail) return;
    setInviting(true);
    try {
      const member = await inviteAccountMember(marinaId, account.id, inviteEmail);
      setMembers((prev) => [...prev, member]);
      setInviteEmail('');
    } finally {
      setInviting(false);
    }
  }

  async function handleRemoveMember(memberId: string) {
    if (!window.confirm('Remove this member?')) return;
    await removeAccountMember(marinaId, account.id, memberId);
    setMembers((prev) => prev.filter((m) => m.id !== memberId));
  }

  async function handleSaveEdit() {
    const updated = await updateBillingAccount(marinaId, account.id, {
      displayName: editName,
      billingEmail: editEmail,
      billingPhone: editPhone || null,
      emergencyContactName: editEmergencyName || null,
      emergencyContactPhone: editEmergencyPhone || null,
    });
    onUpdated(updated);
    setEditing(false);
  }

  async function handleToggleActive() {
    const updated = await updateBillingAccount(marinaId, account.id, { isActive: !account.isActive });
    onUpdated(updated);
  }

  async function handleAddVessel(e: React.FormEvent) {
    e.preventDefault();
    if (!vesselName.trim()) return;
    setAddingVessel(true);
    try {
      const record = await createVesselRecord(marinaId, {
        billingAccountId: account.id,
        vesselName: vesselName.trim(),
        vesselMake: vesselMake || null,
        vesselLength: vesselLength ? Number(vesselLength) : null,
        vesselBeam: vesselBeam ? Number(vesselBeam) : null,
        vesselDraft: vesselDraft ? Number(vesselDraft) : null,
        claimEmail: claimEmail || null,
      });
      setVessels((v) => [...v, record]);
      setVesselName(''); setVesselMake(''); setVesselLength('');
      setVesselBeam(''); setVesselDraft(''); setClaimEmail('');
      setShowAddVessel(false);
    } finally {
      setAddingVessel(false);
    }
  }

  return (
    <div className="pb-4 pt-1 pl-2 space-y-4 text-sm">
      {/* Details */}
      {editing ? (
        <div className="grid grid-cols-2 gap-2">
          <Field label="Account name"><input value={editName} onChange={(e) => setEditName(e.target.value)} className={input} /></Field>
          <Field label="Billing email"><input type="email" value={editEmail} onChange={(e) => setEditEmail(e.target.value)} className={input} /></Field>
          <Field label="Phone"><input value={editPhone} onChange={(e) => setEditPhone(e.target.value)} className={input} /></Field>
          <Field label="Emergency contact name"><input value={editEmergencyName} onChange={(e) => setEditEmergencyName(e.target.value)} className={input} /></Field>
          <Field label="Emergency contact phone"><input value={editEmergencyPhone} onChange={(e) => setEditEmergencyPhone(e.target.value)} className={input} /></Field>
          <div className="col-span-2 flex gap-2">
            <button onClick={handleSaveEdit} className={btn}>Save</button>
            <button onClick={() => setEditing(false)} className={btnSecondary}>Cancel</button>
          </div>
        </div>
      ) : (
        <div className="flex items-start justify-between">
          <div className="space-y-0.5 text-muted-foreground">
            {account.billingPhone && <p>📞 {account.billingPhone}</p>}
            {account.emergencyContactName && (
              <p>🆘 {account.emergencyContactName}{account.emergencyContactPhone ? ` · ${account.emergencyContactPhone}` : ''}</p>
            )}
          </div>
          <div className="flex gap-2">
            <button onClick={() => setEditing(true)} className="text-xs text-muted-foreground/70 hover:text-foreground underline">Edit</button>
            <button onClick={handleToggleActive} className="text-xs text-muted-foreground/70 hover:text-foreground underline">
              {account.isActive ? 'Deactivate' : 'Reactivate'}
            </button>
          </div>
        </div>
      )}

      {/* Members */}
      <div>
        <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">Members</p>
        {members.length === 0 ? (
          <p className="text-xs text-muted-foreground/70">No platform members linked.</p>
        ) : (
          <ul className="space-y-1">
            {members.map((m) => (
              <li key={m.id} className="flex items-center justify-between">
                <span className="text-xs text-foreground/80">
                  {m.userName || m.userEmail} · {m.role}
                  {!m.acceptedAt && <span className="ml-1 text-muted-foreground/70">(pending)</span>}
                </span>
                <button onClick={() => handleRemoveMember(m.id)} className="text-xs text-red-400 hover:text-red-600">Remove</button>
              </li>
            ))}
          </ul>
        )}
        <form onSubmit={handleInvite} className="flex gap-2 mt-2">
          <input value={inviteEmail} onChange={(e) => setInviteEmail(e.target.value)}
            type="email" placeholder="Invite by email…" className={`${input} text-xs py-1.5`} />
          <button type="submit" disabled={inviting} className="rounded-lg bg-foreground text-background px-3 py-1.5 text-xs hover:opacity-90 disabled:opacity-50">
            {inviting ? '…' : 'Invite'}
          </button>
        </form>
      </div>

      {/* Vessels */}
      <div>
        <div className="flex items-center justify-between mb-1">
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Vessels</p>
          <button onClick={() => setShowAddVessel((v) => !v)} className="text-xs text-muted-foreground/70 hover:text-foreground underline">
            {showAddVessel ? 'Cancel' : '+ Add vessel'}
          </button>
        </div>
        {vessels.length === 0 && !showAddVessel && (
          <p className="text-xs text-muted-foreground/70">No vessels on file.</p>
        )}
        <ul className="space-y-1">
          {vessels.map((v) => (
            <li key={v.id} className="text-xs text-foreground/80">
              {v.vesselName}
              {v.vesselMake && ` · ${v.vesselMake}`}
              {v.vesselLength && ` · ${v.vesselLength}′`}
              {v.vesselIsGhost && <span className="ml-1 text-muted-foreground/70">(unregistered)</span>}
              {v.insuranceExpiresOn && (
                <span className={`ml-2 ${new Date(v.insuranceExpiresOn) < new Date() ? 'text-red-500' : 'text-muted-foreground/70'}`}>
                  Ins. exp. {new Date(v.insuranceExpiresOn).toLocaleDateString()}
                </span>
              )}
            </li>
          ))}
        </ul>
        {showAddVessel && (
          <form onSubmit={handleAddVessel} className="mt-2 space-y-2 p-3 bg-muted/30 rounded-lg">
            <div className="grid grid-cols-2 gap-2">
              <div>
                <label className="block text-xs text-muted-foreground mb-0.5">Vessel name *</label>
                <input value={vesselName} onChange={(e) => setVesselName(e.target.value)}
                  placeholder="Sea Breeze" className={`${input} text-xs py-1.5`} required />
              </div>
              <div>
                <label className="block text-xs text-muted-foreground mb-0.5">Make / brand</label>
                <input value={vesselMake} onChange={(e) => setVesselMake(e.target.value)}
                  placeholder="Catalina" className={`${input} text-xs py-1.5`} />
              </div>
              <div>
                <label className="block text-xs text-muted-foreground mb-0.5">Length (ft)</label>
                <input type="number" step="0.1" value={vesselLength} onChange={(e) => setVesselLength(e.target.value)}
                  className={`${input} text-xs py-1.5`} />
              </div>
              <div>
                <label className="block text-xs text-muted-foreground mb-0.5">Beam (ft)</label>
                <input type="number" step="0.1" value={vesselBeam} onChange={(e) => setVesselBeam(e.target.value)}
                  className={`${input} text-xs py-1.5`} />
              </div>
              <div>
                <label className="block text-xs text-muted-foreground mb-0.5">Draft (ft)</label>
                <input type="number" step="0.1" value={vesselDraft} onChange={(e) => setVesselDraft(e.target.value)}
                  className={`${input} text-xs py-1.5`} />
              </div>
              <div>
                <label className="block text-xs text-muted-foreground mb-0.5">Owner email (to claim)</label>
                <input type="email" value={claimEmail} onChange={(e) => setClaimEmail(e.target.value)}
                  placeholder="Optional" className={`${input} text-xs py-1.5`} />
              </div>
            </div>
            <button type="submit" disabled={addingVessel}
              className="rounded-lg bg-foreground text-background px-3 py-1.5 text-xs hover:opacity-90 disabled:opacity-50">
              {addingVessel ? 'Adding…' : 'Add vessel'}
            </button>
          </form>
        )}
      </div>
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
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState({ endDate: '', baseRate: '', notes: '' });
  const [editSaving, setEditSaving] = useState(false);
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

  function startEdit(a: SlipAssignmentDto) {
    setEditingId(a.id);
    setEditForm({ endDate: a.endDate ?? '', baseRate: String(a.baseRate), notes: '' });
  }

  async function handleSaveEdit(id: string) {
    setEditSaving(true);
    try {
      const updated = await updateSlipAssignment(marinaId, id, {
        endDate: editForm.endDate || null,
        baseRate: editForm.baseRate ? Number(editForm.baseRate) : undefined,
      });
      setAssignments((prev) => prev.map((a) => a.id === id ? updated : a));
      setEditingId(null);
    } finally {
      setEditSaving(false);
    }
  }

  return (
    <div className="bg-card rounded-xl border border-border p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-base font-semibold text-foreground">Slip Assignments</h2>
        <button onClick={() => setShowForm(true)} className="text-sm text-muted-foreground hover:text-foreground underline">
          + New assignment
        </button>
      </div>

      {assignments.length === 0 && !showForm && (
        <p className="text-sm text-muted-foreground/70">No active assignments.</p>
      )}

      <ul className="divide-y divide-border/50">
        {assignments.map((a) => (
          <li key={a.id} className="py-3">
            {editingId === a.id ? (
              <div className="space-y-2">
                <p className="text-sm font-medium text-foreground">{a.slipName} — editing</p>
                <div className="grid grid-cols-2 gap-2">
                  <Field label="End date (leave blank for open-ended)">
                    <input type="date" value={editForm.endDate}
                      onChange={(e) => setEditForm((f) => ({ ...f, endDate: e.target.value }))}
                      className={input} />
                  </Field>
                  <Field label="Base rate ($/period)">
                    <input type="number" step="0.01" value={editForm.baseRate}
                      onChange={(e) => setEditForm((f) => ({ ...f, baseRate: e.target.value }))}
                      className={input} />
                  </Field>
                </div>
                <div className="flex gap-2">
                  <button onClick={() => handleSaveEdit(a.id)} disabled={editSaving} className={btn}>
                    {editSaving ? '…' : 'Save'}
                  </button>
                  <button onClick={() => setEditingId(null)} className={btnSecondary}>Cancel</button>
                </div>
              </div>
            ) : (
              <div className="flex justify-between items-start">
                <div>
                  <p className="text-sm font-medium text-foreground">
                    {a.slipName}
                    <span className="ml-2 text-xs text-muted-foreground/70 font-normal">{a.assignmentType}</span>
                  </p>
                  <p className="text-xs text-muted-foreground mt-0.5">
                    {a.billingAccountDisplayName} · {a.vesselName}
                  </p>
                  <p className="text-xs text-muted-foreground/70 mt-0.5">
                    {a.startDate} – {a.endDate ?? 'open-ended'} · ${a.baseRate.toLocaleString()}/period
                  </p>
                </div>
                <div className="flex gap-3 shrink-0 ml-3">
                  <button onClick={() => startEdit(a)} className="text-xs text-muted-foreground/70 hover:text-foreground">Edit</button>
                  <button onClick={() => handleEnd(a.id)} className="text-xs text-muted-foreground/70 hover:text-red-500">End</button>
                </div>
              </div>
            )}
          </li>
        ))}
      </ul>

      {showForm && (
        <form onSubmit={handleSubmit(onSubmit)} className="mt-4 space-y-3 border-t border-border/50 pt-4">
          <h3 className="text-sm font-medium text-foreground">New assignment</h3>

          <div className="grid grid-cols-2 gap-3">
            <Field label="Slip" error={errors.slipId?.message}>
              <select {...register('slipId')} className={`${input} bg-card`}>
                <option value="">— select slip —</option>
                {slips.map((s) => (
                  <option key={s.id} value={s.id}>{s.name} ({s.maxLength}′)</option>
                ))}
              </select>
            </Field>

            <Field label="Assignment type">
              <select {...register('assignmentType')} className={`${input} bg-card`}>
                {ASSIGNMENT_TYPES.map((t) => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>
            </Field>

            <Field label="Billing account" error={errors.billingAccountId?.message}>
              <select {...register('billingAccountId')} className={`${input} bg-card`}>
                <option value="">— select account —</option>
                {accounts.map((a) => (
                  <option key={a.id} value={a.id}>{a.displayName}</option>
                ))}
              </select>
            </Field>

            <Field label="Vessel" error={errors.vesselId?.message}>
              <select {...register('vesselId')} className={`${input} bg-card`} disabled={!selectedAccountId}>
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
            <label className="flex items-center gap-2 text-foreground/80">
              <input {...register('allowHolderSublet')} type="checkbox" /> Holder may sublet
            </label>
            <label className="flex items-center gap-2 text-foreground/80">
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

// ─── Listings panel ──────────────────────────────────────────────────────────

const STATUS_LABELS: Record<AvailabilityWindowStatus, string> = {
  Open: 'Open', Paused: 'Paused', FullyBooked: 'Fully booked', Closed: 'Closed',
};

const listingSchema = z.object({
  slipId:           z.string().min(1, 'Required'),
  startsAt:         z.string().min(1, 'Required'),
  endsAt:           z.string().min(1, 'Required'),
  basePricePerNight: z.coerce.number().positive('Must be > 0'),
  instantBook:      z.boolean(),
  minNights:        z.coerce.number().int().min(1).optional(),
  maxNights:        z.coerce.number().int().min(1).optional(),
  weeklyDiscount:   z.coerce.number().min(0).max(1).optional(),
  monthlyDiscount:  z.coerce.number().min(0).max(1).optional(),
  cleaningFee:      z.coerce.number().min(0).optional(),
});
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type ListingFormData = z.infer<typeof listingSchema>;

function ListingsPanel({ marinaId }: { marinaId: string }) {
  const [windows, setWindows] = useState<AvailabilityWindowDto[]>([]);
  const [slips, setSlips] = useState<SlipDto[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<ListingFormData>({
    resolver: zodResolver(listingSchema) as any,
    defaultValues: { instantBook: true },
  });

  useEffect(() => {
    Promise.all([
      getAvailabilityWindows(marinaId),
      getSlips(marinaId),
    ]).then(([w, s]) => { setWindows(w); setSlips(s); });
  }, [marinaId]);

  async function onSubmit(data: ListingFormData) {
    setError(null);
    try {
      const item = await createAvailabilityWindow(marinaId, {
        slipId:            data.slipId,
        listedByKind:      'Owner',
        listedByMarinaId:  marinaId,
        startsAt:          new Date(data.startsAt).toISOString(),
        endsAt:            new Date(data.endsAt).toISOString(),
        instantBook:       data.instantBook,
        minNights:         data.minNights ?? null,
        maxNights:         data.maxNights ?? null,
        basePricePerNight: data.basePricePerNight,
        weeklyDiscount:    data.weeklyDiscount ?? null,
        monthlyDiscount:   data.monthlyDiscount ?? null,
        cleaningFee:       data.cleaningFee ?? null,
      });
      setWindows((prev) => [item, ...prev]);
      reset();
      setShowForm(false);
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      setError(msg ?? 'Could not create listing window. Check for date overlaps.');
    }
  }

  async function handleSetStatus(id: string, status: AvailabilityWindowStatus) {
    const updated = await setAvailabilityWindowStatus(marinaId, id, status);
    setWindows((prev) => prev.map((w) => w.id === id ? updated : w));
  }

  return (
    <div className="bg-card rounded-xl border border-border p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-base font-semibold text-foreground">Marketplace Listings</h2>
        <button onClick={() => setShowForm(true)} className="text-sm text-muted-foreground hover:text-foreground underline">
          + New listing window
        </button>
      </div>

      {windows.length === 0 && !showForm && (
        <p className="text-sm text-muted-foreground/70">No listing windows yet. Create one to accept transient bookings.</p>
      )}

      <ul className="divide-y divide-border/50">
        {windows.map((w) => (
          <li key={w.id} className="py-3 flex justify-between items-start">
            <div>
              <p className="text-sm font-medium text-foreground">
                {w.slipName}
                <span className={`ml-2 text-xs px-1.5 py-0.5 rounded-full ${
                  w.status === 'Open' ? 'bg-emerald-50 text-emerald-700'
                  : w.status === 'Paused' ? 'bg-amber-50 text-amber-700'
                  : 'bg-muted text-muted-foreground'
                }`}>
                  {STATUS_LABELS[w.status]}
                </span>
                {w.instantBook && (
                  <span className="ml-1 text-xs px-1.5 py-0.5 rounded-full bg-blue-50 text-blue-700">Instant</span>
                )}
              </p>
              <p className="text-xs text-muted-foreground mt-0.5">
                {new Date(w.startsAt).toLocaleDateString()} – {new Date(w.endsAt).toLocaleDateString()}
                {' · '}${w.basePricePerNight.toFixed(2)}/night
                {w.cleaningFee ? ` + $${w.cleaningFee.toFixed(2)} cleaning` : ''}
              </p>
              {(w.minNights || w.maxNights) && (
                <p className="text-xs text-muted-foreground/70 mt-0.5">
                  {w.minNights ? `Min ${w.minNights}n` : ''}
                  {w.minNights && w.maxNights ? ' · ' : ''}
                  {w.maxNights ? `Max ${w.maxNights}n` : ''}
                </p>
              )}
            </div>
            <div className="flex gap-2 mt-1">
              {w.status === 'Open' && (
                <button onClick={() => handleSetStatus(w.id, 'Paused')} className="text-xs text-muted-foreground/70 hover:text-amber-600">
                  Pause
                </button>
              )}
              {w.status === 'Paused' && (
                <button onClick={() => handleSetStatus(w.id, 'Open')} className="text-xs text-muted-foreground/70 hover:text-emerald-600">
                  Reopen
                </button>
              )}
              {w.status !== 'Closed' && (
                <button onClick={() => handleSetStatus(w.id, 'Closed')} className="text-xs text-muted-foreground/70 hover:text-red-500">
                  Close
                </button>
              )}
            </div>
          </li>
        ))}
      </ul>

      {showForm && (
        <form onSubmit={handleSubmit(onSubmit)} className="mt-4 space-y-3 border-t border-border/50 pt-4">
          <h3 className="text-sm font-medium text-foreground">New listing window</h3>

          <Field label="Slip" error={errors.slipId?.message}>
            <select {...register('slipId')} className={`${input} bg-card`}>
              <option value="">— select slip —</option>
              {slips.map((s) => (
                <option key={s.id} value={s.id}>{s.name} ({s.maxLength}′)</option>
              ))}
            </select>
          </Field>

          <div className="grid grid-cols-2 gap-3">
            <Field label="Available from" error={errors.startsAt?.message}>
              <input {...register('startsAt')} type="date" className={input} />
            </Field>
            <Field label="Available until" error={errors.endsAt?.message}>
              <input {...register('endsAt')} type="date" className={input} />
            </Field>
            <Field label="Price per night ($)" error={errors.basePricePerNight?.message}>
              <input {...register('basePricePerNight')} type="number" step="0.01" min="0.01" className={input} />
            </Field>
            <Field label="Cleaning fee ($, optional)">
              <input {...register('cleaningFee')} type="number" step="0.01" min="0" className={input} />
            </Field>
            <Field label="Min nights (optional)">
              <input {...register('minNights')} type="number" step="1" min="1" className={input} />
            </Field>
            <Field label="Max nights (optional)">
              <input {...register('maxNights')} type="number" step="1" min="1" className={input} />
            </Field>
            <Field label="Weekly discount (0–1, e.g. 0.10)">
              <input {...register('weeklyDiscount')} type="number" step="0.01" min="0" max="1" className={input} />
            </Field>
            <Field label="Monthly discount (0–1, e.g. 0.15)">
              <input {...register('monthlyDiscount')} type="number" step="0.01" min="0" max="1" className={input} />
            </Field>
          </div>

          <label className="flex items-center gap-2 text-sm text-foreground/80">
            <input {...register('instantBook')} type="checkbox" />
            Instant book (no approval required)
          </label>

          {error && <p className="text-sm text-red-600">{error}</p>}

          <div className="flex gap-2">
            <button type="submit" disabled={isSubmitting} className={btn}>
              {isSubmitting ? 'Creating…' : 'Create listing'}
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

// ─── Sublet panel ────────────────────────────────────────────────────────────

function SubletPanel({ marinaId }: { marinaId: string }) {
  const [absences, setAbsences] = useState<OwnerAbsenceDto[]>([]);
  const [assignments, setAssignments] = useState<SlipAssignmentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showFormFor, setShowFormFor] = useState<string | null>(null); // absenceId
  const [formState, setFormState] = useState({
    startsAt: '', endsAt: '', basePricePerNight: '', cleaningFee: '', instantBook: true,
  });
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    Promise.all([
      getMarinaAbsences(marinaId),
      getSlipAssignments(marinaId, { activeOnly: true }),
    ]).then(([abs, asgn]) => {
      setAbsences(abs);
      setAssignments(asgn.filter((a) => a.allowOwnerSubletWhenAway));
    }).finally(() => setLoading(false));
  }, [marinaId]);

  // Build a lookup: absenceId → assignment
  const assignmentBySlipId = Object.fromEntries(assignments.map((a) => [a.slipId, a]));

  async function handleCreateSubletWindow(absence: OwnerAbsenceDto) {
    setError(null);
    setSaving(true);
    const assignment = assignmentBySlipId[absence.slipId];
    if (!assignment) { setError('Could not find matching assignment.'); setSaving(false); return; }
    try {
      await createAvailabilityWindow(marinaId, {
        slipId:            absence.slipId,
        listedByKind:      'OwnerForHolder',
        listedByMarinaId:  marinaId,
        relatedAssignmentId: assignment.id,
        startsAt:          new Date(formState.startsAt).toISOString(),
        endsAt:            new Date(formState.endsAt).toISOString(),
        instantBook:       formState.instantBook,
        basePricePerNight: Number(formState.basePricePerNight),
        cleaningFee:       formState.cleaningFee ? Number(formState.cleaningFee) : null,
      });
      setShowFormFor(null);
      setFormState({ startsAt: '', endsAt: '', basePricePerNight: '', cleaningFee: '', instantBook: true });
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      setError(msg ?? 'Could not create listing.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="bg-card rounded-xl border border-border p-6">
      <h2 className="text-base font-semibold text-foreground mb-4">Holder Absences & Sublet Opportunities</h2>

      {loading && <p className="text-sm text-muted-foreground/70">Loading…</p>}

      {!loading && absences.length === 0 && (
        <p className="text-sm text-muted-foreground/70">No holder absences reported yet.</p>
      )}

      {!loading && absences.length > 0 && (
        <ul className="divide-y divide-border/50">
          {absences.map((a) => {
            const asgn = assignmentBySlipId[a.slipId];
            const canSublet = !!asgn;
            return (
              <li key={a.id} className="py-3">
                <div className="flex justify-between items-start">
                  <div>
                    <p className="text-sm font-medium text-foreground">
                      {a.slipName}
                    </p>
                    <p className="text-xs text-muted-foreground mt-0.5">
                      Away {a.startsOn} – {a.endsOn}
                      {a.notes && ` · ${a.notes}`}
                    </p>
                    {!canSublet && (
                      <p className="text-xs text-amber-600 mt-0.5">Lease does not permit sublet</p>
                    )}
                  </div>
                  {canSublet && showFormFor !== a.id && (
                    <button
                      onClick={() => { setShowFormFor(a.id); setError(null); }}
                      className="text-xs text-emerald-600 hover:text-emerald-800 underline mt-1"
                    >
                      + Create sublet listing
                    </button>
                  )}
                </div>

                {showFormFor === a.id && (
                  <div className="mt-3 bg-muted/30 rounded-lg p-3 space-y-2">
                    <p className="text-xs font-medium text-foreground">
                      Sublet listing for {a.slipName}
                      <span className="font-normal text-muted-foreground/70 ml-1">
                        (holder gets {((asgn.ownerSubletShareToHolder ?? 0) * 100).toFixed(0)}% of revenue)
                      </span>
                    </p>
                    <div className="grid grid-cols-2 gap-2">
                      <div>
                        <label className="block text-xs font-medium text-muted-foreground mb-1">Start</label>
                        <input type="date" value={formState.startsAt}
                          onChange={(e) => setFormState({ ...formState, startsAt: e.target.value })}
                          className={input} />
                      </div>
                      <div>
                        <label className="block text-xs font-medium text-muted-foreground mb-1">End</label>
                        <input type="date" value={formState.endsAt}
                          onChange={(e) => setFormState({ ...formState, endsAt: e.target.value })}
                          className={input} />
                      </div>
                      <div>
                        <label className="block text-xs font-medium text-muted-foreground mb-1">Price/night ($)</label>
                        <input type="number" step="0.01" min="0.01" value={formState.basePricePerNight}
                          onChange={(e) => setFormState({ ...formState, basePricePerNight: e.target.value })}
                          className={input} />
                      </div>
                      <div>
                        <label className="block text-xs font-medium text-muted-foreground mb-1">Cleaning fee ($)</label>
                        <input type="number" step="0.01" min="0" value={formState.cleaningFee}
                          onChange={(e) => setFormState({ ...formState, cleaningFee: e.target.value })}
                          className={input} />
                      </div>
                    </div>
                    <label className="flex items-center gap-2 text-xs text-foreground/80">
                      <input type="checkbox" checked={formState.instantBook}
                        onChange={(e) => setFormState({ ...formState, instantBook: e.target.checked })} />
                      Instant book
                    </label>
                    {error && <p className="text-xs text-red-600">{error}</p>}
                    <div className="flex gap-2">
                      <button onClick={() => handleCreateSubletWindow(a)} disabled={saving}
                        className="rounded-lg bg-emerald-700 text-white px-4 py-2 text-sm font-medium hover:bg-emerald-800 disabled:opacity-50">
                        {saving ? 'Creating…' : 'Create listing'}
                      </button>
                      <button onClick={() => { setShowFormFor(null); setError(null); }}
                        className={btnSecondary}>
                        Cancel
                      </button>
                    </div>
                  </div>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}

// ─── Inbox panel ─────────────────────────────────────────────────────────────

const INBOX_STATUS_BADGE: Record<string, string> = {
  PendingApproval:           'bg-amber-50 text-amber-700',
  PendingHostMarinaApproval: 'bg-amber-50 text-amber-700',
  Confirmed:                 'bg-emerald-50 text-emerald-700',
  Declined:                  'bg-red-50 text-red-600',
  Cancelled:                 'bg-muted text-muted-foreground',
  Completed:                 'bg-muted text-muted-foreground',
  NoShow:                    'bg-red-50 text-red-600',
};

const INBOX_STATUS_LABEL: Record<string, string> = {
  PendingApproval:           'Pending approval',
  PendingHostMarinaApproval: 'Pending host approval',
  Confirmed:                 'Confirmed',
  Declined:                  'Declined',
  Cancelled:                 'Cancelled',
  Completed:                 'Completed',
  NoShow:                    'No-show',
};

function fmtDate(d: string) {
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function InboxPanel({ marinaId }: { marinaId: string }) {
  const [reservations, setReservations] = useState<ReservationDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState<string>('');

  useEffect(() => {
    setLoading(true);
    getMarinaReservations(marinaId, statusFilter || undefined)
      .then(setReservations)
      .finally(() => setLoading(false));
  }, [marinaId, statusFilter]);

  async function handleApprove(id: string) {
    const updated = await approveReservation(marinaId, id);
    setReservations((prev) => prev.map((r) => r.id === id ? updated : r));
  }

  async function handleDecline(id: string) {
    if (!window.confirm('Decline this reservation?')) return;
    const updated = await declineReservation(marinaId, id);
    setReservations((prev) => prev.map((r) => r.id === id ? updated : r));
  }

  async function handleNoShow(id: string) {
    if (!window.confirm('Mark as no-show?')) return;
    const updated = await markNoShow(marinaId, id);
    setReservations((prev) => prev.map((r) => r.id === id ? updated : r));
  }

  const pending = reservations.filter((r) =>
    r.status === 'PendingApproval' || r.status === 'PendingHostMarinaApproval'
  );
  const others  = reservations.filter((r) =>
    r.status !== 'PendingApproval' && r.status !== 'PendingHostMarinaApproval'
  );

  return (
    <div className="bg-card rounded-xl border border-border p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-base font-semibold text-foreground">
          Reservation Inbox
          {pending.length > 0 && (
            <span className="ml-2 text-xs px-1.5 py-0.5 rounded-full bg-amber-100 text-amber-700 font-medium">
              {pending.length} pending
            </span>
          )}
        </h2>
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="text-xs border border-border rounded px-2 py-1 text-foreground/80 bg-card"
        >
          <option value="">All statuses</option>
          <option value="PendingApproval">Pending approval</option>
          <option value="PendingHostMarinaApproval">Pending host approval</option>
          <option value="Confirmed">Confirmed</option>
          <option value="Declined">Declined</option>
          <option value="Cancelled">Cancelled</option>
          <option value="Completed">Completed</option>
        </select>
      </div>

      {loading && <p className="text-sm text-muted-foreground/70">Loading…</p>}

      {!loading && reservations.length === 0 && (
        <p className="text-sm text-muted-foreground/70">No reservations found.</p>
      )}

      {!loading && pending.length > 0 && (
        <div className="mb-4 space-y-3">
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Awaiting action</p>
          {pending.map((r) => (
            <div key={r.id} className="border border-amber-200 bg-amber-50/40 rounded-lg p-4">
              <div className="flex justify-between items-start">
                <div>
                  <p className="text-sm font-medium text-foreground">{r.slipName} · {r.vesselName}</p>
                  <p className="text-xs text-muted-foreground mt-0.5">
                    {fmtDate(r.arrivesAt)} – {fmtDate(r.departsAt)} · {r.nights}n · ${r.total.toFixed(2)}
                  </p>
                  {r.notes && <p className="text-xs text-muted-foreground/70 mt-0.5 italic">"{r.notes}"</p>}
                </div>
                <span className={`text-xs px-1.5 py-0.5 rounded-full ${INBOX_STATUS_BADGE[r.status]}`}>
                  {INBOX_STATUS_LABEL[r.status]}
                </span>
              </div>
              <div className="mt-3 flex gap-2">
                {r.status === 'PendingHostMarinaApproval' && r.hostMarinaId !== marinaId ? (
                  <p className="text-xs text-muted-foreground italic">
                    Awaiting approval from the host marina before you can act.
                  </p>
                ) : (
                  <>
                    <button
                      onClick={() => handleApprove(r.id)}
                      className="text-xs rounded-md bg-emerald-600 text-white px-3 py-1.5 hover:bg-emerald-700"
                    >
                      Approve
                    </button>
                    <button
                      onClick={() => handleDecline(r.id)}
                      className="text-xs rounded-md border border-red-300 text-red-600 px-3 py-1.5 hover:bg-red-50"
                    >
                      Decline
                    </button>
                  </>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {!loading && others.length > 0 && (
        <ul className="divide-y divide-border/50">
          {others.map((r) => (
            <li key={r.id} className="py-3 flex justify-between items-start">
              <div>
                <p className="text-sm font-medium text-foreground">
                  {r.slipName}
                  <span className={`ml-2 text-xs px-1.5 py-0.5 rounded-full ${INBOX_STATUS_BADGE[r.status] ?? 'bg-muted text-muted-foreground'}`}>
                    {INBOX_STATUS_LABEL[r.status] ?? r.status}
                  </span>
                </p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  {r.vesselName} · {fmtDate(r.arrivesAt)} – {fmtDate(r.departsAt)} · ${r.total.toFixed(2)}
                </p>
              </div>
              {r.status === 'Confirmed' && (
                <button
                  onClick={() => handleNoShow(r.id)}
                  className="text-xs text-muted-foreground/70 hover:text-red-500 mt-1"
                >
                  No-show
                </button>
              )}
            </li>
          ))}
        </ul>
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
    <div className="bg-card rounded-xl border border-border p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-base font-semibold text-foreground">Staff</h2>
        <button onClick={() => setShowForm(true)} className="text-sm text-muted-foreground hover:text-foreground underline">
          + Invite
        </button>
      </div>

      {staff.length === 0 && !showForm && (
        <p className="text-sm text-muted-foreground/70">No staff yet.</p>
      )}

      <ul className="divide-y divide-border/50">
        {staff.map((m) => (
          <li key={m.id} className="py-2.5 flex justify-between items-center">
            <div>
              <span className="text-sm text-foreground">{m.userEmail ?? m.userId}</span>
              <span className={`ml-2 text-xs px-1.5 py-0.5 rounded-full ${
                m.isPending
                  ? 'bg-amber-50 text-amber-700'
                  : 'bg-muted text-muted-foreground'
              }`}>
                {m.isPending ? 'Pending' : m.role}
              </span>
            </div>
            <button onClick={() => handleRevoke(m.id)} className="text-xs text-muted-foreground/70 hover:text-red-500">
              Remove
            </button>
          </li>
        ))}
      </ul>

      {showForm && (
        <div className="mt-3 space-y-2 border-t border-border/50 pt-3">
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
              className={`${input} w-28 bg-card`}
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

// ─── Invoicing panel ─────────────────────────────────────────────────────────

const INV_STATUS_BADGE: Record<string, string> = {
  Draft:         'bg-muted text-muted-foreground',
  Sent:          'bg-blue-50 text-blue-700',
  PartiallyPaid: 'bg-amber-50 text-amber-700',
  Paid:          'bg-emerald-50 text-emerald-700',
  Overdue:       'bg-red-50 text-red-700',
  Voided:        'bg-muted text-muted-foreground/70',
};

function fmtDateOnly(d: string) {
  const [y, m, day] = d.split('-');
  return new Date(Number(y), Number(m) - 1, Number(day)).toLocaleDateString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
  });
}

function InvoicingPanel({ marinaId }: { marinaId: string }) {
  const [invoices, setInvoices] = useState<InvoiceSummaryDto[]>([]);
  const [accounts, setAccounts] = useState<BillingAccountDto[]>([]);
  const [statusFilter, setStatusFilter] = useState('');
  const [loading, setLoading] = useState(true);

  // Create form state
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [form, setForm] = useState({
    billingAccountId: '', issuedDate: '', dueDate: '', taxAmount: '0', notes: '',
    lineDesc: '', lineQty: '1', lineUnit: '',
  });
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  // Payment form
  const [payingFor, setPayingFor] = useState<string | null>(null);
  const [payForm, setPayForm] = useState({ amount: '', paidOn: '', method: 'Cash', referenceNumber: '', notes: '' });
  const [paying, setPaying] = useState(false);
  const [payError, setPayError] = useState<string | null>(null);

  useEffect(() => { getBillingAccounts(marinaId).then(setAccounts); }, [marinaId]);

  useEffect(() => {
    setLoading(true);
    getMarinaInvoices(marinaId, statusFilter ? { status: statusFilter } : undefined)
      .then(setInvoices)
      .finally(() => setLoading(false));
  }, [marinaId, statusFilter]);

  async function handleCreate() {
    if (!form.billingAccountId || !form.issuedDate || !form.dueDate || !form.lineDesc || !form.lineUnit) {
      setCreateError('Billing account, dates, and at least one line item are required.');
      return;
    }
    setCreateError(null);
    setCreating(true);
    try {
      const inv = await createInvoice(marinaId, {
        billingAccountId: form.billingAccountId,
        issuedDate: form.issuedDate,
        dueDate: form.dueDate,
        taxAmount: Number(form.taxAmount) || 0,
        notes: form.notes || null,
        lineItems: [{
          description: form.lineDesc,
          quantity: Number(form.lineQty) || 1,
          unitPrice: Number(form.lineUnit),
        }],
      });
      setInvoices((prev) => [inv, ...prev]);
      setShowCreateForm(false);
      setForm({ billingAccountId: '', issuedDate: '', dueDate: '', taxAmount: '0', notes: '', lineDesc: '', lineQty: '1', lineUnit: '' });
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      setCreateError(msg ?? 'Could not create invoice.');
    } finally {
      setCreating(false);
    }
  }

  async function handleSend(invoiceId: string) {
    if (!window.confirm('Send this invoice to the billing account?')) return;
    await sendInvoice(marinaId, invoiceId);
    setInvoices((prev) => prev.map((i) => i.id === invoiceId ? { ...i, status: 'Sent' } : i));
  }

  async function handleVoid(invoiceId: string) {
    if (!window.confirm('Void this invoice? This cannot be undone.')) return;
    await voidInvoice(marinaId, invoiceId);
    setInvoices((prev) => prev.map((i) => i.id === invoiceId ? { ...i, status: 'Voided' } : i));
  }

  async function handleRecordPayment() {
    if (!payingFor || !payForm.amount || !payForm.paidOn) { setPayError('Amount and date are required.'); return; }
    setPayError(null);
    setPaying(true);
    try {
      await recordPayment(marinaId, payingFor, {
        amount: Number(payForm.amount),
        paidOn: payForm.paidOn,
        method: payForm.method,
        referenceNumber: payForm.referenceNumber || null,
        notes: payForm.notes || null,
      });
      // Refresh invoices to get updated amountPaid / status
      const updated = await getMarinaInvoices(marinaId, statusFilter ? { status: statusFilter } : undefined);
      setInvoices(updated);
      setPayingFor(null);
      setPayForm({ amount: '', paidOn: '', method: 'Cash', referenceNumber: '', notes: '' });
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      setPayError(msg ?? 'Could not record payment.');
    } finally {
      setPaying(false);
    }
  }

  return (
    <div className="bg-card rounded-xl border border-border p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-base font-semibold text-foreground">Invoices</h2>
        <div className="flex items-center gap-3">
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="text-xs border border-border rounded px-2 py-1 text-foreground/80 bg-card"
          >
            <option value="">All statuses</option>
            <option value="Draft">Draft</option>
            <option value="Sent">Sent</option>
            <option value="PartiallyPaid">Partially paid</option>
            <option value="Paid">Paid</option>
            <option value="Overdue">Overdue</option>
            <option value="Voided">Voided</option>
          </select>
          <button onClick={() => setShowCreateForm(true)} className="text-sm text-muted-foreground hover:text-foreground underline">
            + New invoice
          </button>
        </div>
      </div>

      {loading && <p className="text-sm text-muted-foreground/70">Loading…</p>}

      {!loading && invoices.length === 0 && !showCreateForm && (
        <p className="text-sm text-muted-foreground/70">No invoices found.</p>
      )}

      {!loading && (
        <ul className="divide-y divide-border/50">
          {invoices.map((inv) => (
            <li key={inv.id} className="py-3">
              <div className="flex justify-between items-start">
                <div>
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="text-sm font-medium text-foreground">{inv.invoiceNumber}</span>
                    <span className={`text-xs px-1.5 py-0.5 rounded-full font-medium ${INV_STATUS_BADGE[inv.status] ?? 'bg-muted text-muted-foreground'}`}>
                      {inv.status}
                    </span>
                  </div>
                  <p className="text-xs text-muted-foreground mt-0.5">{inv.billingAccountName}</p>
                  <p className="text-xs text-muted-foreground/70 mt-0.5">
                    Issued {fmtDateOnly(inv.issuedDate)} · Due {fmtDateOnly(inv.dueDate)}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-sm font-semibold text-foreground">
                    ${inv.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </p>
                  {inv.balanceDue > 0 && inv.status !== 'Voided' && (
                    <p className="text-xs text-muted-foreground">
                      Balance: ${inv.balanceDue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </p>
                  )}
                </div>
              </div>

              {/* Actions */}
              <div className="mt-2 flex gap-3">
                {inv.status === 'Draft' && (
                  <button onClick={() => handleSend(inv.id)} className="text-xs text-blue-600 hover:text-blue-800 underline">Send</button>
                )}
                {['Sent', 'PartiallyPaid', 'Overdue'].includes(inv.status) && (
                  <button onClick={() => { setPayingFor(inv.id); setPayError(null); }} className="text-xs text-emerald-600 hover:text-emerald-800 underline">
                    Record payment
                  </button>
                )}
                {!['Paid', 'PartiallyPaid', 'Voided'].includes(inv.status) && (
                  <button onClick={() => handleVoid(inv.id)} className="text-xs text-red-500 hover:text-red-700 underline">Void</button>
                )}
              </div>

              {/* Payment form */}
              {payingFor === inv.id && (
                <div className="mt-3 bg-muted/30 rounded-lg p-3 space-y-2">
                  <p className="text-xs font-medium text-foreground">Record payment for {inv.invoiceNumber}</p>
                  <div className="grid grid-cols-2 gap-2">
                    <div>
                      <label className="block text-xs font-medium text-muted-foreground mb-1">Amount ($)</label>
                      <input type="number" step="0.01" min="0.01"
                        value={payForm.amount}
                        onChange={(e) => setPayForm({ ...payForm, amount: e.target.value })}
                        className={input} />
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-muted-foreground mb-1">Date</label>
                      <input type="date"
                        value={payForm.paidOn}
                        onChange={(e) => setPayForm({ ...payForm, paidOn: e.target.value })}
                        className={input} />
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-muted-foreground mb-1">Method</label>
                      <select value={payForm.method} onChange={(e) => setPayForm({ ...payForm, method: e.target.value })} className={`${input} bg-card`}>
                        <option>Cash</option>
                        <option>Check</option>
                        <option>CreditCard</option>
                        <option>BankTransfer</option>
                        <option>Other</option>
                      </select>
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-muted-foreground mb-1">Reference # (optional)</label>
                      <input type="text" value={payForm.referenceNumber}
                        onChange={(e) => setPayForm({ ...payForm, referenceNumber: e.target.value })}
                        className={input} />
                    </div>
                  </div>
                  {payError && <p className="text-xs text-red-600">{payError}</p>}
                  <div className="flex gap-2">
                    <button onClick={handleRecordPayment} disabled={paying}
                      className="rounded-lg bg-emerald-700 text-white px-4 py-2 text-sm font-medium hover:bg-emerald-800 disabled:opacity-50">
                      {paying ? 'Saving…' : 'Record payment'}
                    </button>
                    <button onClick={() => { setPayingFor(null); setPayError(null); }} className={btnSecondary}>Cancel</button>
                  </div>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}

      {/* Create invoice form */}
      {showCreateForm && (
        <div className="mt-4 border-t border-border/50 pt-4 space-y-3">
          <h3 className="text-sm font-medium text-foreground">New invoice</h3>
          <Field label="Billing account">
            <select value={form.billingAccountId} onChange={(e) => setForm({ ...form, billingAccountId: e.target.value })} className={`${input} bg-card`}>
              <option value="">— select account —</option>
              {accounts.map((a) => <option key={a.id} value={a.id}>{a.displayName}</option>)}
            </select>
          </Field>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Issue date">
              <input type="date" value={form.issuedDate} onChange={(e) => setForm({ ...form, issuedDate: e.target.value })} className={input} />
            </Field>
            <Field label="Due date">
              <input type="date" value={form.dueDate} onChange={(e) => setForm({ ...form, dueDate: e.target.value })} className={input} />
            </Field>
          </div>
          <p className="text-xs font-medium text-foreground/80 mt-1">Line item</p>
          <div className="grid grid-cols-3 gap-2">
            <div className="col-span-3 sm:col-span-1">
              <label className="block text-xs font-medium text-muted-foreground mb-1">Description</label>
              <input type="text" value={form.lineDesc} onChange={(e) => setForm({ ...form, lineDesc: e.target.value })} placeholder="Monthly slip fee" className={input} />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">Qty</label>
              <input type="number" step="1" min="1" value={form.lineQty} onChange={(e) => setForm({ ...form, lineQty: e.target.value })} className={input} />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">Unit price ($)</label>
              <input type="number" step="0.01" min="0.01" value={form.lineUnit} onChange={(e) => setForm({ ...form, lineUnit: e.target.value })} className={input} />
            </div>
          </div>
          <Field label="Tax amount ($)">
            <input type="number" step="0.01" min="0" value={form.taxAmount} onChange={(e) => setForm({ ...form, taxAmount: e.target.value })} className={input} />
          </Field>
          <Field label="Notes (optional)">
            <textarea value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} rows={2} className={`${input} resize-none`} />
          </Field>
          {createError && <p className="text-xs text-red-600">{createError}</p>}
          <div className="flex gap-2">
            <button onClick={handleCreate} disabled={creating} className={btn}>
              {creating ? 'Creating…' : 'Create invoice'}
            </button>
            <button onClick={() => { setShowCreateForm(false); setCreateError(null); }} className={btnSecondary}>Cancel</button>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Lease Inquiries panel ───────────────────────────────────────────────────

const INQSTATUS_BADGE: Record<string, string> = {
  Pending:     'bg-amber-50 text-amber-700',
  UnderReview: 'bg-blue-50 text-blue-700',
  Approved:    'bg-emerald-50 text-emerald-700',
  Declined:    'bg-red-50 text-red-700',
  Withdrawn:   'bg-muted text-muted-foreground',
};

function LeaseInquiriesPanel({ marinaId }: { marinaId: string }) {
  const [inquiries, setInquiries] = useState<LeaseInquiryDto[]>([]);
  const [statusFilter, setStatusFilter] = useState('');
  const [loading, setLoading] = useState(true);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState({
    agreedRateKind: '', agreedBaseRate: '',
    assignmentStartDate: '', assignmentEndDate: '',
    marinaNote: '', markUnderReview: false,
  });
  const [saving, setSaving] = useState(false);
  const [decliningId, setDecliningId] = useState<string | null>(null);
  const [declineReason, setDeclineReason] = useState('');

  useEffect(() => {
    setLoading(true);
    getMarinaLeaseInquiries(marinaId, statusFilter ? { status: statusFilter } : undefined)
      .then(setInquiries)
      .finally(() => setLoading(false));
  }, [marinaId, statusFilter]);

  function startEdit(inq: LeaseInquiryDto) {
    setEditingId(inq.id);
    setEditForm({
      agreedRateKind: inq.agreedRateKind ?? '',
      agreedBaseRate: inq.agreedBaseRate != null ? String(inq.agreedBaseRate) : '',
      assignmentStartDate: inq.assignmentStartDate ?? '',
      assignmentEndDate: inq.assignmentEndDate ?? '',
      marinaNote: inq.marinaNote ?? '',
      markUnderReview: false,
    });
  }

  async function handleSaveEdit(id: string) {
    setSaving(true);
    try {
      const updated = await updateLeaseInquiry(marinaId, id, {
        agreedRateKind: editForm.agreedRateKind || null,
        agreedBaseRate: editForm.agreedBaseRate ? Number(editForm.agreedBaseRate) : null,
        assignmentStartDate: editForm.assignmentStartDate || null,
        assignmentEndDate: editForm.assignmentEndDate || null,
        marinaNote: editForm.marinaNote || null,
        markUnderReview: editForm.markUnderReview,
      });
      setInquiries((prev) => prev.map((i) => i.id === id ? updated : i));
      setEditingId(null);
    } finally {
      setSaving(false);
    }
  }

  async function handleApprove(id: string) {
    if (!window.confirm('Approve this lease inquiry? A slip assignment and billing account will be created automatically.')) return;
    setSaving(true);
    try {
      const updated = await approveLeaseInquiry(marinaId, id);
      setInquiries((prev) => prev.map((i) => i.id === id ? updated : i));
    } finally {
      setSaving(false);
    }
  }

  async function handleDecline(id: string) {
    setSaving(true);
    try {
      const updated = await declineLeaseInquiry(marinaId, id, declineReason || undefined);
      setInquiries((prev) => prev.map((i) => i.id === id ? updated : i));
      setDecliningId(null);
      setDeclineReason('');
    } finally {
      setSaving(false);
    }
  }

  const pending = inquiries.filter((i) => ['Pending', 'UnderReview'].includes(i.status));
  const closed  = inquiries.filter((i) => ['Approved', 'Declined', 'Withdrawn'].includes(i.status));

  return (
    <div className="bg-card rounded-xl border border-border p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-base font-semibold text-foreground">Lease Inquiries</h2>
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="text-xs border border-border rounded px-2 py-1 text-foreground/80 bg-card"
        >
          <option value="">All statuses</option>
          <option value="Pending">Pending</option>
          <option value="UnderReview">Under Review</option>
          <option value="Approved">Approved</option>
          <option value="Declined">Declined</option>
          <option value="Withdrawn">Withdrawn</option>
        </select>
      </div>

      {loading && <p className="text-sm text-muted-foreground/70">Loading…</p>}

      {!loading && inquiries.length === 0 && (
        <p className="text-sm text-muted-foreground/70">No lease inquiries yet.</p>
      )}

      {!loading && pending.length > 0 && (
        <div className="mb-6 space-y-3">
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Awaiting action</p>
          {pending.map((inq) => (
            <div key={inq.id} className="border border-amber-200 bg-amber-50/40 rounded-lg p-4">
              <div className="flex justify-between items-start">
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-foreground">{inq.slipName}</p>
                  <p className="text-xs text-foreground/80 mt-0.5">
                    {inq.requestingUserName} ({inq.requestingUserEmail})
                  </p>
                  <p className="text-xs text-muted-foreground mt-0.5">
                    {inq.desiredTerm}
                    {inq.desiredStartDate && ` · Start: ${inq.desiredStartDate}`}
                    {inq.vesselName && ` · ${inq.vesselName}`}
                  </p>
                  {inq.message && <p className="text-xs text-muted-foreground/70 mt-1 italic">"{inq.message}"</p>}
                  {inq.agreedBaseRate != null && (
                    <p className="text-xs text-blue-600 mt-1">
                      Agreed: {inq.agreedRateKind === 'PerFoot' ? `$${inq.agreedBaseRate}/ft` : `$${inq.agreedBaseRate}`}/{inq.desiredTerm}
                      {inq.assignmentStartDate && ` · From ${inq.assignmentStartDate}`}
                      {inq.assignmentEndDate && ` to ${inq.assignmentEndDate}`}
                    </p>
                  )}
                  {inq.marinaNote && <p className="text-xs text-muted-foreground mt-1">Note: {inq.marinaNote}</p>}
                  <p className="text-xs text-muted-foreground/50 mt-1">Received {new Date(inq.createdAt).toLocaleDateString()}</p>
                </div>
                <span className={`text-xs px-1.5 py-0.5 rounded-full shrink-0 ml-3 ${INQSTATUS_BADGE[inq.status]}`}>
                  {inq.status === 'UnderReview' ? 'Under Review' : inq.status}
                </span>
              </div>

              {editingId === inq.id ? (
                <div className="mt-3 border-t border-amber-200 pt-3 space-y-2">
                  <div className="grid grid-cols-2 gap-2">
                    <Field label="Agreed rate kind">
                      <select value={editForm.agreedRateKind}
                        onChange={(e) => setEditForm((f) => ({ ...f, agreedRateKind: e.target.value }))}
                        className={`${input} bg-card`}>
                        <option value="">— not set —</option>
                        <option value="Flat">Flat</option>
                        <option value="PerFoot">Per foot</option>
                      </select>
                    </Field>
                    <Field label="Agreed base rate ($)">
                      <input type="number" step="0.01" value={editForm.agreedBaseRate}
                        onChange={(e) => setEditForm((f) => ({ ...f, agreedBaseRate: e.target.value }))}
                        className={input} />
                    </Field>
                    <Field label="Assignment start">
                      <input type="date" value={editForm.assignmentStartDate}
                        onChange={(e) => setEditForm((f) => ({ ...f, assignmentStartDate: e.target.value }))}
                        className={input} />
                    </Field>
                    <Field label="Assignment end (optional)">
                      <input type="date" value={editForm.assignmentEndDate}
                        onChange={(e) => setEditForm((f) => ({ ...f, assignmentEndDate: e.target.value }))}
                        className={input} />
                    </Field>
                  </div>
                  <Field label="Marina note">
                    <textarea value={editForm.marinaNote} rows={2}
                      onChange={(e) => setEditForm((f) => ({ ...f, marinaNote: e.target.value }))}
                      className={`${input} resize-none`} />
                  </Field>
                  <label className="flex items-center gap-2 text-sm text-foreground/80">
                    <input type="checkbox" checked={editForm.markUnderReview}
                      onChange={(e) => setEditForm((f) => ({ ...f, markUnderReview: e.target.checked }))} />
                    Mark as under review
                  </label>
                  <div className="flex gap-2">
                    <button onClick={() => handleSaveEdit(inq.id)} disabled={saving} className={btn}>
                      {saving ? '…' : 'Save'}
                    </button>
                    <button onClick={() => setEditingId(null)} className={btnSecondary}>Cancel</button>
                  </div>
                </div>
              ) : (
                <div className="mt-3 flex gap-2 flex-wrap">
                  <button onClick={() => startEdit(inq)} className={btnSecondary}>Edit terms</button>
                  <button
                    onClick={() => handleApprove(inq.id)} disabled={saving}
                    className="text-xs rounded-md bg-emerald-600 text-white px-3 py-1.5 hover:bg-emerald-700 disabled:opacity-50"
                  >
                    Approve
                  </button>
                  <button
                    onClick={() => { setDecliningId(inq.id); setDeclineReason(''); }}
                    className="text-xs rounded-md border border-red-300 text-red-600 px-3 py-1.5 hover:bg-red-50"
                  >
                    Decline
                  </button>
                </div>
              )}

              {decliningId === inq.id && (
                <div className="mt-2 space-y-2 border-t border-amber-200 pt-2">
                  <input value={declineReason} onChange={(e) => setDeclineReason(e.target.value)}
                    placeholder="Reason (optional)" className={input} />
                  <div className="flex gap-2">
                    <button onClick={() => handleDecline(inq.id)} disabled={saving}
                      className="text-xs rounded-md bg-red-600 text-white px-3 py-1.5 hover:bg-red-700 disabled:opacity-50">
                      Confirm decline
                    </button>
                    <button onClick={() => setDecliningId(null)} className={btnSecondary}>Cancel</button>
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {!loading && closed.length > 0 && (
        <div>
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-2">History</p>
          <ul className="divide-y divide-border/50">
            {closed.map((inq) => (
              <li key={inq.id} className="py-3">
                <div className="flex justify-between items-start">
                  <div>
                    <div className="flex items-center gap-2">
                      <p className="text-sm font-medium text-foreground">{inq.slipName}</p>
                      <span className={`text-xs px-1.5 py-0.5 rounded-full ${INQSTATUS_BADGE[inq.status]}`}>
                        {inq.status}
                      </span>
                    </div>
                    <p className="text-xs text-muted-foreground mt-0.5">
                      {inq.requestingUserName} · {inq.desiredTerm}
                      {inq.desiredStartDate && ` · ${inq.desiredStartDate}`}
                    </p>
                    {inq.status === 'Approved' && (
                      <p className="text-xs text-emerald-600 mt-0.5">
                        Approved{inq.approvedByUserName ? ` by ${inq.approvedByUserName}` : ''}
                        {inq.approvedAt ? ` · ${new Date(inq.approvedAt).toLocaleDateString()}` : ''}
                        {inq.slipAssignmentId && ' · Assignment created'}
                      </p>
                    )}
                    {inq.status === 'Declined' && (
                      <p className="text-xs text-red-500 mt-0.5">
                        Declined{inq.declinedByUserName ? ` by ${inq.declinedByUserName}` : ''}
                        {inq.declinedAt ? ` · ${new Date(inq.declinedAt).toLocaleDateString()}` : ''}
                      </p>
                    )}
                    {inq.status === 'Withdrawn' && (
                      <p className="text-xs text-muted-foreground/70 mt-0.5">Withdrawn by boater</p>
                    )}
                  </div>
                </div>
              </li>
            ))}
          </ul>
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

  if (loading) return <div className="min-h-screen bg-background flex items-center justify-center text-muted-foreground text-sm">Loading…</div>;
  if (error || !marina || !marinaId) return (
    <div className="min-h-screen bg-background flex items-center justify-center">
      <p className="text-muted-foreground text-sm">{error ?? 'Marina not found.'}</p>
    </div>
  );

  return (
    <div className="min-h-screen bg-background">
      <NavBar />
      <MarinaStatusBanner marina={marina} onUpdated={setMarina} />
      <div className="max-w-4xl mx-auto py-10 px-4 space-y-6">
        <MarinaInfoPanel marina={marina} onSaved={setMarina} />
        <DocksPanel marinaId={marinaId} />
        <SlipsPanel marinaId={marinaId} />
        <BillingAccountsPanel marinaId={marinaId} />
        <AssignmentsPanel marinaId={marinaId} />
        <ListingsPanel marinaId={marinaId} />
        <SubletPanel marinaId={marinaId} />
        <InboxPanel marinaId={marinaId} />
        <LeaseInquiriesPanel marinaId={marinaId} />
        <InvoicingPanel marinaId={marinaId} />
        <MaintenancePanel marinaId={marinaId} />
        <PhotosPanel marinaId={marinaId} />
        <AnnouncementsPanel marinaId={marinaId} />
        <StaffPanel marinaId={marinaId} />
      </div>
    </div>
  );
}

// ─── Marina status banner ────────────────────────────────────────────────────

function MarinaStatusBanner({ marina, onUpdated }: { marina: MarinaDto; onUpdated: (m: MarinaDto) => void }) {
  const [saving, setSaving] = useState(false);

  if (!marina.isSetupComplete) {
    return (
      <div className="bg-amber-50 border-b border-amber-200 px-4 py-3">
        <div className="max-w-4xl mx-auto flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <span className="text-amber-500 text-lg">🚧</span>
            <div>
              <p className="text-sm font-medium text-amber-800">Setup not complete</p>
              <p className="text-xs text-amber-600 mt-0.5">
                Your marina is in draft mode and not visible to boaters.
              </p>
            </div>
          </div>
          <a
            href={`/marina/${marina.id}/setup`}
            className="shrink-0 rounded-lg bg-amber-600 text-white text-sm font-medium px-4 py-1.5 hover:bg-amber-700 transition-colors"
          >
            Continue setup →
          </a>
        </div>
      </div>
    );
  }

  if (!marina.isListed) {
    return (
      <div className="bg-muted border-b border-border px-4 py-3">
        <div className="max-w-4xl mx-auto flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <span className="text-muted-foreground text-lg">⚪</span>
            <div>
              <p className="text-sm font-medium text-foreground">Not listed on marketplace</p>
              <p className="text-xs text-muted-foreground mt-0.5">
                Boaters cannot find or book your slips yet. Go live when you're ready.
              </p>
            </div>
          </div>
          <button
            type="button"
            disabled={saving}
            onClick={async () => {
              setSaving(true);
              try {
                const updated = await updateMarina(marina.id, { isListed: true });
                onUpdated({ ...marina, isListed: updated.isListed });
              } catch {
                alert('Failed to go live. Please try again.');
              } finally {
                setSaving(false);
              }
            }}
            className="shrink-0 rounded-lg bg-emerald-600 text-white text-sm font-medium px-4 py-1.5 hover:bg-emerald-700 disabled:opacity-50 transition-colors"
          >
            {saving ? 'Publishing…' : 'Go live →'}
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-emerald-50 border-b border-emerald-200 px-4 py-2.5">
      <div className="max-w-4xl mx-auto flex items-center justify-between gap-4">
        <div className="flex items-center gap-2">
          <span className="text-emerald-500 text-sm">●</span>
          <p className="text-sm text-emerald-800 font-medium">Listed — visible to boaters on the marketplace</p>
        </div>
        <button
          type="button"
          disabled={saving}
          onClick={async () => {
            if (!confirm('Remove your marina from the marketplace? Boaters will no longer be able to find or book your slips.')) return;
            setSaving(true);
            try {
              const updated = await updateMarina(marina.id, { isListed: false });
              onUpdated({ ...marina, isListed: updated.isListed });
            } catch {
              alert('Failed to update. Please try again.');
            } finally {
              setSaving(false);
            }
          }}
          className="text-xs text-emerald-600 hover:text-emerald-800 underline disabled:opacity-50"
        >
          {saving ? 'Saving…' : 'Pause listing'}
        </button>
      </div>
    </div>
  );
}

// ─── shared style tokens ─────────────────────────────────────────────────────

const input = 'w-full rounded-lg border border-border px-3 py-2 text-sm bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-ring';
const btn = 'rounded-lg bg-foreground text-background px-4 py-2 text-sm font-medium hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors';
const btnSecondary = 'rounded-lg border border-border text-muted-foreground px-4 py-2 text-sm hover:bg-muted/30 transition-colors';

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-xs font-medium text-muted-foreground mb-1">{label}</label>
      {children}
      {error && <p className="mt-0.5 text-xs text-red-600">{error}</p>}
    </div>
  );
}

// ─── Maintenance panel ───────────────────────────────────────────────────────

const REQ_STATUS_COLORS: Record<string, string> = {
  Submitted:   'bg-blue-50 text-blue-700',
  UnderReview: 'bg-yellow-50 text-yellow-700',
  InProgress:  'bg-orange-50 text-orange-700',
  Completed:   'bg-green-50 text-green-700',
  Declined:    'bg-red-50 text-red-700',
};

const WO_STATUS_COLORS: Record<string, string> = {
  Open:       'bg-muted text-muted-foreground',
  InProgress: 'bg-blue-50 text-blue-700',
  OnHold:     'bg-yellow-50 text-yellow-700',
  Completed:  'bg-green-50 text-green-700',
  Cancelled:  'bg-red-50 text-red-700',
};

function MaintenancePanel({ marinaId }: { marinaId: string }) {
  const [requests, setRequests]   = useState<MaintenanceRequestDto[]>([]);
  const [workOrders, setWorkOrders] = useState<WorkOrderDto[]>([]);
  const [activeTab, setActiveTab] = useState<'requests' | 'workOrders'>('requests');
  const [showWoForm, setShowWoForm] = useState(false);
  const [woTitle, setWoTitle]     = useState('');
  const [woDesc, setWoDesc]       = useState('');
  const [woPriority, setWoPriority] = useState('Medium');
  const [linked, setLinked]       = useState('');

  useEffect(() => {
    getMarinaMaintenanceRequests(marinaId).then(setRequests).catch(() => {});
    getMarinaWorkOrders(marinaId).then(setWorkOrders).catch(() => {});
  }, [marinaId]);

  async function advanceRequest(r: MaintenanceRequestDto) {
    const next: Record<string, string> = {
      Submitted: 'UnderReview', UnderReview: 'InProgress', InProgress: 'Completed',
    };
    if (!next[r.status]) return;
    const updated = await updateMaintenanceRequestStatus(marinaId, r.id, { status: next[r.status], priority: r.priority });
    setRequests((prev) => prev.map((x) => x.id === r.id ? updated : x));
  }

  async function handleCreateWo(e: React.FormEvent) {
    e.preventDefault();
    const wo = await createWorkOrder(marinaId, {
      title: woTitle, description: woDesc, priority: woPriority,
      maintenanceRequestId: linked || undefined,
    });
    setWorkOrders((prev) => [wo, ...prev]);
    setShowWoForm(false); setWoTitle(''); setWoDesc(''); setWoPriority('Medium'); setLinked('');
  }

  async function advanceWorkOrder(w: WorkOrderDto) {
    const next: Record<string, string> = {
      Open: 'InProgress', InProgress: 'Completed',
    };
    if (!next[w.status]) return;
    const updated = await updateWorkOrder(marinaId, w.id, { ...w, status: next[w.status] });
    setWorkOrders((prev) => prev.map((x) => x.id === w.id ? updated : x));
  }

  return (
    <div className="bg-card rounded-xl border border-border p-6">
      <h2 className="text-base font-semibold text-foreground mb-4">Maintenance</h2>
      <div className="flex gap-2 mb-4">
        {(['requests', 'workOrders'] as const).map((t) => (
          <button key={t} onClick={() => setActiveTab(t)}
            className={`text-sm px-3 py-1 rounded-lg ${activeTab === t ? 'bg-foreground text-background' : 'border border-border text-muted-foreground hover:bg-muted/30'}`}>
            {t === 'requests' ? `Requests (${requests.length})` : `Work Orders (${workOrders.length})`}
          </button>
        ))}
      </div>

      {activeTab === 'requests' && (
        requests.length === 0
          ? <p className="text-sm text-muted-foreground/70">No maintenance requests.</p>
          : <div className="space-y-3">
            {requests.map((r) => (
              <div key={r.id} className="border border-border/50 rounded-lg p-3">
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <p className="text-sm font-medium text-foreground">{r.title}</p>
                    <p className="text-xs text-muted-foreground mt-0.5">{r.boaterName} · {new Date(r.submittedAt).toLocaleDateString()}</p>
                  </div>
                  <div className="flex items-center gap-2 shrink-0">
                    <span className={`text-xs px-2 py-0.5 rounded-full ${REQ_STATUS_COLORS[r.status] ?? ''}`}>{r.status}</span>
                    {!['Completed','Declined'].includes(r.status) && (
                      <button onClick={() => advanceRequest(r)} className="text-xs text-muted-foreground/70 hover:text-foreground underline">
                        Advance
                      </button>
                    )}
                  </div>
                </div>
                <p className="text-xs text-muted-foreground/70 mt-1">{r.description}</p>
              </div>
            ))}
          </div>
      )}

      {activeTab === 'workOrders' && (
        <div className="space-y-4">
          <button onClick={() => setShowWoForm((v) => !v)} className={btn}>
            {showWoForm ? 'Cancel' : 'New work order'}
          </button>
          {showWoForm && (
            <form onSubmit={handleCreateWo} className="border border-border/50 rounded-lg p-3 space-y-2">
              <input value={woTitle} onChange={(e) => setWoTitle(e.target.value)} className={input} placeholder="Title" required />
              <textarea value={woDesc} onChange={(e) => setWoDesc(e.target.value)} className={`${input} h-20 resize-none`} placeholder="Description" required />
              <div className="flex gap-2">
                <select value={woPriority} onChange={(e) => setWoPriority(e.target.value)} className={`${input} w-32`}>
                  {['Low','Medium','High','Urgent'].map((p) => <option key={p}>{p}</option>)}
                </select>
                <select value={linked} onChange={(e) => setLinked(e.target.value)} className={input}>
                  <option value="">No linked request</option>
                  {requests.map((r) => <option key={r.id} value={r.id}>{r.title}</option>)}
                </select>
              </div>
              <button type="submit" className={btn}>Create</button>
            </form>
          )}
          {workOrders.length === 0
            ? <p className="text-sm text-muted-foreground/70">No work orders.</p>
            : workOrders.map((w) => (
              <div key={w.id} className="border border-border/50 rounded-lg p-3">
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <p className="text-sm font-medium text-foreground">{w.title}</p>
                    {w.assignedToName && <p className="text-xs text-muted-foreground">{w.assignedToName}</p>}
                  </div>
                  <div className="flex items-center gap-2 shrink-0">
                    <span className={`text-xs px-2 py-0.5 rounded-full ${WO_STATUS_COLORS[w.status] ?? ''}`}>{w.status}</span>
                    {!['Completed','Cancelled'].includes(w.status) && (
                      <button onClick={() => advanceWorkOrder(w)} className="text-xs text-muted-foreground/70 hover:text-foreground underline">
                        Advance
                      </button>
                    )}
                  </div>
                </div>
                {w.scheduledDate && <p className="text-xs text-muted-foreground/70 mt-1">Scheduled: {w.scheduledDate}</p>}
              </div>
            ))
          }
        </div>
      )}
    </div>
  );
}

// ─── Photos panel ────────────────────────────────────────────────────────────

type PhotoKindTab = 'Logo' | 'Banner' | 'Gallery' | 'Aerial' | 'Approach';

function PhotosPanel({ marinaId }: { marinaId: string }) {
  const { upload, getPhotos, reorder, deletePhoto } = usePhotoUpload(marinaId);
  const [photos, setPhotos] = useState<MarinaPhotoDto[]>([]);
  const [activeTab, setActiveTab] = useState<PhotoKindTab>('Logo');
  const [showCropModal, setShowCropModal] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const TABS: PhotoKindTab[] = ['Logo', 'Banner', 'Gallery', 'Aerial', 'Approach'];

  useEffect(() => {
    getPhotos().then(setPhotos).catch(() => {});
  }, [marinaId]);

  // Poll once after 2s when any photo is still processing
  useEffect(() => {
    const processing = photos.some((p) => !p.urlThumbnail);
    if (!processing) return;
    const id = setTimeout(() => {
      getPhotos().then(setPhotos).catch(() => {});
    }, 2000);
    return () => clearTimeout(id);
  }, [photos]);

  const kindPhotos = photos.filter((p) => p.kind === activeTab);
  const logoPhoto = photos.find((p) => p.kind === 'Logo') ?? null;
  const bannerPhoto = photos.find((p) => p.kind === 'Banner') ?? null;

  async function handleCropComplete(blob: Blob, width: number, height: number) {
    setUploading(true);
    setError(null);
    try {
      // delete existing Logo/Banner first (only one allowed)
      const existing = photos.find((p) => p.kind === activeTab);
      if (existing && (activeTab === 'Logo' || activeTab === 'Banner')) {
        await deletePhoto(existing.id);
        setPhotos((prev) => prev.filter((p) => p.id !== existing.id));
      }
      const photo = await upload({
        kind: activeTab,
        file: blob,
        contentType: 'image/jpeg',
        imageWidth: width,
        imageHeight: height,
      });
      setPhotos((prev) => [...prev, photo]);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Upload failed');
    } finally {
      setUploading(false);
      setShowCropModal(false);
    }
  }

  async function handleGalleryUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    setError(null);
    try {
      const img = new Image();
      const url = URL.createObjectURL(file);
      const dims = await new Promise<{ w: number; h: number }>((res, rej) => {
        img.onload = () => res({ w: img.naturalWidth, h: img.naturalHeight });
        img.onerror = rej;
        img.src = url;
      });
      URL.revokeObjectURL(url);
      const photo = await upload({
        kind: activeTab,
        file,
        contentType: file.type || 'image/jpeg',
        imageWidth: dims.w,
        imageHeight: dims.h,
      });
      setPhotos((prev) => [...prev, photo]);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Upload failed');
    } finally {
      setUploading(false);
      e.target.value = '';
    }
  }

  async function handleReorder(photo: MarinaPhotoDto, direction: 'up' | 'down') {
    try {
      await reorder(photo.id, direction);
      const updated = await getPhotos();
      setPhotos(updated);
    } catch {}
  }

  async function handleDelete(photo: MarinaPhotoDto) {
    try {
      await deletePhoto(photo.id);
      setPhotos((prev) => prev.filter((p) => p.id !== photo.id));
    } catch {}
  }

  const aspectRatio = activeTab === 'Logo' ? 1 : activeTab === 'Banner' ? 16 / 9 : undefined;
  const isSingleSlot = activeTab === 'Logo' || activeTab === 'Banner';

  return (
    <div className="bg-card rounded-xl border border-border p-6">
      <h2 className="text-base font-semibold text-foreground mb-4">Photos</h2>

      {/* Tab bar */}
      <div className="flex gap-2 mb-6 flex-wrap">
        {TABS.map((tab) => (
          <button
            key={tab}
            type="button"
            onClick={() => setActiveTab(tab)}
            className={`text-sm px-3 py-1 rounded-lg ${activeTab === tab ? 'bg-foreground text-background' : 'border border-border text-muted-foreground hover:bg-muted/30'}`}
          >
            {tab}
          </button>
        ))}
      </div>

      {error && <p className="text-sm text-red-600 mb-4">{error}</p>}

      {/* Logo tab */}
      {activeTab === 'Logo' && (
        <div className="flex flex-col items-start gap-4">
          {logoPhoto ? (
            <div className="relative group w-32 h-32">
              <img
                src={logoPhoto.urlThumbnail ?? undefined}
                alt="Marina logo"
                className="w-32 h-32 rounded-xl object-cover border border-border"
              />
              {!logoPhoto.urlThumbnail && (
                <div className="absolute inset-0 flex items-center justify-center bg-muted/80 rounded-xl">
                  <span className="text-xs text-muted-foreground animate-pulse">Processing…</span>
                </div>
              )}
              <button
                type="button"
                onClick={() => handleDelete(logoPhoto)}
                className="absolute -top-2 -right-2 rounded-full bg-destructive text-destructive-foreground w-5 h-5 text-xs flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity"
              >✕</button>
            </div>
          ) : (
            <div className="w-32 h-32 rounded-xl border-2 border-dashed border-border flex items-center justify-center text-muted-foreground text-xs">
              No logo
            </div>
          )}
          <button
            type="button"
            disabled={uploading}
            onClick={() => setShowCropModal(true)}
            className={`${btn} text-sm`}
          >
            {uploading ? 'Uploading…' : logoPhoto ? 'Replace logo' : 'Upload logo'}
          </button>
          <p className="text-xs text-muted-foreground">Square image, 1:1 ratio (±10%). Recommended: 512×512px or larger.</p>
        </div>
      )}

      {/* Banner tab */}
      {activeTab === 'Banner' && (
        <div className="flex flex-col gap-4">
          {bannerPhoto ? (
            <div className="relative group rounded-xl overflow-hidden border border-border aspect-video max-w-xl">
              <img
                src={bannerPhoto.urlMedium ?? bannerPhoto.urlFull ?? undefined}
                alt="Marina banner"
                className="w-full h-full object-cover"
              />
              {!bannerPhoto.urlThumbnail && (
                <div className="absolute inset-0 flex items-center justify-center bg-muted/80">
                  <span className="text-xs text-muted-foreground animate-pulse">Processing…</span>
                </div>
              )}
              <button
                type="button"
                onClick={() => handleDelete(bannerPhoto)}
                className="absolute top-2 right-2 rounded-full bg-destructive text-destructive-foreground w-6 h-6 text-xs flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity"
              >✕</button>
            </div>
          ) : (
            <div className="rounded-xl border-2 border-dashed border-border aspect-video max-w-xl flex items-center justify-center text-muted-foreground text-xs">
              No banner
            </div>
          )}
          <div className="flex items-center gap-3">
            <button
              type="button"
              disabled={uploading}
              onClick={() => setShowCropModal(true)}
              className={`${btn} text-sm`}
            >
              {uploading ? 'Uploading…' : bannerPhoto ? 'Replace banner' : 'Upload banner'}
            </button>
          </div>
          <p className="text-xs text-muted-foreground">Wide banner, 16:9 ratio (±15%). Recommended: 1600×900px or larger.</p>
        </div>
      )}

      {/* Gallery / Aerial / Approach tabs */}
      {!isSingleSlot && (
        <div className="space-y-4">
          <div>
            <label className="cursor-pointer">
              <span className={`${btn} text-sm ${uploading ? 'opacity-50 pointer-events-none' : ''}`}>
                {uploading ? 'Uploading…' : 'Add photo'}
              </span>
              <input
                type="file"
                accept="image/*"
                className="hidden"
                disabled={uploading}
                onChange={handleGalleryUpload}
              />
            </label>
            {activeTab === 'Approach' && (
              <p className="mt-1 text-xs text-muted-foreground">Min 800px wide. Add captions and optional GPS coordinates to help boaters navigate.</p>
            )}
          </div>

          {kindPhotos.length === 0 ? (
            <p className="text-sm text-muted-foreground/70">No {activeTab.toLowerCase()} photos yet.</p>
          ) : (
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3">
              {kindPhotos.map((photo, idx) => (
                <PhotoCard
                  key={photo.id}
                  photo={photo}
                  isFirst={idx === 0}
                  isLast={idx === kindPhotos.length - 1}
                  onMoveUp={() => handleReorder(photo, 'up')}
                  onMoveDown={() => handleReorder(photo, 'down')}
                  onDelete={() => handleDelete(photo)}
                  showCaption={activeTab === 'Approach'}
                  showLatLng={activeTab === 'Approach'}
                />
              ))}
            </div>
          )}
        </div>
      )}

      {/* Crop modal for Logo/Banner */}
      {showCropModal && (
        <CropUploadModal
          aspectRatio={aspectRatio}
          onComplete={handleCropComplete}
          onCancel={() => setShowCropModal(false)}
        />
      )}
    </div>
  );
}

// ─── Announcements panel ─────────────────────────────────────────────────────

function AnnouncementsPanel({ marinaId }: { marinaId: string }) {
  const [announcements, setAnnouncements] = useState<AnnouncementDto[]>([]);
  const [showForm, setShowForm]  = useState(false);
  const [title, setTitle]        = useState('');
  const [body, setBody]          = useState('');
  const [audience, setAudience]  = useState('Both');
  const [pinned, setPinned]      = useState(false);

  useEffect(() => {
    getMarinaAnnouncements(marinaId).then(setAnnouncements).catch(() => {});
  }, [marinaId]);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    const a = await createAnnouncement(marinaId, { title, body, audience, isPinned: pinned });
    setAnnouncements((prev) => [a, ...prev]);
    setShowForm(false); setTitle(''); setBody(''); setAudience('Both'); setPinned(false);
  }

  async function handlePublish(a: AnnouncementDto) {
    const updated = await publishAnnouncement(marinaId, a.id);
    setAnnouncements((prev) => prev.map((x) => x.id === a.id ? updated : x));
  }

  async function handleDelete(a: AnnouncementDto) {
    await deleteAnnouncement(marinaId, a.id);
    setAnnouncements((prev) => prev.filter((x) => x.id !== a.id));
  }

  return (
    <div className="bg-card rounded-xl border border-border p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-base font-semibold text-foreground">Announcements</h2>
        <button onClick={() => setShowForm((v) => !v)} className={btn}>
          {showForm ? 'Cancel' : 'New announcement'}
        </button>
      </div>

      {showForm && (
        <form onSubmit={handleCreate} className="border border-border/50 rounded-lg p-3 mb-4 space-y-2">
          <input value={title} onChange={(e) => setTitle(e.target.value)} className={input} placeholder="Title" required />
          <textarea value={body} onChange={(e) => setBody(e.target.value)} className={`${input} h-24 resize-none`} placeholder="Body (Markdown supported)" required />
          <div className="flex gap-2 flex-wrap">
            <select value={audience} onChange={(e) => setAudience(e.target.value)} className={`${input} w-40`}>
              <option value="Both">Both</option>
              <option value="Customers">Customers</option>
              <option value="IncomingBoaters">Incoming Boaters</option>
            </select>
            <label className="flex items-center gap-1 text-sm text-foreground/80">
              <input type="checkbox" checked={pinned} onChange={(e) => setPinned(e.target.checked)} /> Pin
            </label>
          </div>
          <button type="submit" className={btn}>Save as draft</button>
        </form>
      )}

      {announcements.length === 0
        ? <p className="text-sm text-muted-foreground/70">No announcements yet.</p>
        : <div className="space-y-3">
          {announcements.map((a) => (
            <div key={a.id} className="border border-border/50 rounded-lg p-3">
              <div className="flex items-start justify-between gap-2">
                <div>
                  {a.isPinned && <span className="text-xs text-muted-foreground/70 mr-1">📌</span>}
                  <span className="text-sm font-medium text-foreground">{a.title}</span>
                  <span className="ml-2 text-xs text-muted-foreground/70">{a.audience}</span>
                </div>
                <div className="flex items-center gap-2 shrink-0">
                  {a.publishedAt
                    ? <span className="text-xs text-green-600">Published</span>
                    : <button onClick={() => handlePublish(a)} className="text-xs text-blue-600 hover:underline">Publish</button>
                  }
                  <button onClick={() => handleDelete(a)} className="text-xs text-red-400 hover:underline">Delete</button>
                </div>
              </div>
              <p className="text-xs text-muted-foreground mt-1 line-clamp-2">{a.body}</p>
            </div>
          ))}
        </div>
      }
    </div>
  );
}
