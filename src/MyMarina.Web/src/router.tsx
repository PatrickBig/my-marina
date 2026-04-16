import { createRouter, createRoute, createRootRoute, redirect } from "@tanstack/react-router";
import { RootLayout } from "./layouts/RootLayout";
import { OperatorLayout } from "./layouts/OperatorLayout";
import { PortalLayout } from "./layouts/PortalLayout";
import { LoginPage } from "./pages/LoginPage";
import { DashboardPage } from "./pages/DashboardPage";
import { MarinaProfilePage } from "./pages/MarinaProfilePage";
import { DocksPage } from "./pages/DocksPage";
import { SlipsPage } from "./pages/SlipsPage";
import { CustomersPage } from "./pages/CustomersPage";
import { CustomerDetailPage } from "./pages/CustomerDetailPage";
import { AssignmentsPage } from "./pages/AssignmentsPage";
import { StaffPage } from "./pages/StaffPage";
import { InvoicesPage } from "./pages/InvoicesPage";
import { InvoiceDetailPage } from "./pages/InvoiceDetailPage";
import { PortalDashboardPage } from "./pages/portal/PortalDashboardPage";
import { PortalSlipPage } from "./pages/portal/PortalSlipPage";
import { PortalBoatsPage } from "./pages/portal/PortalBoatsPage";
import { PortalInvoicesPage } from "./pages/portal/PortalInvoicesPage";
import { PortalInvoiceDetailPage } from "./pages/portal/PortalInvoiceDetailPage";
import { PortalMaintenanceRequestsPage } from "./pages/portal/PortalMaintenanceRequestsPage";
import { PortalAnnouncementsPage } from "./pages/portal/PortalAnnouncementsPage";
import { useAuthStore } from "./store/authStore";

// ─── Root ────────────────────────────────────────────────────────────────────
const rootRoute = createRootRoute({ component: RootLayout });

// ─── Login ───────────────────────────────────────────────────────────────────
const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/login",
  component: LoginPage,
});

// ─── Protected operator shell ─────────────────────────────────────────────────
const operatorRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: "operator",
  component: OperatorLayout,
  beforeLoad: () => {
    const { token, user } = useAuthStore.getState();
    if (!token) throw redirect({ to: "/login" });
    // Customers belong in the portal, not the operator shell
    if (user?.role === 3) throw redirect({ to: "/portal" });
  },
});

// ─── Customer portal shell ────────────────────────────────────────────────────
const portalRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/portal",
  component: PortalLayout,
  beforeLoad: () => {
    const { token, user } = useAuthStore.getState();
    if (!token) throw redirect({ to: "/login" });
    // Only customers can access the portal; operators go to dashboard
    if (user?.role !== 3) throw redirect({ to: "/" });
  },
});

// ─── Pages ───────────────────────────────────────────────────────────────────
const dashboardRoute = createRoute({
  getParentRoute: () => operatorRoute,
  path: "/",
  component: DashboardPage,
});

const marinaProfileRoute = createRoute({
  getParentRoute: () => operatorRoute,
  path: "/marina",
  component: MarinaProfilePage,
});

const docksRoute = createRoute({
  getParentRoute: () => operatorRoute,
  path: "/docks",
  component: DocksPage,
});

const slipsRoute = createRoute({
  getParentRoute: () => operatorRoute,
  path: "/slips",
  component: SlipsPage,
});

const customersRoute = createRoute({
  getParentRoute: () => operatorRoute,
  path: "/customers",
  component: CustomersPage,
});

const customerDetailRoute = createRoute({
  getParentRoute: () => operatorRoute,
  path: "/customers/$customerId",
  component: CustomerDetailPage,
});

const assignmentsRoute = createRoute({
  getParentRoute: () => operatorRoute,
  path: "/assignments",
  component: AssignmentsPage,
});

const staffRoute = createRoute({
  getParentRoute: () => operatorRoute,
  path: "/staff",
  component: StaffPage,
});

const invoicesRoute = createRoute({
  getParentRoute: () => operatorRoute,
  path: "/invoices",
  component: InvoicesPage,
});

const invoiceDetailRoute = createRoute({
  getParentRoute: () => operatorRoute,
  path: "/invoices/$invoiceId",
  component: InvoiceDetailPage,
});

// ─── Portal page routes (relative paths — parent is /portal) ─────────────────
const portalDashboardRoute = createRoute({
  getParentRoute: () => portalRoute,
  path: "/",
  component: PortalDashboardPage,
});

const portalSlipRoute = createRoute({
  getParentRoute: () => portalRoute,
  path: "slip",
  component: PortalSlipPage,
});

const portalBoatsRoute = createRoute({
  getParentRoute: () => portalRoute,
  path: "boats",
  component: PortalBoatsPage,
});

const portalInvoicesRoute = createRoute({
  getParentRoute: () => portalRoute,
  path: "invoices",
  component: PortalInvoicesPage,
});

const portalInvoiceDetailRoute = createRoute({
  getParentRoute: () => portalRoute,
  path: "invoices/$invoiceId",
  component: PortalInvoiceDetailPage,
});

const portalMaintenanceRoute = createRoute({
  getParentRoute: () => portalRoute,
  path: "maintenance",
  component: PortalMaintenanceRequestsPage,
});

const portalAnnouncementsRoute = createRoute({
  getParentRoute: () => portalRoute,
  path: "announcements",
  component: PortalAnnouncementsPage,
});

// ─── Route tree ───────────────────────────────────────────────────────────────
const routeTree = rootRoute.addChildren([
  loginRoute,
  operatorRoute.addChildren([
    dashboardRoute,
    marinaProfileRoute,
    docksRoute,
    slipsRoute,
    customersRoute,
    customerDetailRoute,
    assignmentsRoute,
    staffRoute,
    invoicesRoute,
    invoiceDetailRoute,
  ]),
  portalRoute.addChildren([
    portalDashboardRoute,
    portalSlipRoute,
    portalBoatsRoute,
    portalInvoicesRoute,
    portalInvoiceDetailRoute,
    portalMaintenanceRoute,
    portalAnnouncementsRoute,
  ]),
]);

export const router = createRouter({ routeTree });

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
