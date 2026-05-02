import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { signupMarina, type MarinaType } from '@/api/api';
import { useAuthStore } from '@/store/authStore';

const schema = z.object({
  tenantName: z.string().min(2, 'Organization name must be at least 2 characters'),
  marinaName: z.string().min(2, 'Marina name must be at least 2 characters'),
  marinaType: z.enum(['Commercial', 'YachtClub', 'PrivateCommunity', 'Dockominium', 'PrivateDock'] as const),
});

type FormData = z.infer<typeof schema>;

const MARINA_TYPE_LABELS: Record<MarinaType, string> = {
  Commercial: 'Commercial Marina',
  YachtClub: 'Yacht Club',
  PrivateCommunity: 'Private Community',
  Dockominium: 'Dockominium',
  PrivateDock: 'Private Dock',
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
      // Store the fresh JWT (includes new owner membership)
      // We need the current user profile — keep existing user data
      if (user) {
        setAuth(res.accessToken, res.refreshToken, res.expiresAt, user);
      }
      window.location.href = `/marina/${res.marina.id}`;
    } catch (err: unknown) {
      setServerError('Failed to create marina. Please try again.');
    }
  }

  return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center px-4">
      <div className="w-full max-w-md bg-white rounded-2xl shadow-sm border border-slate-200 p-8">
        <h1 className="text-2xl font-semibold text-slate-800 mb-1">Set up your marina</h1>
        <p className="text-slate-500 text-sm mb-6">
          Create your marina account to start managing slips and staff.
        </p>

        {serverError && (
          <div className="mb-4 rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
            {serverError}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Organization name
            </label>
            <input
              {...register('tenantName')}
              type="text"
              placeholder="Sunset Harbor LLC"
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
            />
            {errors.tenantName && (
              <p className="mt-1 text-xs text-red-600">{errors.tenantName.message}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Marina name
            </label>
            <input
              {...register('marinaName')}
              type="text"
              placeholder="Sunset Harbor Marina"
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
            />
            {errors.marinaName && (
              <p className="mt-1 text-xs text-red-600">{errors.marinaName.message}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Marina type
            </label>
            <select
              {...register('marinaType')}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 bg-white"
            >
              {(Object.keys(MARINA_TYPE_LABELS) as MarinaType[]).map((t) => (
                <option key={t} value={t}>{MARINA_TYPE_LABELS[t]}</option>
              ))}
            </select>
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full rounded-lg bg-slate-800 text-white py-2.5 text-sm font-medium hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors mt-2"
          >
            {isSubmitting ? 'Creating…' : 'Create marina'}
          </button>
        </form>

        <p className="mt-4 text-center text-xs text-slate-400">
          <a href="/" className="hover:underline">Back to home</a>
        </p>
      </div>
    </div>
  );
}
