import { useEffect, useState } from 'react';
import { useParams, useNavigate, useRouter, useSearch } from '@tanstack/react-router';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  getMarina, updateMarina,
  type MarinaDto,
} from '@/api/api';
import { usePhotoUpload, type MarinaPhotoDto } from '@/hooks/usePhotoUpload';
import { CropUploadModal } from '@/components/CropUploadModal';
import { PhotoCard } from '@/components/PhotoCard';
import { PageHeader, PageBody } from '@/marina-workspace/PageHeader';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

// ─── Types ────────────────────────────────────────────────────────────────────

type SettingsTab = 'profile' | 'address' | 'photos' | 'subscription';

const TABS: { key: SettingsTab; label: string }[] = [
  { key: 'profile', label: 'Profile' },
  { key: 'address', label: 'Address & Location' },
  { key: 'photos', label: 'Photos' },
  { key: 'subscription', label: 'Subscription' },
];

const inp = 'w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-ring';

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1">
      <label className="text-sm font-medium">{label}</label>
      {children}
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  );
}

// ─── Profile tab ──────────────────────────────────────────────────────────────

const profileSchema = z.object({
  name: z.string().min(2, 'Required'),
  phoneNumber: z.string().optional(),
  email: z.string().email('Invalid email').optional().or(z.literal('')),
  website: z.string().optional(),
  description: z.string().optional(),
});
type ProfileFormData = z.infer<typeof profileSchema>;

function ProfileTab({ marina, onSaved }: { marina: MarinaDto; onSaved: (m: MarinaDto) => void }) {
  const { register, handleSubmit, formState: { errors, isSubmitting, isDirty } } = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema) as never,
    defaultValues: {
      name: marina.name,
      phoneNumber: marina.phoneNumber ?? '',
      email: marina.email ?? '',
      website: marina.website ?? '',
      description: marina.description ?? '',
    },
  });

  async function onSubmit(data: ProfileFormData) {
    const updated = await updateMarina(marina.id, {
      name: data.name,
      phoneNumber: data.phoneNumber || null,
      email: data.email || null,
      website: data.website || null,
      description: data.description || null,
    });
    onSaved(updated);
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 max-w-lg">
      <Field label="Marina name" error={errors.name?.message}>
        <input {...register('name')} className={inp} />
      </Field>
      <Field label="Phone number">
        <input {...register('phoneNumber')} className={inp} placeholder="+1 (555) 000-0000" />
      </Field>
      <Field label="Contact email" error={errors.email?.message}>
        <input {...register('email')} type="email" className={inp} />
      </Field>
      <Field label="Website">
        <input {...register('website')} className={inp} placeholder="https://…" />
      </Field>
      <Field label="Description">
        <textarea {...register('description')} rows={4} className={`${inp} resize-none`}
          placeholder="Describe your marina…" />
      </Field>
      <Button type="submit" disabled={isSubmitting || !isDirty}>
        {isSubmitting ? 'Saving…' : 'Save changes'}
      </Button>
    </form>
  );
}

// ─── Address tab ──────────────────────────────────────────────────────────────

async function geocodeAddress(parts: { street?: string; city?: string; state?: string; zip?: string; country?: string }): Promise<{ lat: number; lon: number } | null> {
  const q = [parts.street, parts.city, parts.state, parts.zip, parts.country || 'US'].filter(Boolean).join(', ');
  try {
    const res = await fetch(`https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(q)}&format=json&limit=1`, { headers: { 'Accept-Language': 'en' } });
    const data = await res.json();
    if (!data.length) return null;
    return { lat: parseFloat(data[0].lat), lon: parseFloat(data[0].lon) };
  } catch { return null; }
}

const addressSchema = z.object({
  addressStreet: z.string().optional(),
  addressCity: z.string().optional(),
  addressState: z.string().optional(),
  addressZip: z.string().optional(),
  addressCountry: z.string().optional(),
  latitude: z.preprocess((v) => (v === '' || v == null ? undefined : Number(v)), z.number().min(-90).max(90).optional()),
  longitude: z.preprocess((v) => (v === '' || v == null ? undefined : Number(v)), z.number().min(-180).max(180).optional()),
});
type AddressFormData = z.infer<typeof addressSchema>;

