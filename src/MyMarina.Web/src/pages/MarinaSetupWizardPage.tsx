import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  getMarina, updateMarina, setupDocks, getDocks, getSlips,
  createSlip, updateSlip, deleteSlip,
  type MarinaDto, type SetupDockData,
} from '@/api/api';
import { NavBar } from '@/components/NavBar';
import { MapPicker } from '@/components/MapPicker';
import { nominatimGeocode } from '@/utils/nominatimGeocode';
import {
  generateDockNames, generateDockSlips,
  type DockConvention, type SlipConvention,
} from '@/utils/namingConventions';
import { useWizardDraft } from '@/hooks/useWizardDraft';
import { usePhotoUpload } from '@/hooks/usePhotoUpload';
import { CropUploadModal } from '@/components/CropUploadModal';

// ─── Shared styles ────────────────────────────────────────────────────────────
const input = 'w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring';
const btn = 'rounded-lg bg-primary text-primary-foreground px-5 py-2.5 text-sm font-medium hover:bg-primary/90 disabled:opacity-50 transition-colors';
const btnSecondary = 'rounded-lg border border-border bg-card px-5 py-2.5 text-sm font-medium hover:bg-muted/30 transition-colors';

// ─── Wizard draft shape ───────────────────────────────────────────────────────
interface WizardDraft {
  step1?: {
    addressStreet?: string; addressCity?: string; addressState?: string;
    addressZip?: string; addressCountry?: string;
    phoneNumber?: string; email?: string; website?: string;
    description?: string; timeZoneId?: string;
  };
  step2?: { lat: number; lng: number };
  step3?: {
    hasDocks: boolean;
    dockCount: number;
    dockConvention: DockConvention;
    slipConvention: SlipConvention;
    slipsPerDock: number | number[];
    defaultMaxLength: number;
    defaultMaxBeam: number;
    defaultMaxDraft: number;
    defaultSlipType: string;
    defaultHasElectric: boolean;
    defaultHasWater: boolean;
    defaultHasPumpOut: boolean;
    defaultAmenities: string[];
  };
  generatedDocks?: Array<{ name: string; slips: Array<{ name: string }> }>;
}

// ─── Step 1 — Marina profile ──────────────────────────────────────────────────
const step1Schema = z.object({
  addressStreet:  z.string().optional(),
  addressCity:    z.string().min(1, 'City is required'),
  addressState:   z.string().min(1, 'State is required'),
  addressZip:     z.string().optional(),
  addressCountry: z.string().optional(),
  phoneNumber:    z.string().optional(),
  email:          z.string().email('Invalid email').or(z.literal('')).optional(),
  website:        z.string().optional(),
  description:    z.string().optional(),
  timeZoneId:     z.string().min(1, 'Timezone is required'),
});
type Step1Data = z.infer<typeof step1Schema>;

const US_TIMEZONES = [
  'America/New_York', 'America/Chicago', 'America/Denver', 'America/Los_Angeles',
  'America/Anchorage', 'Pacific/Honolulu',
];

function Step1({ marina, onNext }: { marina: MarinaDto; onNext: (data: Step1Data) => Promise<void> }) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<Step1Data>({
    resolver: zodResolver(step1Schema),
    defaultValues: {
      addressStreet: marina.addressStreet ?? '',
      addressCity:   marina.addressCity ?? '',
      addressState:  marina.addressState ?? '',
      addressZip:    marina.addressZip ?? '',
      addressCountry: marina.addressCountry ?? 'US',
      phoneNumber:   marina.phoneNumber ?? '',
      email:         marina.email ?? '',
      website:       marina.website ?? '',
      description:   marina.description ?? '',
      timeZoneId:    marina.timeZoneId ?? 'America/New_York',
    },
  });

  return (
    <form onSubmit={handleSubmit(onNext)} className="space-y-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="sm:col-span-2">
          <label className="block text-sm font-medium text-foreground mb-1">Street address</label>
          <input {...register('addressStreet')} type="text" placeholder="123 Harbor Way" className={input} />
        </div>
        <div>
          <label className="block text-sm font-medium text-foreground mb-1">City <span className="text-red-500">*</span></label>
          <input {...register('addressCity')} type="text" placeholder="Annapolis" className={input} />
          {errors.addressCity && <p className="mt-1 text-xs text-red-600">{errors.addressCity.message}</p>}
        </div>
        <div>
          <label className="block text-sm font-medium text-foreground mb-1">State <span className="text-red-500">*</span></label>
          <input {...register('addressState')} type="text" placeholder="MD" maxLength={2} className={input} />
          {errors.addressState && <p className="mt-1 text-xs text-red-600">{errors.addressState.message}</p>}
        </div>
        <div>
          <label className="block text-sm font-medium text-foreground mb-1">ZIP code</label>
          <input {...register('addressZip')} type="text" placeholder="21401" className={input} />
        </div>
        <div>
          <label className="block text-sm font-medium text-foreground mb-1">Timezone <span className="text-red-500">*</span></label>
          <select {...register('timeZoneId')} className={`${input} bg-card`}>
            {US_TIMEZONES.map((tz) => <option key={tz} value={tz}>{tz}</option>)}
          </select>
          {errors.timeZoneId && <p className="mt-1 text-xs text-red-600">{errors.timeZoneId.message}</p>}
        </div>
        <div>
          <label className="block text-sm font-medium text-foreground mb-1">Phone</label>
          <input {...register('phoneNumber')} type="tel" placeholder="(410) 555-0100" className={input} />
        </div>
        <div>
          <label className="block text-sm font-medium text-foreground mb-1">Email</label>
          <input {...register('email')} type="email" placeholder="info@mymarina.com" className={input} />
          {errors.email && <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>}
        </div>
        <div>
          <label className="block text-sm font-medium text-foreground mb-1">Website</label>
          <input {...register('website')} type="url" placeholder="https://mymarina.com" className={input} />
        </div>
        <div className="sm:col-span-2">
          <label className="block text-sm font-medium text-foreground mb-1">Description</label>
          <textarea {...register('description')} rows={3} placeholder="Tell boaters about your marina…" className={input} />
        </div>
      </div>
      <div className="flex justify-end pt-2">
        <button type="submit" disabled={isSubmitting} className={btn}>
          {isSubmitting ? 'Saving…' : 'Next: GPS location →'}
        </button>
      </div>
    </form>
  );
}

