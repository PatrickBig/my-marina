import { createRouter, createRoute, createRootRoute, redirect } from "@tanstack/react-router";
import { RootLayout } from "./layouts/RootLayout";
import { OperatorLayout } from "./layouts/OperatorLayout";
import { LoginPage } from "./pages/LoginPage";
import { DashboardPage } from "./pages/DashboardPage";
import { MarinaProfilePage } from "./pages/MarinaProfilePage";
import { DocksPage } from "./pages/DocksPage";
import { SlipsPage } from "./pages/SlipsPage";
import { CustomersPage } from "./pages/CustomersPage";
import { CustomerDetailPage } from "./pages/CustomerDetailPage";
import { AssignmentsPage } from "./pages/AssignmentsPage";
import { StaffPage } from "./pages/StaffPage";
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
    const { token } = useAuthStore.getState();
    if (!token) {
      throw redirect({ to: "/login" });
    }
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
  ]),
]);

export const router = createRouter({ routeTree });

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
