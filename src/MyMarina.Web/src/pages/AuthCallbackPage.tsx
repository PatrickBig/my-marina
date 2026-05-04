import { useEffect } from 'react';
import { getMe } from '@/api/api';
import { useAuthStore } from '@/store/authStore';

export function AuthCallbackPage() {
  const { setAuth } = useAuthStore();

  useEffect(() => {
    async function handleCallback() {
      const params = new URLSearchParams(window.location.search);
      const accessToken  = params.get('accessToken');
      const refreshToken = params.get('refreshToken');
      const expiresAt    = params.get('expiresAt');
      const error        = params.get('error');

      if (error) {
        window.location.replace(`/login?error=${encodeURIComponent(error)}`);
        return;
      }

      if (!accessToken || !refreshToken || !expiresAt) {
        window.location.replace('/login?error=invalid_callback');
        return;
      }

      try {
        // Temporarily set the access token so the API client can attach it to the /me request.
        useAuthStore.setState({ accessToken, refreshToken, expiresAt });
        const meData = await getMe();
        setAuth(accessToken, refreshToken, expiresAt, meData, meData.memberships, meData.isPlatformOperator);
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