// ─── Step 2 — GPS location ────────────────────────────────────────────────────
function Step2({
  marina, onBack, onNext,
}: { marina: MarinaDto; onBack: () => void; onNext: (lat: number, lng: number) => Promise<void> }) {
  const [lat, setLat] = useState(marina.latitude ?? 38.9784);
  const [lng, setLng] = useState(marina.longitude ?? -76.4922);
  const [geocoding, setGeocoding] = useState(false);
  const [precisionLabel, setPrecisionLabel] = useState<string | null>(
    marina.latitude ? 'Loaded from saved location' : null
  );
  const [saving, setSaving] = useState(false);

  async function handleGeocode() {
    setGeocoding(true);
    setPrecisionLabel(null);
    const result = await nominatimGeocode({
      street:  marina.addressStreet ?? undefined,
      city:    marina.addressCity ?? undefined,
      state:   marina.addressState ?? undefined,
      zip:     marina.addressZip ?? undefined,
    });
    setGeocoding(false);
    if (result) {
      setLat(result.lat);
      setLng(result.lng);
      setPrecisionLabel(`Located to: ${result.precisionLabel}`);
    } else {
      setPrecisionLabel('Could not locate address — drag the pin manually.');
    }
  }

  async function handleNext() {
    setSaving(true);
    await onNext(lat, lng);
    setSaving(false);
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">
        Place your marina on the map. Boaters use this to find you in search results.
        Drag the pin to fine-tune the location.
      </p>
      <div className="flex items-center gap-3">
        <button
          type="button" onClick={handleGeocode} disabled={geocoding}
          className={btnSecondary}
        >
          {geocoding ? 'Locating…' : 'Locate on map from address'}
        </button>
        {precisionLabel && (
          <p className="text-sm text-muted-foreground">{precisionLabel}</p>
        )}
      </div>
      <div className="h-80 rounded-xl overflow-hidden border border-border">
        <MapPicker lat={lat} lng={lng} onPositionChange={(la, ln) => { setLat(la); setLng(ln); }} />
      </div>
      <div className="flex gap-4 text-sm text-muted-foreground">
        <span>Lat: {lat.toFixed(6)}</span>
        <span>Lng: {lng.toFixed(6)}</span>
      </div>
      <div className="flex justify-between pt-2">
        <button type="button" onClick={onBack} className={btnSecondary}>← Back</button>
        <button type="button" onClick={handleNext} disabled={saving} className={btn}>
          {saving ? 'Saving…' : 'Next: Dock structure →'}
        </button>
      </div>
    </div>
  );
}

// ─── Step 3 — Dock structure builder ─────────────────────────────────────────
const DOCK_CONVENTIONS: { value: DockConvention['kind']; label: string }[] = [
  { value: 'Lettered', label: 'Lettered (A, B, C…)' },
  { value: 'Numbered', label: 'Numbered (1, 2, 3…)' },
  { value: 'Manual',   label: 'Custom names' },
];

const SLIP_CONVENTIONS: { value: SlipConvention['kind']; label: string }[] = [
  { value: 'PerDockReset',  label: 'Per-dock, reset (A-1…A-10, B-1…B-10)' },
  { value: 'PerDockGlobal', label: 'Per-dock, global (A-1…A-10, B-11…B-20)' },
  { value: 'Sequential',    label: 'Sequential (1, 2, 3…)' },
  { value: 'Manual',        label: 'Custom names' },
];

interface Step3State {
  hasDocks: boolean;
  dockCount: number;
  dockConvKind: DockConvention['kind'];
  dockPrefix: string;
  slipConvKind: SlipConvention['kind'];
  slipSeparator: string;
  slipsPerDock: number;
  defaultMaxLength: number;
  defaultMaxBeam: number;
  defaultMaxDraft: number;
  defaultSlipType: string;
  defaultHasElectric: boolean;
  defaultHasWater: boolean;
  defaultHasPumpOut: boolean;
  customAmenityInput: string;
  amenities: string[];
}

