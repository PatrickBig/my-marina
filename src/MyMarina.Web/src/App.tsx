import { useAuthStore } from '@/store/authStore';
import { AuthCallbackPage } from '@/pages/AuthCallbackPage';
import { LoginPage } from '@/pages/LoginPage';
import { MyBoatsPage } from '@/pages/MyBoatsPage';
import { ProfilePage } from '@/pages/ProfilePage';
import { NavBar } from '@/components/NavBar';

export function App() {
  const { isAuthenticated } = useAuthStore();
  const path = window.location.pathname;

  if (path === '/auth/callback') return <AuthCallbackPage />;
  if (path === '/login') return <LoginPage />;
  if (!isAuthenticated()) return <LoginPage />;
  if (path === '/boats') return <MyBoatsPage />;
  if (path === '/profile') return <ProfilePage />;

  return (
    <div className="min-h-screen bg-slate-50">
      <NavBar />
      <div className="max-w-4xl mx-auto py-12 px-4">
        <h1 className="text-2xl font-semibold text-slate-800">Welcome to MyMarina</h1>
        <p className="text-slate-500 mt-2">
          Phase 3 complete.{' '}
          <a href="/boats" className="underline text-slate-600">Manage your boats</a> or{' '}
          <a href="/profile" className="underline text-slate-600">edit your profile</a>.
        </p>
      </div>
    </div>
  );
}
