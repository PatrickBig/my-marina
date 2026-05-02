import { useAuthStore } from '@/store/authStore';
import { LoginPage } from '@/pages/LoginPage';

export function App() {
  const { isAuthenticated } = useAuthStore();
  const path = window.location.pathname;

  if (path === '/login') return <LoginPage />;
  if (!isAuthenticated()) return <LoginPage />;

  return (
    <div className="min-h-screen bg-slate-50">
      <div className="max-w-4xl mx-auto py-12 px-4">
        <h1 className="text-2xl font-semibold text-slate-800">Welcome to MyMarina</h1>
        <p className="text-slate-500 mt-2">Phase 1 complete. Host dashboard coming in Phase 4.</p>
      </div>
    </div>
  );
}