function Step3({
  onBack, onNext,
}: { onBack: () => void; onNext: (docks: SetupDockData[]) => Promise<void> }) {
  const [s, setS] = useState<Step3State>({
    hasDocks: true,
    dockCount: 3,
    dockConvKind: 'Lettered',
    dockPrefix: 'Dock ',
    slipConvKind: 'PerDockReset',
    slipSeparator: '-',
    slipsPerDock: 10,
    defaultMaxLength: 40,
    defaultMaxBeam: 14,
    defaultMaxDraft: 5,
    defaultSlipType: 'Floating',
    defaultHasElectric: true,
    defaultHasWater: true,
    defaultHasPumpOut: false,
    customAmenityInput: '',
    amenities: [],
  });
  const [saving, setSaving] = useState(false);

  function upd(patch: Partial<Step3State>) { setS((prev) => ({ ...prev, ...patch })); }

  // Live preview of generated dock names
  const dockConvention: DockConvention =
    s.dockConvKind === 'Manual'
      ? { kind: 'Manual', names: [] }
      : s.dockConvKind === 'Lettered'
        ? { kind: 'Lettered', prefix: s.dockPrefix }
        : { kind: 'Numbered', prefix: s.dockPrefix };

  const dockNames = s.dockConvKind === 'Manual' ? [] : generateDockNames(s.dockCount, dockConvention);
  const previewNames = dockNames.slice(0, 4);

  async function handleNext() {
    if (!s.hasDocks) {
      await onNext([]);
      return;
    }
    const slipConvention: SlipConvention =
      s.slipConvKind === 'Manual'
        ? { kind: 'Manual', names: [] }
        : s.slipConvKind === 'Sequential'
          ? { kind: 'Sequential' }
          : s.slipConvKind === 'PerDockGlobal'
            ? { kind: 'PerDockGlobal', separator: s.slipSeparator }
            : { kind: 'PerDockReset', separator: s.slipSeparator };

    const generated = generateDockSlips({
      dockNames,
      slipsPerDock: s.slipsPerDock,
      convention: slipConvention,
    });

    const payload: SetupDockData[] = generated.map((d) => ({
      name: d.name,
      slips: d.slips.map((slipName) => ({
        name: slipName,
        maxLength: s.defaultMaxLength,
        maxBeam: s.defaultMaxBeam,
        maxDraft: s.defaultMaxDraft,
        slipType: s.defaultSlipType,
        hasElectric: s.defaultHasElectric,
        hasWater: s.defaultHasWater,
        hasPumpOut: s.defaultHasPumpOut,
        amenities: s.amenities,
      })),
    }));

    setSaving(true);
    await onNext(payload);
    setSaving(false);
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <label className="text-sm font-medium text-foreground">Does your marina have docks?</label>
        <div className="flex gap-2">
          {[true, false].map((v) => (
            <button
              key={String(v)} type="button"
              onClick={() => upd({ hasDocks: v })}
              className={`px-3 py-1.5 rounded-lg text-sm font-medium border transition-colors ${
                s.hasDocks === v ? 'bg-foreground text-background border-foreground' : 'border-border text-foreground/80 hover:bg-muted/30'
              }`}
            >
              {v ? 'Yes' : 'No (free-standing moorings)'}
            </button>
          ))}
        </div>
      </div>

      {s.hasDocks && (
        <>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 p-4 rounded-xl border border-border bg-muted/30">
            <h3 className="sm:col-span-2 text-sm font-semibold text-foreground">Dock setup</h3>
            <div>
              <label className="block text-xs font-medium text-foreground/80 mb-1">Number of docks</label>
              <input type="number" min={1} max={200} value={s.dockCount}
                onChange={(e) => upd({ dockCount: Number(e.target.value) })} className={input} />
            </div>
            <div>
              <label className="block text-xs font-medium text-foreground/80 mb-1">Dock naming</label>
              <select value={s.dockConvKind} onChange={(e) => upd({ dockConvKind: e.target.value as DockConvention['kind'] })}
                className={`${input} bg-card`}>
                {DOCK_CONVENTIONS.map((c) => <option key={c.value} value={c.value}>{c.label}</option>)}
              </select>
            </div>
            {s.dockConvKind !== 'Manual' && (
              <div>
                <label className="block text-xs font-medium text-foreground/80 mb-1">Prefix (optional)</label>
                <input type="text" value={s.dockPrefix} placeholder='e.g. "Dock "'
                  onChange={(e) => upd({ dockPrefix: e.target.value })} className={input} />
              </div>
            )}
            {dockNames.length > 0 && (
              <div className="sm:col-span-2">
                <p className="text-xs text-muted-foreground mb-1">Preview:</p>
                <p className="text-sm text-foreground">
                  {previewNames.join(', ')}{dockNames.length > 4 ? ` … (${dockNames.length} total)` : ''}
                </p>
              </div>
            )}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 p-4 rounded-xl border border-border bg-muted/30">
            <h3 className="sm:col-span-2 text-sm font-semibold text-foreground">Slip setup</h3>
            <div>
              <label className="block text-xs font-medium text-foreground/80 mb-1">Slips per dock</label>
              <input type="number" min={1} max={500} value={s.slipsPerDock}
                onChange={(e) => upd({ slipsPerDock: Number(e.target.value) })} className={input} />
            </div>
            <div>
              <label className="block text-xs font-medium text-foreground/80 mb-1">Slip naming</label>
              <select value={s.slipConvKind} onChange={(e) => upd({ slipConvKind: e.target.value as SlipConvention['kind'] })}
                className={`${input} bg-card`}>
                {SLIP_CONVENTIONS.map((c) => <option key={c.value} value={c.value}>{c.label}</option>)}
              </select>
            </div>
            {(s.slipConvKind === 'PerDockReset' || s.slipConvKind === 'PerDockGlobal') && (
              <div>
                <label className="block text-xs font-medium text-foreground/80 mb-1">Separator</label>
                <input type="text" value={s.slipSeparator} maxLength={3}
                  onChange={(e) => upd({ slipSeparator: e.target.value })} className={`${input} w-20`} />
              </div>
            )}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 p-4 rounded-xl border border-border bg-muted/30">
            <h3 className="sm:col-span-3 text-sm font-semibold text-foreground">Default slip dimensions & amenities</h3>
            <div>
              <label className="block text-xs font-medium text-foreground/80 mb-1">Max length (ft)</label>
              <input type="number" step="0.5" value={s.defaultMaxLength}
                onChange={(e) => upd({ defaultMaxLength: Number(e.target.value) })} className={input} />
            </div>
            <div>
              <label className="block text-xs font-medium text-foreground/80 mb-1">Max beam (ft)</label>
              <input type="number" step="0.5" value={s.defaultMaxBeam}
                onChange={(e) => upd({ defaultMaxBeam: Number(e.target.value) })} className={input} />
            </div>
            <div>
              <label className="block text-xs font-medium text-foreground/80 mb-1">Max draft (ft)</label>
              <input type="number" step="0.5" value={s.defaultMaxDraft}
                onChange={(e) => upd({ defaultMaxDraft: Number(e.target.value) })} className={input} />
            </div>
            <div>
              <label className="block text-xs font-medium text-foreground/80 mb-1">Slip type</label>
              <select value={s.defaultSlipType} onChange={(e) => upd({ defaultSlipType: e.target.value })}
                className={`${input} bg-card`}>
                {['Floating', 'Fixed', 'Mooring', 'DryStorage', 'Anchorage'].map((t) => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>
            </div>
            <div className="flex flex-col gap-2 justify-end">
              {[
                { key: 'defaultHasElectric', label: 'Electric' },
                { key: 'defaultHasWater',    label: 'Water' },
                { key: 'defaultHasPumpOut',  label: 'Pump-out' },
              ].map(({ key, label }) => (
                <label key={key} className="flex items-center gap-2 text-sm text-foreground cursor-pointer">
                  <input type="checkbox" checked={s[key as keyof Step3State] as boolean}
                    onChange={(e) => upd({ [key]: e.target.checked } as Partial<Step3State>)}
                    className="rounded" />
                  {label}
                </label>
              ))}
            </div>
            <div className="sm:col-span-3">
              <label className="block text-xs font-medium text-foreground/80 mb-1">Custom amenity tags</label>
              <div className="flex gap-2">
                <input type="text" value={s.customAmenityInput} placeholder="e.g. Fuel dock"
                  onChange={(e) => upd({ customAmenityInput: e.target.value })}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      e.preventDefault();
                      const tag = s.customAmenityInput.trim();
                      if (tag && !s.amenities.includes(tag)) upd({ amenities: [...s.amenities, tag], customAmenityInput: '' });
                    }
                  }}
                  className={`${input} flex-1`} />
                <button type="button" onClick={() => {
                  const tag = s.customAmenityInput.trim();
                  if (tag && !s.amenities.includes(tag)) upd({ amenities: [...s.amenities, tag], customAmenityInput: '' });
                }} className={btnSecondary}>Add</button>
              </div>
              {s.amenities.length > 0 && (
                <div className="flex flex-wrap gap-1.5 mt-2">
                  {s.amenities.map((a) => (
                    <span key={a} className="flex items-center gap-1 rounded-full bg-muted text-foreground text-xs px-2.5 py-0.5">
                      {a}
                      <button type="button" onClick={() => upd({ amenities: s.amenities.filter((x) => x !== a) })}
                        className="text-muted-foreground/70 hover:text-foreground">×</button>
                    </span>
                  ))}
                </div>
              )}
            </div>
          </div>
        </>
      )}

      <div className="flex justify-between pt-2">
        <button type="button" onClick={onBack} className={btnSecondary}>← Back</button>
        <button type="button" onClick={handleNext} disabled={saving} className={btn}>
          {saving ? 'Saving…' : 'Next: Preview →'}
        </button>
      </div>
    </div>
  );
}

