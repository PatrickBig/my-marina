import { Outlet, createRootRoute, createRoute, createRouter, redirect } from '@tanstack/react-router';
import { useEffect } from 'react';
import { useAuthStore } from '@/store/authStore';
import { useThemeStore } from '@/store/themeStore';
import { DemoBanner } from '@/components/DemoBanner';

import { AuthCallbackPage } from '@/pages/AuthCallbackPage';
import { LoginPage } from '@/pages/LoginPage';
import { SearchPage } from '@/pages/SearchPage';
import { MarinaSlipsPage } from '@/pages/MarinaSlipsPage';
import { SlipDetailPage } from '@/pages/SlipDetailPage';
import { HomePage } from '@/pages/HomePage';
import { MyTripsPage } from '@/pages/MyTripsPage';
import { MySlipsPage } from '@/pages/MySlipsPage';
import { MyInvoicesPage } from '@/pages/MyInvoicesPage';
import { MaintenancePage } from '@/pages/MaintenancePage';
import { PlatformOperatorPage } from '@/pages/PlatformOperatorPage';
import { MyBoatsPage } from '@/pages/MyBoatsPage';
import { ProfilePage } from '@/pages/ProfilePage';
import { MarinaOnboardingPage } from '@/pages/MarinaOnboardingPage';
import { PrivateDockOnboardingPage } from '@/pages/PrivateDockOnboardingPage';
import { DockominionOnboardingPage } from '@/pages/DockominionOnboardingPage';
import { MarinaSetupWizardPage } from '@/pages/MarinaSetupWizardPage';
import { PricingPlansPage } from '@/pages/PricingPlansPage';
import { DashboardPage } from '@/routes/marina/dashboard';
import { ReservationsPage } from '@/routes/marina/reservations';
import { InquiriesPage } from '@/routes/marina/inquiries';
import { MaintenancePage as MarinaMaintenancePage } from '@/routes/marina/maintenance';
import { MyMarinasPage } from '@/pages/MyMarinasPage';
import { MarinaWorkspaceLayout } from '@/marina-workspace/MarinaWorkspaceLayout';
import { BillingPage } from '@/routes/marina/billing';
import { CustomersPage } from '@/routes/marina/customers';
import { AssignmentsPage } from '@/routes/marina/assignments';
import { ListingsPage } from '@/routes/marina/listings';
import { SlipsPage } from '@/routes/marina/slips';
import { AnnouncementsPage } from '@/routes/marina/announcements';
import { StaffPage } from '@/routes/marina/staff';
import { SettingsPage } from '@/routes/marina/settings';

// ─── Root layout ──────────────────────────────────────────────────────────────

function RootLayout() {
  const resolvedTheme = useThemeStore((s) => s.resolvedTheme);
  const applySystemPreference = useThemeStore((s) => s.applySystemPreference);

  useEffect(() => {
    document.documentElement.classList.toggle('dark', resolvedTheme === 'dark');
  }, [resolvedTheme]);

  useEffect(() => {
    const mq = window.matchMedia('(prefers-color-scheme: dark)');
    const handler = (e: MediaQueryListEvent) => applySystemPreference(e.matches);
    mq.addEventListener('change', handler);
    return () => mq.removeEventListener('change', handler);
  }, [applySystemPreference]);

  return (
    <>
      <DemoBanner />
      <Outlet />
    </>
  );
}

// ─── Route definitions ────────────────────────────────────────────────────────

const rootRoute = createRootRoute({ component: RootLayout });

// ── Public routes ──────────────────────────────────────────────────────────
const authCallbackRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/auth/callback',
  component: AuthCallbackPage,
});

const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/login',
  component: LoginPage,
});

const searchRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/search',
  component: SearchPage,
});

const marinaSlipsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/search/marinas/$marinaId',
  component: MarinaSlipsPage,
});

const slipDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/slips/$slipId',
  component: SlipDetailPage,
});

// ── Auth guard layout — redirect to /login if unauthenticated ───────────────
const authGuardRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: 'auth-guard',
  beforeLoad: () => {
    if (!useAuthStore.getState().isAuthenticated()) {
      throw redirect({ to: '/login' });
    }
  },
}).update({ component: Outlet });