function AddressTab({ marina, onSaved }: { marina: MarinaDto; onSaved: (m: MarinaDto) => void }) {
  const [geocoding, setGeocoding] = useState(false);
  const [geocodeError, setGeocodeError] = useState<string | null>(null);

  const { register, handleSubmit, getValues, setValue, formState: { errors, isSubmitting, isDirty } } = useForm<AddressFormData>({
    resolver: zodResolver(addressSchema) as never,
    defaultValues: {
      addressStreet: marina.addressStreet ?? '',
      addressCity: marina.addressCity ?? '',
      addressState: marina.addressState ?? '',
      addressZip: marina.addressZip ?? '',
      addressCountry: marina.addressCountry ?? '',
      latitude: marina.latitude ?? undefined,
      longitude: marina.longitude ?? undefined,
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
      setGeocodeError('Address not found. Try adding more detail.');
      return;
    }
    setValue('latitude', coords.lat as never, { shouldDirty: true });
    setValue('longitude', coords.lon as never, { shouldDirty: true });
  }

  async function onSubmit(data: AddressFormData) {
    const updated = await updateMarina(marina.id, {
      addressStreet: data.addressStreet || null,
      addressCity: data.addressCity || null,
      addressState: data.addressState || null,
      addressZip: data.addressZip || null,
      addressCountry: data.addressCountry || null,
      latitude: data.latitude ?? null,
      longitude: data.longitude ?? null,
    });
    onSaved(updated);
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 max-w-lg">
      <div className="grid grid-cols-2 gap-3">
        <div className="col-span-2">
          <Field label="Street address">
            <input {...register('addressStreet')} className={inp} />
          </Field>
        </div>
        <Field label="City">
          <input {...register('addressCity')} className={inp} />
        </Field>
        <Field label="State / Province">
          <input {...register('addressState')} className={inp} />
        </Field>
        <Field label="ZIP / Postal code">
          <input {...register('addressZip')} className={inp} />
        </Field>
        <Field label="Country">
          <input {...register('addressCountry')} className={inp} placeholder="US" />
        </Field>
      </div>

      <div className="border-t border-border pt-4 space-y-3">
        <p className="text-sm font-medium">Coordinates</p>
        {marina.latitude == null && (
          <p className="text-xs text-amber-600">⚠ No coordinates set — your marina won't appear in search results</p>
        )}
        <div className="grid grid-cols-2 gap-3">
          <Field label="Latitude" error={errors.latitude?.message}>
            <input {...register('latitude')} type="number" step="0.00001" placeholder="e.g. 25.77427" className={inp} />
          </Field>
          <Field label="Longitude" error={errors.longitude?.message}>
            <input {...register('longitude')} type="number" step="0.00001" placeholder="e.g. -80.19366" className={inp} />
          </Field>
        </div>
        <div className="flex items-center gap-3">
          <button type="button" onClick={handleGeocode} disabled={geocoding}
            className="text-sm text-primary hover:underline disabled:opacity-50">
            {geocoding ? 'Looking up…' : '📍 Auto-fill from address above'}
          </button>
          {geocodeError && <span className="text-xs text-destructive">{geocodeError}</span>}
        </div>
      </div>

      <Button type="submit" disabled={isSubmitting || !isDirty}>
        {isSubmitting ? 'Saving…' : 'Save changes'}
      </Button>
    </form>
  );
}

// ─── Photos tab ───────────────────────────────────────────────────────────────

type PhotoKindTab = 'Logo' | 'Banner' | 'Gallery' | 'Aerial' | 'Approach';
const PHOTO_TABS: PhotoKindTab[] = ['Logo', 'Banner', 'Gallery', 'Aerial', 'Approach'];

function PhotosTab({ marinaId }: { marinaId: string }) {
  const { upload, getPhotos, reorder, deletePhoto } = usePhotoUpload(marinaId);
  const [photos, setPhotos] = useState<MarinaPhotoDto[]>([]);
  const [activeTab, setActiveTab] = useState<PhotoKindTab>('Logo');
  const [showCropModal, setShowCropModal] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getPhotos().then(setPhotos).catch(() => {});
  }, [marinaId]);

  useEffect(() => {
    const processing = photos.some((p) => !p.urlThumbnail);
    if (!processing) return;
    const id = setTimeout(() => { getPhotos().then(setPhotos).catch(() => {}); }, 2000);
    return () => clearTimeout(id);
  }, [photos]);

  const kindPhotos = photos.filter((p) => p.kind === activeTab);
  const logoPhoto = photos.find((p) => p.kind === 'Logo') ?? null;
  const bannerPhoto = photos.find((p) => p.kind === 'Banner') ?? null;
  const isSingleSlot = activeTab === 'Logo' || activeTab === 'Banner';
  const aspectRatio = activeTab === 'Logo' ? 1 : activeTab === 'Banner' ? 16 / 9 : undefined;

  async function handleCropComplete(blob: Blob) {
    setUploading(true); setError(null);
    try {
      const existing = photos.find((p) => p.kind === activeTab);
      if (existing && isSingleSlot) {
        await deletePhoto(existing.id);
        setPhotos((prev) => prev.filter((p) => p.id !== existing.id));
      }
      const photo = await upload({ kind: activeTab, file: blob, contentType: 'image/jpeg' });
      setPhotos((prev) => [...prev, photo]);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Upload failed');
    } finally {
      setUploading(false); setShowCropModal(false);
    }
  }

  async function handleGalleryUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true); setError(null);
    try {
      const photo = await upload({ kind: activeTab, file, contentType: file.type || 'image/jpeg' });
      setPhotos((prev) => [...prev, photo]);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Upload failed');
    } finally {
      setUploading(false); e.target.value = '';
    }
  }

  async function handleReorder(photo: MarinaPhotoDto, direction: 'up' | 'down') {
    try { await reorder(photo.id, direction); setPhotos(await getPhotos()); } catch {}
  }

  async function handleDelete(photo: MarinaPhotoDto) {
    try { await deletePhoto(photo.id); setPhotos((prev) => prev.filter((p) => p.id !== photo.id)); } catch {}
  }

  return (
    <div className="space-y-4">
      {/* Kind tab strip */}
      <div className="flex gap-2 flex-wrap">
        {PHOTO_TABS.map((tab) => (
          <button key={tab} type="button" onClick={() => setActiveTab(tab)}
            className={cn('text-sm px-3 py-1 rounded-lg',
              activeTab === tab ? 'bg-foreground text-background' : 'border border-border text-muted-foreground hover:bg-muted/30')}>
            {tab}
          </button>
        ))}
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      {/* Logo */}
      {activeTab === 'Logo' && (
        <div className="flex flex-col items-start gap-4">
          {logoPhoto ? (
            <div className="relative group w-32 h-32">
              <img src={logoPhoto.urlThumbnail ?? undefined} alt="Marina logo"
                className="w-32 h-32 rounded-xl object-cover border border-border" />
              {!logoPhoto.urlThumbnail && (
                <div className="absolute inset-0 flex items-center justify-center bg-muted/80 rounded-xl">
                  <span className="text-xs text-muted-foreground animate-pulse">Processing…</span>
                </div>
              )}
              <button type="button" onClick={() => handleDelete(logoPhoto)}
                className="absolute -top-2 -right-2 rounded-full bg-destructive text-destructive-foreground w-5 h-5 text-xs flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">✕</button>
            </div>
          ) : (
            <div className="w-32 h-32 rounded-xl border-2 border-dashed border-border flex items-center justify-center text-muted-foreground text-xs">No logo</div>
          )}
          <Button type="button" disabled={uploading} onClick={() => setShowCropModal(true)}>
            {uploading ? 'Uploading…' : logoPhoto ? 'Replace logo' : 'Upload logo'}
          </Button>
          <p className="text-xs text-muted-foreground">Square image, 1:1 ratio. Recommended: 512×512px.</p>
        </div>
      )}

      {/* Banner */}
      {activeTab === 'Banner' && (
        <div className="flex flex-col gap-4">
          {bannerPhoto ? (
            <div className="relative group rounded-xl overflow-hidden border border-border aspect-video max-w-xl">
              <img src={bannerPhoto.urlMedium ?? bannerPhoto.urlFull ?? undefined} alt="Marina banner"
                className="w-full h-full object-cover" />
              {!bannerPhoto.urlThumbnail && (
                <div className="absolute inset-0 flex items-center justify-center bg-muted/80">
                  <span className="text-xs text-muted-foreground animate-pulse">Processing…</span>
                </div>
              )}
              <button type="button" onClick={() => handleDelete(bannerPhoto)}
                className="absolute top-2 right-2 rounded-full bg-destructive text-destructive-foreground w-6 h-6 text-xs flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">✕</button>
            </div>
          ) : (
            <div className="rounded-xl border-2 border-dashed border-border aspect-video max-w-xl flex items-center justify-center text-muted-foreground text-xs">No banner</div>
          )}
          <Button type="button" disabled={uploading} onClick={() => setShowCropModal(true)}>
            {uploading ? 'Uploading…' : bannerPhoto ? 'Replace banner' : 'Upload banner'}
          </Button>
          <p className="text-xs text-muted-foreground">Wide banner, 16:9 ratio. Recommended: 1600×900px.</p>
        </div>
      )}

      {/* Gallery / Aerial / Approach */}
      {!isSingleSlot && (
        <div className="space-y-4">
          <label className="cursor-pointer">
            <span className={cn('inline-flex items-center rounded-md bg-primary text-primary-foreground px-4 py-2 text-sm font-medium hover:bg-primary/90 transition-colors',
              uploading && 'opacity-50 pointer-events-none')}>
              {uploading ? 'Uploading…' : 'Add photo'}
            </span>
            <input type="file" accept="image/*" className="hidden" disabled={uploading} onChange={handleGalleryUpload} />
          </label>
          {kindPhotos.length === 0 ? (
            <p className="text-sm text-muted-foreground">No {activeTab.toLowerCase()} photos yet.</p>
          ) : (
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3">
              {kindPhotos.map((photo, idx) => (
                <PhotoCard key={photo.id} photo={photo} isFirst={idx === 0} isLast={idx === kindPhotos.length - 1}
                  onMoveUp={() => handleReorder(photo, 'up')} onMoveDown={() => handleReorder(photo, 'down')}
                  onDelete={() => handleDelete(photo)}
                  showCaption={activeTab === 'Approach'} showLatLng={activeTab === 'Approach'} />
              ))}
            </div>
          )}
        </div>
      )}

      {showCropModal && (
        <CropUploadModal aspectRatio={aspectRatio} onComplete={handleCropComplete}
          onCancel={() => setShowCropModal(false)} />
      )}
    </div>
  );
}