// ─── Step 4 — Preview & adjust ────────────────────────────────────────────────
interface PreviewDock { id: string; name: string; slips: PreviewSlip[] }
interface PreviewSlip { id: string; name: string; maxLength: number; maxBeam: number; maxDraft: number }
type EditingCell = { slipId: string; field: 'name' | 'maxLength' | 'maxBeam' | 'maxDraft'; value: string };
interface BulkEdit { dockId: string; maxLength: string; maxBeam: string; maxDraft: string }
interface RangeEdit { maxLength: string; maxBeam: string; maxDraft: string }

function Step4({
  marinaId, onBack, onNext,
}: { marinaId: string; onBack: () => void; onNext: () => void }) {
  const [docks, setDocks] = useState<PreviewDock[]>([]);
  const [loading, setLoading] = useState(true);
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());
  const [editing, setEditing] = useState<EditingCell | null>(null);
  const [bulkEdit, setBulkEdit] = useState<BulkEdit | null>(null);
  const [selectedSlips, setSelectedSlips] = useState<Set<string>>(new Set());
  const [lastClickedSlipId, setLastClickedSlipId] = useState<string | null>(null);
  const [rangeEdit, setRangeEdit] = useState<RangeEdit | null>(null);

  function reload() {
    return Promise.all([getDocks(marinaId), getSlips(marinaId)]).then(([ds, ss]) => {
      setDocks(ds.map((d) => ({
        id: d.id,
        name: d.name,
        slips: ss
          .filter((s) => s.dockId === d.id)
          .sort((a, b) => a.name.localeCompare(b.name))
          .map((s) => ({ id: s.id, name: s.name, maxLength: s.maxLength, maxBeam: s.maxBeam, maxDraft: s.maxDraft })),
      })));
    });
  }

  useEffect(() => { reload().finally(() => setLoading(false)); }, [marinaId]);

  async function commitEdit(cell: EditingCell) {
    const dock = docks.find((d) => d.slips.some((s) => s.id === cell.slipId));
    if (!dock) return;
    const slip = dock.slips.find((s) => s.id === cell.slipId)!;
    const patch: Record<string, string | number> = {};
    if (cell.field === 'name') patch.name = cell.value;
    else patch[cell.field] = parseFloat(cell.value) || slip[cell.field];

    await updateSlip(marinaId, cell.slipId, patch);
    setDocks((prev) => prev.map((d) => ({
      ...d,
      slips: d.slips.map((s) => s.id === cell.slipId
        ? { ...s, ...patch, [cell.field]: patch[cell.field] }
        : s
      ),
    })));
    setEditing(null);
  }

  async function handleRemoveSlip(slipId: string) {
    await deleteSlip(marinaId, slipId);
    setDocks((prev) => prev.map((d) => ({ ...d, slips: d.slips.filter((s) => s.id !== slipId) })));
  }

  async function handleAddSlip(dock: PreviewDock) {
    const nextNum = dock.slips.length + 1;
    const newName = `${dock.name}-${nextNum}`;
    const created = await createSlip(marinaId, {
      dockId: dock.id, name: newName,
      maxLength: 40, maxBeam: 14, maxDraft: 5,
      hasElectric: false, hasWater: false,
    });
    setDocks((prev) => prev.map((d) => d.id === dock.id
      ? { ...d, slips: [...d.slips, { id: created.id, name: created.name, maxLength: created.maxLength, maxBeam: created.maxBeam, maxDraft: created.maxDraft }] }
      : d
    ));
  }

  async function commitBulkEdit() {
    if (!bulkEdit) return;
    const dock = docks.find((d) => d.id === bulkEdit.dockId);
    if (!dock) return;
    const patch = {
      maxLength: parseFloat(bulkEdit.maxLength),
      maxBeam: parseFloat(bulkEdit.maxBeam),
      maxDraft: parseFloat(bulkEdit.maxDraft),
    };
    await Promise.all(dock.slips.map((s) => updateSlip(marinaId, s.id, patch)));
    setDocks((prev) => prev.map((d) => d.id === bulkEdit.dockId
      ? { ...d, slips: d.slips.map((s) => ({ ...s, ...patch })) }
      : d
    ));
    setBulkEdit(null);
  }

  function allSlipIds(): string[] {
    return docks.flatMap((d) => d.slips.map((s) => s.id));
  }

  function handleRowClick(slipId: string, e: React.MouseEvent) {
    if (e.shiftKey && lastClickedSlipId) {
      const ids = allSlipIds();
      const a = ids.indexOf(lastClickedSlipId);
      const b = ids.indexOf(slipId);
      const [lo, hi] = a <= b ? [a, b] : [b, a];
      const rangeIds = ids.slice(lo, hi + 1);
      setSelectedSlips((prev) => {
        const next = new Set(prev);
        rangeIds.forEach((id) => next.add(id));
        return next;
      });
    } else {
      setSelectedSlips((prev) => {
        const next = new Set(prev);
        if (next.has(slipId)) next.delete(slipId); else next.add(slipId);
        return next;
      });
      setLastClickedSlipId(slipId);
    }
  }

  async function commitRangeEdit() {
    if (!rangeEdit || selectedSlips.size === 0) return;
    const patch: Record<string, number> = {};
    if (rangeEdit.maxLength !== '') patch.maxLength = parseFloat(rangeEdit.maxLength);
    if (rangeEdit.maxBeam !== '') patch.maxBeam = parseFloat(rangeEdit.maxBeam);
    if (rangeEdit.maxDraft !== '') patch.maxDraft = parseFloat(rangeEdit.maxDraft);
    if (Object.keys(patch).length === 0) return;
    await Promise.all([...selectedSlips].map((id) => updateSlip(marinaId, id, patch)));
    setDocks((prev) => prev.map((d) => ({
      ...d,
      slips: d.slips.map((s) => selectedSlips.has(s.id) ? { ...s, ...patch } : s),
    })));
    setSelectedSlips(new Set());
    setLastClickedSlipId(null);
    setRangeEdit(null);
  }

  if (loading) return <p className="text-sm text-muted-foreground/70">Loading…</p>;

  const totalSlips = docks.reduce((acc, d) => acc + d.slips.length, 0);

  function editableCell(slip: PreviewSlip, field: EditingCell['field'], display: string) {
    const isActive = editing?.slipId === slip.id && editing.field === field;
    if (isActive) {
      return (
        <input
          autoFocus
          type={field === 'name' ? 'text' : 'number'}
          step={field !== 'name' ? '0.5' : undefined}
          value={editing.value}
          onChange={(e) => setEditing({ ...editing, value: e.target.value })}
          onBlur={() => commitEdit(editing!)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') commitEdit(editing!);
            if (e.key === 'Escape') setEditing(null);
          }}
          className="w-full rounded border border-blue-400 px-1 py-0.5 text-xs outline-none"
        />
      );
    }
    return (
      <span
        className="cursor-pointer hover:bg-blue-50 rounded px-1 py-0.5 transition-colors"
        title="Click to edit"
        onClick={(e) => { e.stopPropagation(); setEditing({ slipId: slip.id, field, value: String(slip[field]) }); }}
      >
        {display}
      </span>
    );
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">
        Review your {docks.length} dock{docks.length !== 1 ? 's' : ''} and {totalSlips} slip{totalSlips !== 1 ? 's' : ''}.
        Click any cell to edit it inline. Click a row to select it; shift+click to select a range.
      </p>

      <div className="space-y-3">
        {docks.map((dock) => {
          const isCollapsed = collapsed.has(dock.id);
          const isBulkOpen = bulkEdit?.dockId === dock.id;

          return (
            <div key={dock.id} className="rounded-xl border border-border overflow-hidden">
              {/* Dock header */}
              <div className="flex items-center justify-between px-4 py-3 bg-muted/30">
                <button
                  type="button"
                  onClick={() => setCollapsed((prev) => {
                    const next = new Set(prev);
                    if (next.has(dock.id)) next.delete(dock.id); else next.add(dock.id);
                    return next;
                  })}
                  className="flex items-center gap-2 text-sm font-semibold text-foreground hover:text-foreground"
                >
                  <span>{isCollapsed ? '▸' : '▾'}</span>
                  <span>{dock.name}</span>
                  <span className="text-xs font-normal text-muted-foreground/70">({dock.slips.length} slips)</span>
                </button>
                <button
                  type="button"
                  onClick={() => setBulkEdit(isBulkOpen ? null : {
                    dockId: dock.id,
                    maxLength: String(dock.slips[0]?.maxLength ?? 40),
                    maxBeam:   String(dock.slips[0]?.maxBeam ?? 14),
                    maxDraft:  String(dock.slips[0]?.maxDraft ?? 5),
                  })}
                  className="text-xs text-muted-foreground hover:text-foreground underline"
                >
                  {isBulkOpen ? 'Cancel bulk edit' : 'Edit all slips'}
                </button>
              </div>

              {/* Bulk edit panel */}
              {isBulkOpen && (
                <div className="flex items-end gap-3 px-4 py-3 bg-blue-50 border-b border-blue-100">
                  <p className="text-xs font-medium text-blue-700 mr-1">Apply to all slips in {dock.name}:</p>
                  {(['maxLength', 'maxBeam', 'maxDraft'] as const).map((f) => (
                    <div key={f}>
                      <label className="block text-xs text-blue-600 mb-0.5">{f === 'maxLength' ? 'Length' : f === 'maxBeam' ? 'Beam' : 'Draft'} (ft)</label>
                      <input
                        type="number" step="0.5" value={bulkEdit![f]}
                        onChange={(e) => setBulkEdit({ ...bulkEdit!, [f]: e.target.value })}
                        className="w-20 rounded border border-blue-300 px-2 py-1 text-xs focus:outline-none"
                      />
                    </div>
                  ))}
                  <button type="button" onClick={commitBulkEdit} className="rounded-lg bg-blue-600 text-white text-xs px-3 py-1.5 hover:bg-blue-700 transition-colors">
                    Apply
                  </button>
                </div>
              )}

              {/* Slip table */}
              {!isCollapsed && (
                <div className="overflow-x-auto">
                  <table className="w-full text-xs">
                    <thead className="bg-muted/30 border-b border-border">
                      <tr>
                        <th className="text-left px-4 py-2 font-medium text-muted-foreground">Slip</th>
                        <th className="text-right px-4 py-2 font-medium text-muted-foreground">Length</th>
                        <th className="text-right px-4 py-2 font-medium text-muted-foreground">Beam</th>
                        <th className="text-right px-4 py-2 font-medium text-muted-foreground">Draft</th>
                        <th className="px-4 py-2" />
                      </tr>
                    </thead>
                    <tbody>
                      {dock.slips.map((slip) => {
                        const isSelected = selectedSlips.has(slip.id);
                        return (
                          <tr
                            key={slip.id}
                            onClick={(e) => handleRowClick(slip.id, e)}
                            className={`border-b border-border/50 last:border-0 cursor-pointer select-none ${
                              isSelected ? 'bg-blue-50' : 'hover:bg-muted/30'
                            }`}
                          >
                            <td className="px-4 py-2">{editableCell(slip, 'name', slip.name)}</td>
                            <td className="px-4 py-2 text-right">{editableCell(slip, 'maxLength', `${slip.maxLength}′`)}</td>
                            <td className="px-4 py-2 text-right">{editableCell(slip, 'maxBeam', `${slip.maxBeam}′`)}</td>
                            <td className="px-4 py-2 text-right">{editableCell(slip, 'maxDraft', `${slip.maxDraft}′`)}</td>
                            <td className="px-4 py-2 text-right">
                              <button
                                type="button"
                                onClick={(e) => { e.stopPropagation(); handleRemoveSlip(slip.id); }}
                                className="text-red-400 hover:text-red-600 text-xs"
                                title="Remove slip"
                              >
                                ×
                              </button>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                  <div className="px-4 py-2 border-t border-border/50">
                    <button
                      type="button"
                      onClick={() => handleAddSlip(dock)}
                      className="text-xs text-muted-foreground hover:text-foreground underline"
                    >
                      + Add slip
                    </button>
                  </div>
                </div>
              )}
            </div>
          );
        })}

        {docks.length === 0 && (
          <p className="text-sm text-muted-foreground/70 text-center py-8">No docks configured.</p>
        )}
      </div>

      {/* Sticky bulk-selection bar */}
      {selectedSlips.size > 0 && (
        <div className="sticky bottom-4 rounded-xl border border-blue-200 bg-card shadow-lg px-4 py-3 flex items-center gap-3 flex-wrap">
          <span className="text-sm font-medium text-blue-700 shrink-0">
            {selectedSlips.size} slip{selectedSlips.size !== 1 ? 's' : ''} selected
          </span>
          {rangeEdit ? (
            <>
              {(['maxLength', 'maxBeam', 'maxDraft'] as const).map((f) => (
                <div key={f} className="flex items-center gap-1">
                  <label className="text-xs text-muted-foreground">{f === 'maxLength' ? 'Length' : f === 'maxBeam' ? 'Beam' : 'Draft'}</label>
                  <input
                    type="number" step="0.5" placeholder="—"
                    value={rangeEdit[f]}
                    onChange={(e) => setRangeEdit({ ...rangeEdit, [f]: e.target.value })}
                    className="w-20 rounded border border-border px-2 py-1 text-xs focus:outline-none focus:ring-1 focus:ring-blue-400"
                  />
                </div>
              ))}
              <button type="button" onClick={commitRangeEdit}
                className="rounded-lg bg-blue-600 text-white text-xs px-3 py-1.5 hover:bg-blue-700 transition-colors">
                Apply to {selectedSlips.size} slips
              </button>
              <button type="button" onClick={() => setRangeEdit(null)}
                className="text-xs text-muted-foreground/70 hover:text-foreground/80">
                Cancel
              </button>
            </>
          ) : (
            <>
              <button type="button"
                onClick={() => setRangeEdit({ maxLength: '', maxBeam: '', maxDraft: '' })}
                className="rounded-lg bg-blue-600 text-white text-xs px-3 py-1.5 hover:bg-blue-700 transition-colors">
                Edit dimensions
              </button>
              <button type="button"
                onClick={() => { setSelectedSlips(new Set()); setLastClickedSlipId(null); }}
                className="text-xs text-muted-foreground/70 hover:text-foreground/80">
                Clear selection
              </button>
            </>
          )}
        </div>
      )}

      <div className="flex justify-between pt-2">
        <button type="button" onClick={onBack} className={btnSecondary}>← Back</button>
        <button type="button" onClick={onNext} className={btn}>Next: Publish →</button>
      </div>
    </div>
  );
}

// ─── Step 5 — Photos ──────────────────────────────────────────────────────────
function Step5Photos({
  marinaId, onBack, onNext, onSkip,
}: { marinaId: string; onBack: () => void; onNext: () => void; onSkip: () => void }) {
  const { upload } = usePhotoUpload(marinaId);
  const [logoUploaded, setLogoUploaded] = useState(false);
  const [bannerUploaded, setBannerUploaded] = useState(false);
  const [showLogoCrop, setShowLogoCrop] = useState(false);
  const [showBannerCrop, setShowBannerCrop] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleLogoComplete(blob: Blob, width: number, height: number) {
    setUploading(true);
    setError(null);
    try {
      await upload({ kind: 'Logo', file: blob, contentType: 'image/jpeg', imageWidth: width, imageHeight: height });
      setLogoUploaded(true);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Upload failed');
    } finally {
      setUploading(false);
      setShowLogoCrop(false);
    }
  }

  async function handleBannerComplete(blob: Blob, width: number, height: number) {
    setUploading(true);
    setError(null);
    try {
      await upload({ kind: 'Banner', file: blob, contentType: 'image/jpeg', imageWidth: width, imageHeight: height });
      setBannerUploaded(true);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Upload failed');
    } finally {
      setUploading(false);
      setShowBannerCrop(false);
    }
  }

  return (
    <div className="space-y-6">
      {/* Encouragement */}
      <div className="rounded-xl bg-primary/5 border border-primary/20 px-5 py-4">
        <p className="text-sm font-medium text-foreground">Marinas with photos receive significantly more inquiries</p>
        <p className="text-xs text-muted-foreground mt-1">
          A logo and banner help your marina stand out on the marketplace. You can add more photos from your dashboard anytime.
        </p>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {/* Logo slot */}
      <div className="rounded-xl border border-border p-5 space-y-3">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm font-medium text-foreground">Logo</p>
            <p className="text-xs text-muted-foreground">Square image, 1:1 ratio. Appears as your marina's avatar.</p>
          </div>
          {logoUploaded && <span className="text-xs text-emerald-600 font-medium">✓ Uploaded</span>}
        </div>
        <button
          type="button"
          disabled={uploading}
          onClick={() => setShowLogoCrop(true)}
          className={btnSecondary}
        >
          {logoUploaded ? 'Replace logo' : 'Upload logo'}
        </button>
      </div>

      {/* Banner slot */}
      <div className="rounded-xl border border-border p-5 space-y-3">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm font-medium text-foreground">Banner</p>
            <p className="text-xs text-muted-foreground">Wide 16:9 image. Displayed at the top of your marina page.</p>
          </div>
          {bannerUploaded && <span className="text-xs text-emerald-600 font-medium">✓ Uploaded</span>}
        </div>
        <button
          type="button"
          disabled={uploading}
          onClick={() => setShowBannerCrop(true)}
          className={btnSecondary}
        >
          {bannerUploaded ? 'Replace banner' : 'Upload banner'}
        </button>
      </div>

      <div className="flex justify-between pt-2 items-center">
        <button type="button" onClick={onBack} className={btnSecondary}>← Back</button>
        <div className="flex flex-col items-end gap-2">
          <button type="button" onClick={onNext} disabled={uploading} className={btn}>
            Next →
          </button>
          <button type="button" onClick={onSkip} className="text-xs text-muted-foreground/70 hover:text-foreground/80 underline">
            Skip for now — add photos later from your marina settings
          </button>
        </div>
      </div>

      {showLogoCrop && (
        <CropUploadModal aspectRatio={1} onComplete={handleLogoComplete} onCancel={() => setShowLogoCrop(false)} />
      )}
      {showBannerCrop && (
        <CropUploadModal aspectRatio={16 / 9} onComplete={handleBannerComplete} onCancel={() => setShowBannerCrop(false)} />
      )}
    </div>
  );
}

// ─── Step 6 — Review & publish ────────────────────────────────────────────────
function Step6({
  marina, onBack, onFinish,
}: { marina: MarinaDto; onBack: () => void; onFinish: (isListed: boolean) => Promise<void> }) {
  const [isListed, setIsListed] = useState(false);
  const [saving, setSaving] = useState(false);

  async function handleFinish() {
    setSaving(true);
    await onFinish(isListed);
    setSaving(false);
  }

  return (
    <div className="space-y-6">
      <div className="rounded-xl border border-border bg-muted/30 p-5 space-y-2">
        <h3 className="text-sm font-semibold text-foreground">Summary</h3>
        <p className="text-sm text-foreground/80"><span className="font-medium">Marina:</span> {marina.name}</p>
        <p className="text-sm text-foreground/80"><span className="font-medium">Type:</span> {marina.marinaType}</p>
        {marina.addressCity && (
          <p className="text-sm text-foreground/80">
            <span className="font-medium">Location:</span> {[marina.addressCity, marina.addressState].filter(Boolean).join(', ')}
          </p>
        )}
        {marina.latitude && (
          <p className="text-sm text-foreground/80">
            <span className="font-medium">GPS:</span> {marina.latitude.toFixed(5)}, {marina.longitude?.toFixed(5)}
          </p>
        )}
      </div>

      <div className="rounded-xl border border-border p-5 space-y-3">
        <h3 className="text-sm font-semibold text-foreground">Marketplace listing</h3>
        <label className="flex items-start gap-3 cursor-pointer">
          <input type="checkbox" checked={isListed} onChange={(e) => setIsListed(e.target.checked)}
            className="mt-0.5 rounded" />
          <div>
            <p className="text-sm font-medium text-foreground">List on marketplace immediately</p>
            <p className="text-xs text-muted-foreground mt-0.5">
              When listed, boaters can find and book your slips right away.
              You can always update this later from your dashboard.
            </p>
          </div>
        </label>
        {isListed && (
          <div className="rounded-lg bg-amber-50 border border-amber-200 px-4 py-3 text-sm text-amber-800">
            Your marina will be publicly visible as soon as you click "Finish setup."
            Make sure your slip rates are configured before accepting bookings.
          </div>
        )}
      </div>

      <div className="flex justify-between pt-2">
        <button type="button" onClick={onBack} className={btnSecondary}>← Back</button>
        <button type="button" onClick={handleFinish} disabled={saving} className={btn}>
          {saving ? 'Finishing…' : 'Finish setup →'}
        </button>
      </div>
    </div>
  );
}

// ─── Wizard shell ─────────────────────────────────────────────────────────────
const STEPS = ['Marina profile', 'GPS location', 'Dock structure', 'Preview', 'Photos', 'Publish'];

export function MarinaSetupWizardPage() {
  const marinaId = window.location.pathname.split('/')[2];
  const [marina, setMarina] = useState<MarinaDto | null>(null);
  const [step, setStep] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const { setDraft, clearDraft } = useWizardDraft<WizardDraft>(marinaId, {});

  useEffect(() => {
    getMarina(marinaId)
      .then((m) => { setMarina(m); setStep(Math.max(1, m.setupStep + 1)); })
      .catch(() => setError('Marina not found.'))
      .finally(() => setLoading(false));
  }, [marinaId]);

  async function handleStep1(data: Step1Data) {
    const updated = await updateMarina(marinaId, { ...data, setupStep: 1 });
    setMarina(updated);
    setDraft((d) => ({ ...d, step1: data }));
    setStep(2);
  }

  async function handleStep2(lat: number, lng: number) {
    const updated = await updateMarina(marinaId, { latitude: lat, longitude: lng, setupStep: 2 });
    setMarina(updated);
    setDraft((d) => ({ ...d, step2: { lat, lng } }));
    setStep(3);
  }

  async function handleStep3(docks: SetupDockData[]) {
    if (docks.length > 0) {
      await setupDocks(marinaId, docks);
    } else {
      await updateMarina(marinaId, { setupStep: 3 });
    }
    if (marina) setMarina({ ...marina, setupStep: 3 });
    setStep(4);
  }

  async function handleFinish(isListed: boolean) {
    await updateMarina(marinaId, { isSetupComplete: true, isListed, setupStep: 6 });
    clearDraft();
    window.location.href = `/marina/${marinaId}`;
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-muted/30">
        <NavBar />
        <div className="flex items-center justify-center py-20">
          <p className="text-muted-foreground/70">Loading…</p>
        </div>
      </div>
    );
  }

  if (error || !marina) {
    return (
      <div className="min-h-screen bg-muted/30">
        <NavBar />
        <div className="flex items-center justify-center py-20">
          <p className="text-red-600">{error ?? 'Unknown error'}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-muted/30">
      <NavBar />
      <div className="max-w-3xl mx-auto py-10 px-4">
        {/* Header */}
        <div className="mb-8">
          <p className="text-xs font-semibold text-muted-foreground/70 uppercase tracking-wide mb-1">
            Setting up · {marina.name}
          </p>
          <h1 className="text-2xl font-semibold text-foreground">{STEPS[step - 1]}</h1>

          {/* Step progress bar */}
          <div className="flex gap-1.5 mt-4">
            {STEPS.map((label, i) => (
              <div key={label} className="flex-1 flex flex-col gap-1">
                <div className={`h-1.5 rounded-full transition-colors ${
                  i + 1 < step ? 'bg-foreground' : i + 1 === step ? 'bg-primary' : 'bg-muted'
                }`} />
                <span className="text-xs text-muted-foreground/70 truncate hidden sm:block">{label}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Save progress button */}
        <div className="mb-6 flex justify-end">
          <button
            type="button"
            onClick={async () => {
              await updateMarina(marinaId, { setupStep: step - 1 });
              const toast = document.createElement('div');
              toast.textContent = 'Progress saved';
              toast.className = 'fixed bottom-4 right-4 rounded-lg bg-primary text-primary-foreground text-sm px-4 py-2 shadow-lg z-50';
              document.body.appendChild(toast);
              setTimeout(() => toast.remove(), 2000);
            }}
            className="text-xs text-muted-foreground/70 hover:text-foreground/80 underline"
          >
            Save progress
          </button>
        </div>

        {/* Step content */}
        <div className="bg-card rounded-2xl border border-border shadow-sm p-6 sm:p-8">
          {step === 1 && <Step1 marina={marina} onNext={handleStep1} />}
          {step === 2 && <Step2 marina={marina} onBack={() => setStep(1)} onNext={handleStep2} />}
          {step === 3 && <Step3 onBack={() => setStep(2)} onNext={handleStep3} />}
          {step === 4 && <Step4 marinaId={marinaId} onBack={() => setStep(3)} onNext={() => setStep(5)} />}
          {step === 5 && (
            <Step5Photos
              marinaId={marinaId}
              onBack={() => setStep(4)}
              onNext={async () => { await updateMarina(marinaId, { setupStep: 5 }); setStep(6); }}
              onSkip={async () => { await updateMarina(marinaId, { setupStep: 5 }); setStep(6); }}
            />
          )}
          {step === 6 && <Step6 marina={marina} onBack={() => setStep(5)} onFinish={handleFinish} />}
        </div>
      </div>
    </div>
  );
}
