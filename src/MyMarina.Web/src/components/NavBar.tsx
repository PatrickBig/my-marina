import { logout } from '@/api/api';
import { useAuthStore } from '@/store/authStore';

export function NavBar() {
  const { user, refreshToken, clearAuth, marinaMemberships } = useAuthStore();
  const marinaMems = marinaMemberships();

  async function handleLogout() {
    if (refreshToken) {
      try { await logout(refreshToken); } catch { /* best-effort */ }
    }
    clearAuth();
    window.location.href = '/login';
  }

  const path = window.location.pathname;

  const staticLinks = [
    { href: '/search', label: 'Find a slip' },
    { href: '/boats', label: 'My Boats' },
    { href: '/profile', label: 'Profile' },
  ];

  const marinaLinks = marinaMems.map((m) => ({
    href: `/marina/${m.marinaId}`,
    label: 'My Marina',
  }));

  const links = [...staticLinks, ...marinaLinks];

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
                path.startsWith(l.href)
                  ? 'text-slate-900 font-medium'
                  : 'text-slate-500 hover:text-slate-800'
              }`}
            >
              {l.label}
            </a>
          ))}
          {marinaMems.length === 0 && (
            <a
              href="/marina/new"
              className={`text-sm ${
                path === '/marina/new'
                  ? 'text-slate-900 font-medium'
                  : 'text-slate-500 hover:text-slate-800'
              }`}
            >
              Set up marina
            </a>
          )}
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
