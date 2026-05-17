import { useEffect } from 'react';
import { useAuthStore } from '@/store/authStore';
import { useThemeStore } from '@/store/themeStore';
import { DemoBanner } from '@/components/DemoBanner';
import { AuthCallbackPage } from '@/pages/AuthCallbackPage';
import { LoginPage } from '@/pages/LoginPage';
import { MyBoatsPage } from '@/pages/MyBoatsPage';
import { MyInvoicesPage } from '@/pages/MyInvoicesPage';
import { ProfilePage } from '@/pages/ProfilePage';
import { MarinaOnboardingPage } from '@/pages/MarinaOnboardingPage';
import { PrivateDockOnboardingPage } from '@/pages/PrivateDockOnboardingPage';
import { DockominionOnboardingPage } from '@/pages/DockominionOnboardingPage';
import { MarinaDashboardPage } from '@/pages/MarinaDashboardPage';
import { MarinaSetupWizardPage } from '@/pages/MarinaSetupWizardPage';
import { SearchPage } from '@/pages/SearchPage';
import { MarinaSlipsPage } from '@/pages/MarinaSlipsPage';
import { SlipDetailPage } from '@/pages/SlipDetailPage';
import { MyTripsPage } from '@/pages/MyTripsPage';
import { MySlipsPage } from '@/pages/MySlipsPage';
import { MaintenancePage } from '@/pages/MaintenancePage';
import { PlatformOperatorPage } from '@/pages/PlatformOperatorPage';
import { HomePage } from '@/pages/HomePage';

export function App() {
  const { isAuthenticated } = useAuthStore();
  const { preference, resolvedTheme } = useThemeStore();

  useEffect(() => {
    const applyTheme = () => {
      document.documentElement.classList.toggle('dark', resolvedTheme() === 'dark');
    };
    applyTheme();

    if (preference === 'system') {
      const mq = window.matchMedia('(prefers-color-scheme: dark)');
      mq.addEventListener('change', applyTheme);
      return () => mq.removeEventListener('change', applyTheme);
    }
  }, [preference, resolvedTheme]);
  const path = window.location.pathname;

  function renderPage() {
    if (path === '/auth/callback') return <AuthCallbackPage />;
    if (path === '/search') return <SearchPage />;
    if (path.startsWith('/search/marinas/')) return <MarinaSlipsPage />;
    if (path.startsWith('/slips/')) return <SlipDetailPage />;

    if (path === '/login') return <LoginPage />;
    if (!isAuthenticated()) return <LoginPage />;
    if (path === '/trips') return <MyTripsPage />;
    if (path === '/my-slips') return <MySlipsPage />;
    if (path === '/invoices') return <MyInvoicesPage />;
    if (path === '/maintenance') return <MaintenancePage />;
    if (path === '/admin') return <PlatformOperatorPage />;
    if (path === '/boats') return <MyBoatsPage />;
    if (path === '/profile') return <ProfilePage />;
    if (path === '/marina/new') return <MarinaOnboardingPage />;
    if (path === '/dock/new')   return <PrivateDockOnboardingPage />;
    if (path === '/slip/new')   return <DockominionOnboardingPage />;
    if (path.match(/^\/marina\/[^/]+\/setup$/)) return <MarinaSetupWizardPage />;
    if (path.startsWith('/marina/')) return <MarinaDashboardPage />;

    return <HomePage />;
  }

  return (
    <>
      <DemoBanner />
      {renderPage()}
    </>
  );
}
