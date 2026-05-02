import { logout } from '@/api/api';
import { useAuthStore } from '@/store/authStore';

const links = [
  { href: '/boats',   label: 'My Boats' },
  { href: '/profile', label: 'Profile' },
];

export function NavBar() {
  const { user, refreshToken, clearAuth } = useAuthStore();

  async function handleLogout() {
    if (refreshToken) {
      try { await logout(refreshToken); } catch { /* best-effort */ }
    }
    clearAuth();
    window.location.href = '/login';
  }

  const path = window.location.pathname;

  return (
    <nav className="border-b border-slate-200 bg-white">
      <div className="max-w-4xl mx-auto px-4 h-14 flex items-center justify-between">
        <div className="flex items-center gap-6">
          <a href="/" className="font-semibold text-slate-800 text-sm">MyMarina</a>
          {links.map((l) => (
            <a
              key={l.href}
              href={l.href}
              className={`text-sm ${
                path === l.href
                  ? 'text-slate-900 font-medium'
                  : 'text-slate-500 hover:text-slate-800'
              }`}
            >
              {l.label}
            </a>
          ))}
        </div>

        <div className="flex items-center gap-3">
          {user && (
            <span className="text-sm text-slate-500">
              {user.firstName} {user.lastName}
            </span>
          )}
          <button
            onClick={handleLogout}
            className="text-sm text-slate-500 hover:text-slate-800"
          >
            Sign out
          </button>
        </div>
      </div>
    </nav>
  );
}
