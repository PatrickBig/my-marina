import { useEffect } from 'react';
import { getMe } from '@/api/api';
import { useAuthStore } from '@/store/authStore';

export function AuthCallbackPage() {
  const { setAuth } = useAuthStore();

  useEffect(() => {
    async function handleCallback() {
      const params = new URLSearchParams(window.location.search);
      const accessToken  = params.get('accessToken');
      const refreshToken = params.get('refreshToken');   // null for demo sessions
      const expiresAt    = params.get('expiresAt');
      const isDemo       = params.get('isDemo') === 'true';
      const error        = params.get('error');

      if (error) {
        window.location.replace(`/login?error=${encodeURIComponent(error)}`);
        return;
      }

      if (!accessToken || !expiresAt) {
        window.location.replace('/login?error=invalid_callback');
        return;
      }

      try {
        useAuthStore.setState({ accessToken, refreshToken, expiresAt });
        const meData = await getMe();
        setAuth(accessToken, refreshToken, expiresAt, meData, meData.memberships, meData.isPlatformOperator, isDemo);

        if (isDemo) {
          // Send demo users straight to their first marina dashboard.
          const marinaMembership = meData.memberships?.find((m: { scope: string }) => m.scope === 'Marina');
          if (marinaMembership?.marinaId) {
            window.location.replace(`/marina/${marinaMembership.marinaId}`);
            return;
          }
        }

        window.location.replace('/');
      } catch {
        useAuthStore.getState().clearAuth();
        window.location.replace('/login?error=profile_fetch_failed');
      }
    }

    handleCallback();
  }, [setAuth]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-50">
      <p className="text-slate-500">Signing in…</p>
    </div>
  );
}
