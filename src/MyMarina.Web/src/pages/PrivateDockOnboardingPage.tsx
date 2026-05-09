import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { signupMarina, getMe } from '@/api/api';
import { useAuthStore } from '@/store/authStore';

const schema = z.object({
  dockName:  z.string().min(2, 'A name for your dock is required'),
  slipName:  z.string().min(1, 'Slip name / identifier is required'),
  maxLength: z.number().positive('Max length must be positive'),
  maxBeam:   z.number().positive('Max beam must be positive'),
  maxDraft:  z.number().positive('Max draft must be positive'),
  slipType:  z.enum(['Floating', 'Fixed', 'Mooring', 'Anchorage']),
  slipNotes: z.string().optional(),
});

type FormData = z.infer<typeof schema>;

export function PrivateDockOnboardingPage() {
  const { setAuth, user } = useAuthStore();
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
      const res = await signupMarina({
        tenantName: data.dockName,
        marinaName: data.dockName,
        marinaType: 'PrivateDock',
        slipName:   data.slipName,
        maxLength:  data.maxLength,
        maxBeam:    data.maxBeam,
        maxDraft:   data.maxDraft,
        slipType:   data.slipType,
        slipNotes:  data.slipNotes || undefined,
      });
      if (user) setAuth(res.accessToken, res.refreshToken, res.expiresAt, user);
      const me = await getMe();
      setAuth(res.accessToken, res.refreshToken, res.expiresAt, me, me.memberships, me.isPlatformOperator);
      window.location.href = `/marina/${res.marina.id}`;
    } catch {
      setServerError('Could not create your dock. Please try again.');
    }
  }

  return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center px-4">
      <div className="w-full max-w-md bg-white rounded-2xl shadow-sm border border-slate-200 p-8">
        <div className="mb-6">
          <h1 className="text-2xl font-semibold text-slate-800 mb-1">Add your dock</h1>
          <p className="text-slate-500 text-sm">
            List your private dock on the MyMarina marketplace and start earning.
          </p>
        </div>

        {serverError && (
          <div className="mb-4 rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
            {serverError}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Name for your dock
            </label>
            <input
              {...register('dockName')}
              type="text"
              placeholder="Pat's Dock"
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
            />
            {errors.dockName && <p className="mt-1 text-xs text-red-600">{errors.dockName.message}</p>}
          </div>

          <div className="border-t border-slate-100 pt-4">
            <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-3">Your slip</p>

            <div className="grid grid-cols-2 gap-3">
              <div className="col-span-2">
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Slip name / identifier
                </label>
                <input
                  {...register('slipName')}
                  type="text"
                  placeholder="Slip A"
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
                />
                {errors.slipName && <p className="mt-1 text-xs text-red-600">{errors.slipName.message}</p>}
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Max length (ft)</label>
                <input
                  {...register('maxLength', { valueAsNumber: true })}
                  type="number" step="0.5" min="1"
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
                />
                {errors.maxLength && <p className="mt-1 text-xs text-red-600">{errors.maxLength.message}</p>}
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Max beam (ft)</label>
                <input
                  {...register('maxBeam', { valueAsNumber: true })}
                  type="number" step="0.5" min="1"
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
                />
                {errors.maxBeam && <p className="mt-1 text-xs text-red-600">{errors.maxBeam.message}</p>}
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Max draft (ft)</label>
                <input
                  {...register('maxDraft', { valueAsNumber: true })}
                  type="number" step="0.25" min="0.5"
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
                />
                {errors.maxDraft && <p className="mt-1 text-xs text-red-600">{errors.maxDraft.message}</p>}
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Slip type</label>
                <select
                  {...register('slipType')}
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 bg-white"
                >
                  <option value="Floating">Floating</option>
                  <option value="Fixed">Fixed</option>
                  <option value="Mooring">Mooring</option>
                  <option value="Anchorage">Anchorage</option>
                </select>
              </div>

              <div className="col-span-2">
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Notes for boaters (optional)
                </label>
                <textarea
                  {...register('slipNotes')}
                  rows={2}
                  placeholder="Access code, parking instructions, water depth notes…"
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 resize-none"
                />
              </div>
            </div>
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full rounded-lg bg-slate-800 text-white py-2.5 text-sm font-medium hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors mt-2"
          >
            {isSubmitting ? 'Setting up your dock…' : 'Add my dock'}
          </button>
        </form>

        <p className="mt-4 text-center text-xs text-slate-400">
          <a href="/marina/new" className="hover:underline">Back</a>
        </p>
      </div>
    </div>
  );
}
