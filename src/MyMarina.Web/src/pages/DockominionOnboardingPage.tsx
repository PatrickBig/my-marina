import { useState, useEffect, useRef } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { signupMarina, searchMarinas, getMe, type MarinaSummaryDto } from '@/api/api';
import { useAuthStore } from '@/store/authStore';

type Step = 'slip' | 'marina' | 'policy';

const schema = z.object({
  slipLabel:  z.string().min(1, 'A name for your slip is required'),
  maxLength:  z.number().positive('Max length must be positive'),
  maxBeam:    z.number().positive('Max beam must be positive'),
  maxDraft:   z.number().positive('Max draft must be positive'),
  slipType:   z.enum(['Floating', 'Fixed', 'Mooring', 'Anchorage']),
  slipNotes:  z.string().optional(),
});

type FormData = z.infer<typeof schema>;

const POLICY_OPTIONS: { value: 'None' | 'NotifyOnly' | 'RequiresApproval'; label: string; description: string }[] = [
  {
    value: 'NotifyOnly',
    label: 'Notify only (recommended)',
    description: 'The marina is informed of each booking but cannot block it.',
  },
  {
    value: 'RequiresApproval',
    label: 'Requires marina approval',
    description: 'Each reservation is held until the marina approves it.',
  },
  {
    value: 'None',
    label: 'No coordination',
    description: 'The marina is not notified about bookings at your slip.',
  },
];

