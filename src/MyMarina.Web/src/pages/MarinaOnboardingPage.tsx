import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { signupMarina, getMe } from '@/api/api';
import { useAuthStore } from '@/store/authStore';

type OrgMarinaType = 'Commercial' | 'YachtClub' | 'PrivateCommunity';

const schema = z.object({
  tenantName: z.string().min(2, 'Organization name must be at least 2 characters'),
  marinaName: z.string().min(2, 'Marina name must be at least 2 characters'),
  marinaType: z.enum(['Commercial', 'YachtClub', 'PrivateCommunity'] as const),
});

type FormData = z.infer<typeof schema>;

const MARINA_TYPE_LABELS: Record<OrgMarinaType, string> = {
  Commercial:       'Commercial Marina',
  YachtClub:        'Yacht Club',
  PrivateCommunity: 'Private Community',
};

export function MarinaOnboardingPage() {
  const { setAuth, user } = useAuthStore();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { marinaType: 'Commercial' },
  });

  async function onSubmit(data: FormData) {
    setServerError(null);
    try {
      const res = await signupMarina({
        tenantName: data.tenantName,
        marinaName: data.marinaName,
        marinaType: data.marinaType,
      });
      if (user) setAuth(res.accessToken, res.refreshToken, res.expiresAt, user);
      const me = await getMe();
      setAuth(res.accessToken, res.refreshToken, res.expiresAt, me, me.memberships, me.isPlatformOperator);
      window.location.href = `/marina/${res.marina.id}/setup`;
    } catch {
      setServerError('Failed to create marina. Please try again.');
    }
  }

  return (
    <div className="min-h-screen bg-muted/30 flex items-center justify-center px-4">
      <div className="w-full max-w-md bg-card rounded-2xl shadow-sm border border-border p-8">
        <h1 className="text-2xl font-semibold text-foreground mb-1">Set up your marina</h1>
        <p className="text-muted-foreground text-sm mb-6">
          Create your marina account to start managing slips and staff.
        </p>

        {/* Private host shortcuts */}
        <div className="mb-6 rounded-lg border border-border/50 bg-muted/30 p-4 space-y-2">
          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">
            Not a commercial marina?
          </p>
          <div className="flex gap-2">
            <a href="/dock/new"
              className="flex-1 rounded-lg border border-border bg-card px-3 py-2 text-sm text-foreground font-medium hover:bg-muted/30 transition-colors text-center">
              Add my private dock
            </a>
            <a href="/slip/new"
              className="flex-1 rounded-lg border border-border bg-card px-3 py-2 text-sm text-foreground font-medium hover:bg-muted/30 transition-colors text-center">
              Add my slip at a marina
            </a>
          </div>
        </div>

        {serverError && (
          <div className="mb-4 rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
            {serverError}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-foreground mb-1">
              Organization name
            </label>
            <input
              {...register('tenantName')}
              type="text"
              placeholder="Sunset Harbor LLC"
              className="w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
            />
            {errors.tenantName && (
              <p className="mt-1 text-xs text-red-600">{errors.tenantName.message}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-foreground mb-1">
              Marina name
            </label>
            <input
              {...register('marinaName')}
              type="text"
              placeholder="Sunset Harbor Marina"
              className="w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
            />
            {errors.marinaName && (
              <p className="mt-1 text-xs text-red-600">{errors.marinaName.message}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-foreground mb-1">
              Marina type
            </label>
            <select
              {...register('marinaType')}
              className="w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring bg-card"
            >
              {(Object.keys(MARINA_TYPE_LABELS) as OrgMarinaType[]).map((t) => (
                <option key={t} value={t}>{MARINA_TYPE_LABELS[t]}</option>
              ))}
            </select>
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full rounded-lg bg-primary text-primary-foreground py-2.5 text-sm font-medium hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors mt-2"
          >
            {isSubmitting ? 'Creating…' : 'Create marina'}
          </button>
        </form>

        <p className="mt-4 text-center text-xs text-muted-foreground/70">
          <a href="/" className="hover:underline">Back to home</a>
        </p>
      </div>
    </div>
  );
}
