import { Sun, Moon, Monitor } from 'lucide-react';
import { logout } from '@/api/api';
import { useAuthStore } from '@/store/authStore';
import { useThemeStore } from '@/store/themeStore';

const themeIcons = {
  light: Sun,
  dark: Moon,
  system: Monitor,
} as const;

export function NavBar() {
  const { user, refreshToken, clearAuth, marinaMemberships, isPlatformOperator } = useAuthStore();
  const { preference, cyclePreference } = useThemeStore();
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
    { href: '/trips', label: 'My Trips' },
    { href: '/my-slips', label: 'My Slips' },
    { href: '/invoices', label: 'My Invoices' },
    { href: '/maintenance', label: 'Maintenance' },
    { href: '/boats', label: 'My Boats' },
    { href: '/profile', label: 'Profile' },
  ];

  const marinaLinks = marinaMems.map((m) => ({
    href: `/marina/${m.marinaId}`,
    label: m.marinaName ?? 'My Marina',
  }));

  const operatorLinks = isPlatformOperator ? [{ href: '/admin', label: 'Admin' }] : [];
  const links = [...staticLinks, ...marinaLinks, ...operatorLinks];

  const ThemeIcon = themeIcons[preference];

  return (
    <nav className="border-b border-border bg-card">
      <div className="max-w-4xl mx-auto px-4 h-14 flex items-center justify-between">
        <div className="flex items-center gap-6 overflow-x-auto">
          <a href="/" className="font-semibold text-foreground text-sm shrink-0">
            ⚓ MyMarina
          </a>
          {links.map((l) => {
            const isActive = path.startsWith(l.href);
            return (
              <a
                key={l.href}
                href={l.href}
                className={`text-sm shrink-0 pb-0.5 transition-colors ${
                  isActive
                    ? 'text-foreground font-medium border-b-2 border-accent'
                    : 'text-muted-foreground hover:text-foreground border-b-2 border-transparent'
                }`}
              >
                {l.label}
              </a>
            );
          })}
          {marinaMems.length === 0 && (
            <a
              href="/marina/new"
              className={`text-sm shrink-0 pb-0.5 border-b-2 transition-colors ${
                path === '/marina/new'
                  ? 'text-foreground font-medium border-accent'
                  : 'text-muted-foreground hover:text-foreground border-transparent'
              }`}
            >
              Set up marina
            </a>
          )}
        </div>

        <div className="flex items-center gap-3 shrink-0">
          {user && (
            <span className="text-sm text-muted-foreground hidden sm:block">
              {user.firstName} {user.lastName}
            </span>
          )}
          <button
            type="button"
            onClick={cyclePreference}
            title={`Theme: ${preference}`}
            className="p-1.5 rounded-md text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
          >
            <ThemeIcon size={16} />
          </button>
          <button
            onClick={handleLogout}
            className="text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            Sign out
          </button>
        </div>
      </div>
    </nav>
  );
}
