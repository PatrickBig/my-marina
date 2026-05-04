import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  type BoatType,
  type VesselDto,
  type CreateVesselData,
  getVessels,
  createVessel,
  updateVessel,
  archiveVessel,
} from '@/api/api';
import { NavBar } from '@/components/NavBar';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

const BOAT_TYPES: BoatType[] = ['Sailboat', 'Powerboat', 'Catamaran', 'Dinghy', 'Pwc', 'Other'];
const BOAT_TYPE_LABELS: Record<BoatType, string> = {
  Sailboat: 'Sailboat', Powerboat: 'Powerboat', Catamaran: 'Catamaran',
  Dinghy: 'Dinghy', Pwc: 'Personal Watercraft (PWC)', Other: 'Other',
};

const vesselSchema = z.object({
  name:               z.string().min(1, 'Name is required'),
  make:               z.string().optional(),
  model:              z.string().optional(),
  year:               z.coerce.number().int().min(1900).max(new Date().getFullYear() + 1).optional().or(z.literal('')),
  length:             z.coerce.number().positive('Length must be > 0'),
  beam:               z.coerce.number().positive('Beam must be > 0'),
  draft:              z.coerce.number().positive('Draft must be > 0'),
  boatType:           z.enum(['Sailboat', 'Powerboat', 'Catamaran', 'Dinghy', 'Pwc', 'Other'] as const),
  hullColor:          z.string().optional(),
  registrationNumber: z.string().optional(),
  registrationState:  z.string().optional(),
});

type VesselFormValues = z.infer<typeof vesselSchema>;

function toFormValues(v: VesselDto): VesselFormValues {
  return {
    name: v.name, make: v.make ?? '', model: v.model ?? '',
    year: v.year ?? '', length: v.length, beam: v.beam, draft: v.draft,
    boatType: v.boatType, hullColor: v.hullColor ?? '',
    registrationNumber: v.registrationNumber ?? '',
    registrationState: v.registrationState ?? '',
  };
}

