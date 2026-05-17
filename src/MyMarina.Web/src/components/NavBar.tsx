import { useEffect, useRef, useState } from 'react';
import { Bell, LogOut, User, Moon, Sun, HelpCircle, Navigation, Anchor, Ship, FileText, Wrench } from 'lucide-react';
import { logout } from '@/api/api';
import { useAuthStore } from '@/store/authStore';
import { useThemeStore } from '@/store/themeStore';
import './NavBar.css';

function BrandIcon() {
  return (
    <svg viewBox="0 0 16 16" fill="currentColor" aria-hidden="true" style={{ width: 16, height: 16, flexShrink: 0 }}>
      <path d="M8 1.5a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3Zm.75 4.4V13a4 4 0 0 0 3.18-2.66l.95.32A5 5 0 0 1 8.75 14.03V15h-1.5v-.97A5 5 0 0 1 3.12 10.66l.95-.32A4 4 0 0 0 7.25 13V5.9H5.5v-1.5h5v1.5H8.75Z" />
    </svg>
  );
}

function ChevronIcon() {
  return (
    <svg className="mm-chev" viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true">
      <path d="M3 4.5 6 7.5 9 4.5" />
    </svg>
  );
}

type OpenMenu = 'avatar' | 'account' | null;

export function NavBar() {
  const { user, refreshToken, clearAuth, marinaMemberships, isPlatformOperator } = useAuthStore();
  const { preference, cyclePreference } = useThemeStore();
  const marinaMems = marinaMemberships();

  const [openMenu, setOpenMenu] = useState<OpenMenu>(null);
  const shellRef = useRef<HTMLElement>(null);
  const avatarBtnRef = useRef<HTMLButtonElement>(null);
  const accountBtnRef = useRef<HTMLButtonElement>(null);

  const path = window.location.pathname;

  const initials = user
    ? `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toUpperCase()
    : '?';

  const collapsiblePaths = ['/trips', '/my-slips', '/boats', '/maintenance', '/invoices'];
  const isCollapsibleActive = collapsiblePaths.some((p) => path.startsWith(p));

  const marinaLinks = marinaMems.map((m) => ({
    href: `/marina/${m.marinaId}`,
    label: m.marinaName ?? 'My Marina',
  }));

  const isDark = preference === 'dark' ||
    (preference === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches);

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (shellRef.current && !shellRef.current.contains(e.target as Node)) {
        setOpenMenu(null);
      }
    }
    function handleKeydown(e: KeyboardEvent) {
      if (e.key !== 'Escape') return;
      setOpenMenu((prev) => {
        if (prev === 'avatar') avatarBtnRef.current?.focus();
        if (prev === 'account') accountBtnRef.current?.focus();
        return null;
      });
    }
    document.addEventListener('click', handleClick);
    document.addEventListener('keydown', handleKeydown);
    return () => {
      document.removeEventListener('click', handleClick);
      document.removeEventListener('keydown', handleKeydown);
    };
  }, []);

  function toggleMenu(name: 'avatar' | 'account') {
    setOpenMenu((prev) => (prev === name ? null : name));
  }

  async function handleLogout() {
    setOpenMenu(null);
    if (refreshToken) {
      try { await logout(refreshToken); } catch { /* best-effort */ }
    }
    clearAuth();
    window.location.href = '/login';
  }

  const collapsibleLinks = [
    { href: '/trips', label: 'My trips' },
    { href: '/my-slips', label: 'My slips' },
    { href: '/boats', label: 'My boats' },
    { href: '/maintenance', label: 'Maintenance' },
    { href: '/invoices', label: 'Invoices' },
    ...marinaLinks,
    ...(isPlatformOperator ? [{ href: '/admin', label: 'Admin' }] : []),
  ];

  return (
    <header ref={shellRef} className="mm-topbar-shell" role="banner">
      <div className="mm-topbar">

        {/* Brand */}
        <a href="/" className="mm-brand" aria-label="MyMarina, home">
          <BrandIcon />
          <span className="mm-wordmark">MyMarina</span>
        </a>

        {/* Primary nav */}
        <nav className="mm-nav" aria-label="Primary">
          <a
            href="/search"
            aria-current={path.startsWith('/search') ? 'page' : undefined}
            className="mm-nav-link"
          >
            Find a slip
          </a>

          {collapsibleLinks.map((link) => (
            <a
              key={link.href}
              href={link.href}
              aria-current={path.startsWith(link.href) ? 'page' : undefined}
              className="mm-nav-link mm-collapsible"
            >
              {link.label}
            </a>
          ))}

          {/* My account — collapsed mode only (< 760px) */}
          <button
            ref={accountBtnRef}
            type="button"
            onClick={(e) => { e.stopPropagation(); toggleMenu('account'); }}
            aria-haspopup="menu"
            aria-expanded={openMenu === 'account'}
            aria-controls="mm-account-menu"
            data-active={isCollapsibleActive}
            className="mm-nav-link mm-account-btn"
          >
            My account
            <ChevronIcon />
            <span className="mm-active-dot" aria-hidden="true" />
          </button>
        </nav>

        <div className="mm-spacer" />

        {/* Right cluster */}
        <div className="mm-cluster">
          <button
            type="button"
            className="mm-icon-btn mm-notif-btn"
            aria-label="Notifications"
          >
            <Bell size={16} aria-hidden="true" />
          </button>

          <button
            ref={avatarBtnRef}
            type="button"
            onClick={(e) => { e.stopPropagation(); toggleMenu('avatar'); }}
            aria-haspopup="menu"
            aria-expanded={openMenu === 'avatar'}
            aria-controls="mm-avatar-menu"
            className="mm-avatar-btn"
          >
            <span className="mm-avatar" aria-hidden="true">{initials}</span>
            <span className="mm-avatar-name">{user?.firstName}</span>
            <ChevronIcon />
          </button>
        </div>
      </div>

      {/* Avatar dropdown (right-aligned) */}
      {openMenu === 'avatar' && (
        <div
          id="mm-avatar-menu"
          className="mm-menu"
          data-align="right"
          role="menu"
          aria-label="Account menu"
        >
          <div className="mm-menu-header" role="presentation">
            <div className="mm-menu-header-name">{user?.firstName} {user?.lastName}</div>
            <div className="mm-menu-header-email">{user?.email}</div>
          </div>
          <div className="mm-sep" />
          <a href="/profile" role="menuitem" className="mm-menu-item" onClick={() => setOpenMenu(null)}>
            <User size={14} aria-hidden="true" />
            Account &amp; profile
          </a>
          <div className="mm-sep" />
          <button
            type="button"
            role="menuitem"
            className="mm-menu-item"
            onClick={(e) => { e.stopPropagation(); cyclePreference(); }}
          >
            {isDark ? <Sun size={14} aria-hidden="true" /> : <Moon size={14} aria-hidden="true" />}
            Dark mode
            <span className="mm-menu-meta">{isDark ? 'On' : 'Off'}</span>
          </button>
          <a href="/help" role="menuitem" className="mm-menu-item" onClick={() => setOpenMenu(null)}>
            <HelpCircle size={14} aria-hidden="true" />
            Help &amp; support
          </a>
          <div className="mm-sep" />
          <button
            type="button"
            role="menuitem"
            className="mm-menu-item danger"
            onClick={handleLogout}
          >
            <LogOut size={14} aria-hidden="true" />
            Sign out
          </button>
        </div>
      )}

      {/* Account dropdown (left-aligned, collapsed mode < 760px) */}
      {openMenu === 'account' && (
        <div
          id="mm-account-menu"
          className="mm-menu"
          data-align="left"
          role="menu"
          aria-label="My account"
        >
          <div className="mm-section-label">Reservations</div>
          <a href="/trips" role="menuitem" className="mm-menu-item" onClick={() => setOpenMenu(null)}>
            <Navigation size={14} aria-hidden="true" />
            My trips
          </a>
          <a href="/my-slips" role="menuitem" className="mm-menu-item" onClick={() => setOpenMenu(null)}>
            <Anchor size={14} aria-hidden="true" />
            My slips
          </a>
          <a href="/boats" role="menuitem" className="mm-menu-item" onClick={() => setOpenMenu(null)}>
            <Ship size={14} aria-hidden="true" />
            My boats
          </a>
          <div className="mm-sep" />
          <div className="mm-section-label">Billing &amp; service</div>
          <a href="/invoices" role="menuitem" className="mm-menu-item" onClick={() => setOpenMenu(null)}>
            <FileText size={14} aria-hidden="true" />
            Invoices
          </a>
          <a href="/maintenance" role="menuitem" className="mm-menu-item" onClick={() => setOpenMenu(null)}>
            <Wrench size={14} aria-hidden="true" />
            Maintenance requests
          </a>
          {marinaLinks.length > 0 && (
            <>
              <div className="mm-sep" />
              <div className="mm-section-label">My marinas</div>
              {marinaLinks.map((link) => (
                <a key={link.href} href={link.href} role="menuitem" className="mm-menu-item" onClick={() => setOpenMenu(null)}>
                  <Anchor size={14} aria-hidden="true" />
                  {link.label}
                </a>
              ))}
            </>
          )}
          {isPlatformOperator && (
            <>
              <div className="mm-sep" />
              <a href="/admin" role="menuitem" className="mm-menu-item" onClick={() => setOpenMenu(null)}>
                Admin
              </a>
            </>
          )}
        </div>
      )}
    </header>
  );
}