// ── Authenticated routes ────────────────────────────────────────────────────
const tripsRoute = createRoute({
  getParentRoute: () => authGuardRoute,
  path: '/trips',
  component: MyTripsPage,
});

const mySlipsRoute = createRoute({
  getParentRoute: () => authGuardRoute,
  path: '/my-slips',
  component: MySlipsPage,
});

const invoicesRoute = createRoute({
  getParentRoute: () => authGuardRoute,
  path: '/invoices',
  component: MyInvoicesPage,
});

const maintenanceRoute = createRoute({
  getParentRoute: () => authGuardRoute,
  path: '/maintenance',
  component: MaintenancePage,
});

const adminRoute = createRoute({
  getParentRoute: () => authGuardRoute,
  path: '/admin',
  component: PlatformOperatorPage,
});

const boatsRoute = createRoute({
  getParentRoute: () => authGuardRoute,
  path: '/boats',
  component: MyBoatsPage,
});

const profileRoute = createRoute({
  getParentRoute: () => authGuardRoute,
  path: '/profile',
  component: ProfilePage,
});

const myMarinasRoute = createRoute({
  getParentRoute: () => authGuardRoute,
  path: '/my-marinas',
  component: MyMarinasPage,
});

// Marina onboarding & pre-workspace routes (specific paths before the workspace layout)
const marinaNewRoute = createRoute({
  getParentRoute: () => authGuardRoute,
  path: '/marina/new',
  component: MarinaOnboardingPage,
});

const dockNewRoute = createRoute({
  getParentRoute: () => authGuardRoute,
  path: '/dock/new',
  component: PrivateDockOnboardingPage,
});

const slipNewRoute = createRoute({
  getParentRoute: () => authGuardRoute,
  path: '/slip/new',
  component: DockominionOnboardingPage,
});

// These retain their own full-page layouts (own NavBar) until their workspace migration
const marinaSetupRoute = createRoute({
  getParentRoute: () => authGuardRoute,
  path: '/marina/$marinaId/setup',
  component: MarinaSetupWizardPage,
});

// ── Marina workspace layout route ──────────────────────────────────────────
const marinaWorkspaceRoute = createRoute({
  getParentRoute: () => authGuardRoute,
  path: '/marina/$marinaId',
  component: MarinaWorkspaceLayout,
});

// /marina/$marinaId → redirect to /marina/$marinaId/dashboard
const marinaWorkspaceIndexRoute = createRoute({
  getParentRoute: () => marinaWorkspaceRoute,
  path: '/',
  beforeLoad: ({ params }) => {
    throw redirect({ to: '/marina/$marinaId/dashboard', params });
  },
}).update({ component: () => null });

const dashboardRoute = createRoute({
  getParentRoute: () => marinaWorkspaceRoute,
  path: '/dashboard',
  component: DashboardPage,
});