function VesselForm({
  initial,
  onSave,
  onCancel,
}: {
  initial?: VesselDto;
  onSave: () => void;
  onCancel: () => void;
}) {
  const [serverError, setServerError] = useState<string | null>(null);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<VesselFormValues>({
      resolver: zodResolver(vesselSchema) as any,
      defaultValues: initial ? toFormValues(initial) : { boatType: 'Powerboat', length: 0, beam: 0, draft: 0 },
    });

  const onSubmit = async (values: VesselFormValues) => {
    setServerError(null);
    const data: CreateVesselData = {
      name:               values.name,
      make:               values.make || null,
      model:              values.model || null,
      year:               values.year ? Number(values.year) : null,
      length:             values.length,
      beam:               values.beam,
      draft:              values.draft,
      boatType:           values.boatType,
      hullColor:          values.hullColor || null,
      registrationNumber: values.registrationNumber || null,
      registrationState:  values.registrationState || null,
    };
    try {
      if (initial) {
        await updateVessel(initial.id, data);
      } else {
        await createVessel(data);
      }
      onSave();
    } catch {
      setServerError('Something went wrong. Please try again.');
    }
  };

  return (
    <Card className="mb-6">
      <CardHeader>
        <CardTitle className="text-base">{initial ? 'Edit boat' : 'Add a boat'}</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="col-span-2 space-y-1">
              <Label htmlFor="name">Boat name *</Label>
              <Input id="name" {...register('name')} />
              {errors.name && <p className="text-xs text-red-600">{errors.name.message}</p>}
            </div>

            <div className="space-y-1">
              <Label htmlFor="make">Make</Label>
              <Input id="make" placeholder="e.g. Grady-White" {...register('make')} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="model">Model</Label>
              <Input id="model" placeholder="e.g. Freedom 307" {...register('model')} />
            </div>

            <div className="space-y-1">
              <Label htmlFor="year">Year</Label>
              <Input id="year" type="number" placeholder="e.g. 2019" {...register('year')} />
              {errors.year && <p className="text-xs text-red-600">{errors.year.message}</p>}
            </div>
            <div className="space-y-1">
              <Label htmlFor="boatType">Type *</Label>
              <select
                id="boatType"
                className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm"
                {...register('boatType')}
              >
                {BOAT_TYPES.map((t) => (
                  <option key={t} value={t}>{BOAT_TYPE_LABELS[t]}</option>
                ))}
              </select>
            </div>

            <div className="space-y-1">
              <Label htmlFor="length">Length (ft) *</Label>
              <Input id="length" type="number" step="0.1" {...register('length')} />
              {errors.length && <p className="text-xs text-red-600">{errors.length.message}</p>}
            </div>
            <div className="space-y-1">
              <Label htmlFor="beam">Beam (ft) *</Label>
              <Input id="beam" type="number" step="0.1" {...register('beam')} />
              {errors.beam && <p className="text-xs text-red-600">{errors.beam.message}</p>}
            </div>
            <div className="space-y-1">
              <Label htmlFor="draft">Draft (ft) *</Label>
              <Input id="draft" type="number" step="0.1" {...register('draft')} />
              {errors.draft && <p className="text-xs text-red-600">{errors.draft.message}</p>}
            </div>
            <div className="space-y-1">
              <Label htmlFor="hullColor">Hull color</Label>
              <Input id="hullColor" placeholder="e.g. White" {...register('hullColor')} />
            </div>

            <div className="space-y-1">
              <Label htmlFor="registrationNumber">Registration #</Label>
              <Input id="registrationNumber" {...register('registrationNumber')} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="registrationState">State</Label>
              <Input id="registrationState" placeholder="e.g. FL" maxLength={2} {...register('registrationState')} />
            </div>
          </div>

          {serverError && <p className="text-sm text-red-600">{serverError}</p>}

          <div className="flex gap-2">
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Saving…' : initial ? 'Save changes' : 'Add boat'}
            </Button>
            <Button type="button" variant="outline" onClick={onCancel}>
              Cancel
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}

function VesselCard({ vessel, onEdit, onArchive }: {
  vessel: VesselDto;
  onEdit: () => void;
  onArchive: () => void;
}) {
  return (
    <Card>
      <CardContent className="pt-4">
        <div className="flex items-start justify-between">
          <div>
            <p className="font-medium text-slate-800">{vessel.name}</p>
            <p className="text-sm text-slate-500">
              {[vessel.make, vessel.model, vessel.year].filter(Boolean).join(' ')}
              {vessel.make || vessel.model || vessel.year ? ' · ' : ''}
              {BOAT_TYPE_LABELS[vessel.boatType]}
            </p>
            <p className="text-xs text-slate-400 mt-1">
              {vessel.length}ft LOA · {vessel.beam}ft beam · {vessel.draft}ft draft
            </p>
          </div>
          <div className="flex gap-2">
            <Button size="sm" variant="outline" onClick={onEdit}>Edit</Button>
            <Button size="sm" variant="outline" onClick={onArchive}
                    className="text-red-600 hover:text-red-700">
              Archive
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

export function MyBoatsPage() {
  const [vessels, setVessels] = useState<VesselDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingVessel, setEditingVessel] = useState<VesselDto | null>(null);

  async function loadVessels() {
    try {
      const data = await getVessels();
      setVessels(data);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { loadVessels(); }, []);

  async function handleArchive(id: string) {
    if (!confirm('Archive this boat? It will no longer appear in your list.')) return;
    await archiveVessel(id);
    await loadVessels();
  }

  function handleSaved() {
    setShowForm(false);
    setEditingVessel(null);
    loadVessels();
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <NavBar />
      <div className="max-w-4xl mx-auto py-10 px-4">
        <div className="flex items-center justify-between mb-6">
          <h1 className="text-xl font-semibold text-slate-800">My Boats</h1>
          {!showForm && !editingVessel && (
            <Button onClick={() => setShowForm(true)}>Add a boat</Button>
          )}
        </div>

        {(showForm && !editingVessel) && (
          <VesselForm
            onSave={handleSaved}
            onCancel={() => setShowForm(false)}
          />
        )}

        {editingVessel && (
          <VesselForm
            initial={editingVessel}
            onSave={handleSaved}
            onCancel={() => setEditingVessel(null)}
          />
        )}

        {loading ? (
          <p className="text-slate-500 text-sm">Loading…</p>
        ) : vessels.length === 0 ? (
          <p className="text-slate-500 text-sm">
            No boats yet.{' '}
            <button className="underline" onClick={() => setShowForm(true)}>
              Add your first boat
            </button>
            .
          </p>
        ) : (
          <div className="space-y-3">
            {vessels.map((v) => (
              <VesselCard
                key={v.id}
                vessel={v}
                onEdit={() => { setShowForm(false); setEditingVessel(v); }}
                onArchive={() => handleArchive(v.id)}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
