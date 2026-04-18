import { createRouter, createRoute, createRootRoute, redirect } from "@tanstack/react-router";
import { RootLayout } from "./layouts/RootLayout";
import { OperatorLayout } from "./layouts/OperatorLayout";
import { PortalLayout } from "./layouts/PortalLayout";
import { LoginPage } from "./pages/LoginPage";
import { MyMarinasPage } from "./pages/MyManinasPage";
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
import { AnnouncementsPage } from "./pages/AnnouncementsPage";
import { MaintenanceRequestsPage } from "./pages/MaintenanceRequestsPage";
import { WorkOrdersPage } from "./pages/WorkOrdersPage";
import { PlatformLayout } from "./layouts/PlatformLayout";
import { TenantsPage } from "./pages/platform/TenantsPage";
import { TenantDetailPage } from "./pages/platform/TenantDetailPage";
import { UsersPage } from "./pages/platform/UsersPage";
import { AuditLogPage } from "./pages/platform/AuditLogPage";
import { ProfilePage } from "./pages/ProfilePage";
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
    if (user?.role === "Customer") throw redirect({ to: "/portal" });
    // Platform operators have their own shell
    if (user?.role === "PlatformAdmin") throw redirect({ to: "/platform/tenants" });
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
    if (user?.role !== "Customer") throw redirect({ to: "/" });
  },
});

// ─── Pages ───────────────────────────────────────────────────────────────────
const dashboardRoute = createRoute({
  getParentRoute: () => operatorRoute,
  path: "/",
  component: MyMarinasPage,
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

const announcementsRoute = createRoute({
  getParentRoute: () => operatorRoute,
  path: "/announcements",
  component: AnnouncementsPage,
});

const maintenanceRequestsRoute = createRoute({
  getParentRoute: () => operatorRoute,
  path: "/maintenance",
  component: MaintenanceRequestsPage,
});

const workOrdersRoute = createRoute({
  getParentRoute: () => operatorRoute,
  path: "/work-orders",
  component: WorkOrdersPage,
});

// ─── Profile (shared across all authenticated layouts) ───────────────────────
const profileRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/profile",
  component: ProfilePage,
  beforeLoad: () => {
    const { token } = useAuthStore.getState();
    if (!token) throw redirect({ to: "/login" });
  },
});

// ─── Platform admin shell ─────────────────────────────────────────────────────
const platformRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/platform",
  component: PlatformLayout,
  beforeLoad: () => {
    const { token, user } = useAuthStore.getState();
    if (!token) throw redirect({ to: "/login" });
    if (user?.role !== "PlatformAdmin") throw redirect({ to: "/" });
  },
});

// ─── Platform page routes (relative paths — parent is /platform) ──────────────
const platformTenantsRoute = createRoute({
  getParentRoute: () => platformRoute,
  path: "tenants",
  component: TenantsPage,
});

const platformTenantDetailRoute = createRoute({
  getParentRoute: () => platformRoute,
  path: "tenants/$tenantId",
  component: TenantDetailPage,
});

const platformUsersRoute = createRoute({
  getParentRoute: () => platformRoute,
  path: "users",
  component: UsersPage,
  validateSearch: (search: Record<string, unknown>) => ({
    tenantId: typeof search.tenantId === "string" ? search.tenantId : undefined,
  }),
});

const platformAuditLogRoute = createRoute({
  getParentRoute: () => platformRoute,
  path: "audit-logs",
  component: AuditLogPage,
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
  profileRoute,
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
    announcementsRoute,
    maintenanceRequestsRoute,
    workOrdersRoute,
  ]),
  platformRoute.addChildren([
    platformTenantsRoute,
    platformTenantDetailRoute,
    platformUsersRoute,
    platformAuditLogRoute,
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