// Stub routes — filled in during Phases 7–16
const reservationsRoute = createRoute({
  getParentRoute: () => marinaWorkspaceRoute,
  path: '/reservations',
  component: ReservationsPage,
  validateSearch: (s: Record<string, unknown>): { status?: string; id?: string; page?: string } => ({
    status: typeof s.status === 'string' ? s.status : undefined,
    id:     typeof s.id     === 'string' ? s.id     : undefined,
    page:   typeof s.page   === 'string' ? s.page   : undefined,
  }),
});
const inquiriesRoute = createRoute({
  getParentRoute: () => marinaWorkspaceRoute,
  path: '/inquiries',
  component: InquiriesPage,
});
const workspaceMaintenanceRoute = createRoute({
  getParentRoute: () => marinaWorkspaceRoute,
  path: '/maintenance',
  component: MarinaMaintenancePage,
  validateSearch: (s: Record<string, unknown>): { view?: string; done?: string; col?: string } => ({
    view: typeof s.view === 'string' ? s.view : undefined,
    done: typeof s.done === 'string' ? s.done : undefined,
    col:  typeof s.col  === 'string' ? s.col  : undefined,
  }),
});
const listingsRoute = createRoute({
  getParentRoute: () => marinaWorkspaceRoute,
  path: '/listings',
  component: ListingsPage,
  validateSearch: (s: Record<string, unknown>): { slipId?: string; slipName?: string; windowId?: string } => ({
    slipId:   typeof s.slipId   === 'string' ? s.slipId   : undefined,
    slipName: typeof s.slipName === 'string' ? s.slipName : undefined,
    windowId: typeof s.windowId === 'string' ? s.windowId : undefined,
  }),
});
const accountsRoute = createRoute({
  getParentRoute: () => marinaWorkspaceRoute,
  path: '/accounts',
  component: CustomersPage,
  validateSearch: (s: Record<string, unknown>): { status?: string; id?: string; page?: string; q?: string } => ({
    status: typeof s.status === 'string' ? s.status : undefined,
    id:     typeof s.id     === 'string' ? s.id     : undefined,
    page:   typeof s.page   === 'string' ? s.page   : undefined,
    q:      typeof s.q      === 'string' ? s.q      : undefined,
  }),
});
const assignmentsRoute = createRoute({
  getParentRoute: () => marinaWorkspaceRoute,
  path: '/assignments',
  component: AssignmentsPage,
  validateSearch: (s: Record<string, unknown>): { type?: string; endingSoon?: string; page?: string } => ({
    type:       typeof s.type       === 'string' ? s.type       : undefined,
    endingSoon: typeof s.endingSoon === 'string' ? s.endingSoon : undefined,
    page:       typeof s.page       === 'string' ? s.page       : undefined,
  }),
});
const billingRoute = createRoute({
  getParentRoute: () => marinaWorkspaceRoute,
  path: '/billing',
  component: BillingPage,
  validateSearch: (s: Record<string, unknown>): { status?: string; id?: string; page?: string } => ({
    status: typeof s.status === 'string' ? s.status : undefined,
    id:     typeof s.id     === 'string' ? s.id     : undefined,
    page:   typeof s.page   === 'string' ? s.page   : undefined,
  }),
});
const slipsRoute = createRoute({
  getParentRoute: () => marinaWorkspaceRoute,
  path: '/slips',
  component: SlipsPage,
  validateSearch: (s: Record<string, unknown>): { status?: string; page?: string; dock?: string } => ({
    status: typeof s.status === 'string' ? s.status : undefined,
    page:   typeof s.page   === 'string' ? s.page   : undefined,
    dock:   typeof s.dock   === 'string' ? s.dock   : undefined,
  }),
});
const pricingRoute = createRoute({
  getParentRoute: () => marinaWorkspaceRoute,
  path: '/pricing',
  component: PricingPlansPage,
});
const announcementsRoute = createRoute({
  getParentRoute: () => marinaWorkspaceRoute,
  path: '/announcements',
  component: AnnouncementsPage,
});
const staffRoute = createRoute({
  getParentRoute: () => marinaWorkspaceRoute,
  path: '/staff',
  component: StaffPage,
});
const settingsRoute = createRoute({
  getParentRoute: () => marinaWorkspaceRoute,
  path: '/settings',
  component: SettingsPage,
  validateSearch: (s: Record<string, unknown>): { tab?: string } => ({
    tab: typeof s.tab === 'string' ? s.tab : undefined,
  }),
});

const homeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: HomePage,
});

// ─── Router ───────────────────────────────────────────────────────────────────

const routeTree = rootRoute.addChildren([
  authCallbackRoute,
  loginRoute,
  searchRoute,
  marinaSlipsRoute,
  slipDetailRoute,
  homeRoute,
  authGuardRoute.addChildren([
    tripsRoute,
    mySlipsRoute,
    invoicesRoute,
    maintenanceRoute,
    adminRoute,
    boatsRoute,
    profileRoute,
    myMarinasRoute,
    marinaNewRoute,
    dockNewRoute,
    slipNewRoute,
    marinaSetupRoute,
    marinaWorkspaceRoute.addChildren([
      marinaWorkspaceIndexRoute,
      dashboardRoute,
      reservationsRoute,
      inquiriesRoute,
      workspaceMaintenanceRoute,
      listingsRoute,
      accountsRoute,
      assignmentsRoute,
      billingRoute,
      slipsRoute,
      pricingRoute,
      announcementsRoute,
      staffRoute,
      settingsRoute,
    ]),
  ]),
]);

export const router = createRouter({
  routeTree,
  defaultNotFoundComponent: () => <HomePage />,
});

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