export function DockominionOnboardingPage() {
  const { setAuth, user } = useAuthStore();
  const [step, setStep]             = useState<Step>('slip');
  const [hostMarina, setHostMarina] = useState<MarinaSummaryDto | null>(null);
  const [policy, setPolicy]         = useState<'None' | 'NotifyOnly' | 'RequiresApproval'>('NotifyOnly');
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { slipType: 'Floating' },
  });

  async function onSubmit(data: FormData) {
    setServerError(null);
    try {
      const slipName = data.slipLabel;
      const label    = hostMarina ? `${slipName} at ${hostMarina.name}` : slipName;

      const res = await signupMarina({
        tenantName:      label,
        marinaName:      label,
        marinaType:      'Dockominium',
        slipName:        slipName,
        maxLength:       data.maxLength,
        maxBeam:         data.maxBeam,
        maxDraft:        data.maxDraft,
        slipType:        data.slipType,
        slipNotes:       data.slipNotes || undefined,
        hostMarinaId:    hostMarina?.id,
        hostMarinaPolicy: hostMarina ? policy : undefined,
      });
      if (user) setAuth(res.accessToken, res.refreshToken, res.expiresAt, user);
      const me = await getMe();
      setAuth(res.accessToken, res.refreshToken, res.expiresAt, me, me.memberships, me.isPlatformOperator);
      window.location.href = `/marina/${res.marina.id}`;
    } catch {
      setServerError('Could not set up your slip. Please try again.');
    }
  }

  function handleSlipNext() {
    // Trigger validation on slip fields before moving to next step
    handleSubmit(() => setStep('marina'))();
  }

  return (
    <div className="min-h-screen bg-muted/30 flex items-center justify-center px-4">
      <div className="w-full max-w-md bg-card rounded-2xl shadow-sm border border-border p-8">
        {/* Progress */}
        <div className="flex items-center gap-2 mb-6">
          {(['slip', 'marina', 'policy'] as Step[]).map((s, i) => (
            <div key={s} className="flex items-center gap-2">
              {i > 0 && <div className="h-px w-4 bg-muted" />}
              <div
                className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-semibold ${
                  step === s
                    ? 'bg-primary text-primary-foreground'
                    : (step === 'marina' && s === 'slip') || step === 'policy'
                    ? 'bg-muted text-muted-foreground'
                    : 'bg-muted text-muted-foreground/70'
                }`}
              >
                {i + 1}
              </div>
            </div>
          ))}
          <span className="ml-2 text-xs text-muted-foreground">
            {step === 'slip' ? 'Your slip' : step === 'marina' ? 'Your marina' : 'Coordination'}
          </span>
        </div>

        <h1 className="text-2xl font-semibold text-foreground mb-1">
          {step === 'slip' ? 'Add your slip' : step === 'marina' ? 'Which marina is it at?' : 'Marina coordination'}
        </h1>
        <p className="text-muted-foreground text-sm mb-6">
          {step === 'slip'   && 'Tell us about the slip you own at a marina.'}
          {step === 'marina' && 'Search for your marina so we can coordinate bookings. You can skip this.'}
          {step === 'policy' && `How should bookings at your slip be coordinated with ${hostMarina?.name}?`}
        </p>

        {serverError && (
          <div className="mb-4 rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
            {serverError}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)}>
          {step === 'slip' && (
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-foreground mb-1">Slip name / identifier</label>
                <input
                  {...register('slipLabel')}
                  type="text"
                  placeholder="Slip 42B"
                  className="w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
                {errors.slipLabel && <p className="mt-1 text-xs text-red-600">{errors.slipLabel.message}</p>}
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">Max length (ft)</label>
                  <input {...register('maxLength', { valueAsNumber: true })} type="number" step="0.5" min="1"
                    className="w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring" />
                  {errors.maxLength && <p className="mt-1 text-xs text-red-600">{errors.maxLength.message}</p>}
                </div>
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">Max beam (ft)</label>
                  <input {...register('maxBeam', { valueAsNumber: true })} type="number" step="0.5" min="1"
                    className="w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring" />
                  {errors.maxBeam && <p className="mt-1 text-xs text-red-600">{errors.maxBeam.message}</p>}
                </div>
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">Max draft (ft)</label>
                  <input {...register('maxDraft', { valueAsNumber: true })} type="number" step="0.25" min="0.5"
                    className="w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring" />
                  {errors.maxDraft && <p className="mt-1 text-xs text-red-600">{errors.maxDraft.message}</p>}
                </div>
                <div>
                  <label className="block text-sm font-medium text-foreground mb-1">Slip type</label>
                  <select {...register('slipType')}
                    className="w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring bg-card">
                    <option value="Floating">Floating</option>
                    <option value="Fixed">Fixed</option>
                    <option value="Mooring">Mooring</option>
                    <option value="Anchorage">Anchorage</option>
                  </select>
                </div>
                <div className="col-span-2">
                  <label className="block text-sm font-medium text-foreground mb-1">Notes for boaters (optional)</label>
                  <textarea {...register('slipNotes')} rows={2}
                    placeholder="Access code, parking, gate instructions…"
                    className="w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring resize-none" />
                </div>
              </div>

              <button type="button" onClick={handleSlipNext}
                className="w-full rounded-lg bg-primary text-primary-foreground py-2.5 text-sm font-medium hover:bg-primary/90 transition-colors mt-2">
                Continue
              </button>
            </div>
          )}

          {step === 'marina' && (
            <MarinaSearchStep
              onSelect={(m) => { setHostMarina(m); setStep('policy'); }}
              onSkip={() => handleSubmit(onSubmit)()}
              onBack={() => setStep('slip')}
            />
          )}

          {step === 'policy' && (
            <div className="space-y-3">
              {POLICY_OPTIONS.map((opt) => (
                <label key={opt.value}
                  className={`flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-colors ${
                    policy === opt.value ? 'border-primary bg-muted/30' : 'border-border hover:border-primary/50'
                  }`}>
                  <input type="radio" name="policy" value={opt.value} checked={policy === opt.value}
                    onChange={() => setPolicy(opt.value)} className="mt-0.5 accent-primary" />
                  <div>
                    <p className="text-sm font-medium text-foreground">{opt.label}</p>
                    <p className="text-xs text-muted-foreground">{opt.description}</p>
                  </div>
                </label>
              ))}

              <div className="flex gap-2 pt-2">
                <button type="button" onClick={() => setStep('marina')}
                  className="flex-1 rounded-lg border border-border text-foreground py-2.5 text-sm font-medium hover:bg-muted/30 transition-colors">
                  Back
                </button>
                <button type="submit" disabled={isSubmitting}
                  className="flex-1 rounded-lg bg-primary text-primary-foreground py-2.5 text-sm font-medium hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors">
                  {isSubmitting ? 'Setting up…' : 'Add my slip'}
                </button>
              </div>
            </div>
          )}
        </form>

        <p className="mt-4 text-center text-xs text-muted-foreground/70">
          <a href="/marina/new" className="hover:underline">Back to options</a>
        </p>
      </div>
    </div>
  );
}

function MarinaSearchStep({
  onSelect,
  onSkip,
  onBack,
}: {
  onSelect: (m: MarinaSummaryDto) => void;
  onSkip: () => void;
  onBack: () => void;
}) {
  const [query, setQuery]       = useState('');
  const [results, setResults]   = useState<MarinaSummaryDto[]>([]);
  const [loading, setLoading]   = useState(false);
  const debounceRef             = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (query.trim().length < 2) { setResults([]); return; }
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(async () => {
      setLoading(true);
      try {
        const data = await searchMarinas(query.trim());
        setResults(data);
      } finally {
        setLoading(false);
      }
    }, 300);
    return () => { if (debounceRef.current) clearTimeout(debounceRef.current); };
  }, [query]);

  return (
    <div className="space-y-3">
      <input
        type="text"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Search by marina name or city…"
        className="w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
      />

      {loading && <p className="text-xs text-muted-foreground/70">Searching…</p>}

      {results.length > 0 && (
        <ul className="border border-border rounded-lg overflow-hidden divide-y divide-border/50">
          {results.map((m) => (
            <li key={m.id}>
              <button type="button" onClick={() => onSelect(m)}
                className="w-full text-left px-4 py-3 hover:bg-muted/30 transition-colors">
                <p className="text-sm font-medium text-foreground">{m.name}</p>
                {(m.addressCity || m.addressState) && (
                  <p className="text-xs text-muted-foreground">{[m.addressCity, m.addressState].filter(Boolean).join(', ')}</p>
                )}
              </button>
            </li>
          ))}
        </ul>
      )}

      {query.trim().length >= 2 && !loading && results.length === 0 && (
        <p className="text-xs text-muted-foreground">No marinas found. You can skip this step.</p>
      )}

      <div className="flex gap-2 pt-1">
        <button type="button" onClick={onBack}
          className="flex-1 rounded-lg border border-border text-foreground py-2.5 text-sm font-medium hover:bg-muted/30 transition-colors">
          Back
        </button>
        <button type="button" onClick={onSkip}
          className="flex-1 rounded-lg bg-primary text-primary-foreground py-2.5 text-sm font-medium hover:bg-primary/90 transition-colors">
          Skip
        </button>
      </div>
    </div>
  );
}