// ─── Subscription tab ─────────────────────────────────────────────────────────

const TIER_LABELS: Record<number, string> = { 0: 'Free', 1: 'Pro', 2: 'Premium' };
const TIER_FEATURES: { feature: string; free: boolean; pro: boolean; premium: boolean }[] = [
  { feature: 'Marina profile & listings', free: true, pro: true, premium: true },
  { feature: 'Slip management', free: true, pro: true, premium: true },
  { feature: 'Reservations', free: true, pro: true, premium: true },
  { feature: 'Customer accounts & billing', free: false, pro: true, premium: true },
  { feature: 'Announcements', free: false, pro: true, premium: true },
  { feature: 'Staff management', free: false, pro: true, premium: true },
  { feature: 'Analytics & reports', free: false, pro: false, premium: true },
  { feature: 'Advanced pricing plans', free: false, pro: false, premium: true },
];

function SubscriptionTab({ marina }: { marina: MarinaDto }) {
  const tier = (marina as MarinaDto & { subscriptionTier?: number }).subscriptionTier ?? 0;
  const tierLabel = TIER_LABELS[tier] ?? 'Free';
  const check = (v: boolean) => v ? '✓' : '—';

  return (
    <div className="space-y-6 max-w-lg">
      <div className="rounded-xl border border-border p-4 flex items-center justify-between">
        <div>
          <p className="text-sm font-medium">Current plan</p>
          <p className="text-2xl font-bold mt-1">{tierLabel}</p>
          <p className="text-xs text-muted-foreground mt-0.5">Billing managed through your account</p>
        </div>
        <div className="text-right">
          <Button variant="outline" disabled
            title="Plan upgrades are coming soon">
            Change plan
          </Button>
          <p className="text-xs text-muted-foreground mt-1">Coming soon</p>
        </div>
      </div>

      <div className="rounded-xl border border-border overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted/40">
            <tr>
              <th className="px-4 py-2 text-left text-xs font-medium text-muted-foreground">Feature</th>
              <th className="px-4 py-2 text-center text-xs font-medium text-muted-foreground">Free</th>
              <th className="px-4 py-2 text-center text-xs font-medium text-muted-foreground">Pro</th>
              <th className="px-4 py-2 text-center text-xs font-medium text-muted-foreground">Premium</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border/50">
            {TIER_FEATURES.map((row) => (
              <tr key={row.feature} className="hover:bg-muted/10">
                <td className="px-4 py-2.5">{row.feature}</td>
                <td className={cn('px-4 py-2.5 text-center', row.free ? 'text-success' : 'text-muted-foreground')}>{check(row.free)}</td>
                <td className={cn('px-4 py-2.5 text-center', row.pro ? 'text-success' : 'text-muted-foreground')}>{check(row.pro)}</td>
                <td className={cn('px-4 py-2.5 text-center', row.premium ? 'text-success' : 'text-muted-foreground')}>{check(row.premium)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export function SettingsPage() {
  const { marinaId } = useParams({ strict: false }) as { marinaId: string };
  const navigate = useNavigate();
  const router = useRouter();
  const search = useSearch({ strict: false }) as { tab?: string };
  const activeTab = (search.tab as SettingsTab) || 'profile';
  const qc = useQueryClient();

  const { data: marina } = useQuery<MarinaDto>({
    queryKey: ['marina', marinaId],
    queryFn: () => getMarina(marinaId),
    staleTime: 60_000,
  });

  function setTab(tab: SettingsTab) {
    navigate({ to: router.state.location.pathname, search: { tab } as Record<string, string>, replace: false });
  }

  function onSaved(updated: MarinaDto) {
    qc.setQueryData(['marina', marinaId], updated);
  }

  const tabStrip = (
    <div className="flex gap-0">
      {TABS.map(({ key, label }) => (
        <button key={key} type="button" onClick={() => setTab(key)}
          className={cn('px-4 py-2.5 text-sm font-medium border-b-2 transition-colors whitespace-nowrap',
            activeTab === key
              ? 'border-primary text-primary'
              : 'border-transparent text-muted-foreground hover:text-foreground')}>
          {label}
        </button>
      ))}
    </div>
  );

  return (
    <>
      <PageHeader title="Settings" tabs={tabStrip} />
      <PageBody>
        {!marina && <p className="text-sm text-muted-foreground">Loading…</p>}
        {marina && activeTab === 'profile' && <ProfileTab marina={marina} onSaved={onSaved} />}
        {marina && activeTab === 'address' && <AddressTab marina={marina} onSaved={onSaved} />}
        {activeTab === 'photos' && <PhotosTab marinaId={marinaId} />}
        {marina && activeTab === 'subscription' && <SubscriptionTab marina={marina} />}
      </PageBody>
    </>
  );
}
