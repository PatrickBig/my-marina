import { Outlet, Link, useRouter, useLocation, useNavigate } from "@tanstack/react-router";
import {
  Anchor, LayoutDashboard, Building2, Package, Ship, Users, Calendar, UserPlus,
  LogOut, ChevronDown, FileText, Megaphone, Wrench, ClipboardList, Repeat, UserCircle,
  ChevronRight, ChevronsUpDown,
} from "lucide-react";
import { useAuthStore } from "@/store/authStore";
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem,
  DropdownMenuSeparator, DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { hasMultipleContexts } from "@/lib/jwt";
import { useQuery } from "@tanstack/react-query";
import { getMarinas } from "@/api/api";
import { useMemo } from "react";

// Marina-scoped nav items — to is a function of the active marinaId
const marinaNavItems = (marinaId: string) => [
  { to: `/marinas/${marinaId}/profile`, label: "Marina Profile", icon: Building2 },
  { to: `/marinas/${marinaId}/docks`, label: "Docks", icon: Package },
  { to: `/marinas/${marinaId}/slips`, label: "Slips", icon: Ship },
  { to: `/marinas/${marinaId}/assignments`, label: "Slip Assignments", icon: Calendar },
  { to: `/marinas/${marinaId}/announcements`, label: "Announcements", icon: Megaphone },
  { to: `/marinas/${marinaId}/maintenance`, label: "Maintenance", icon: Wrench },
  { to: `/marinas/${marinaId}/work-orders`, label: "Work Orders", icon: ClipboardList },
  { to: `/marinas/${marinaId}/staff`, label: "Staff", icon: UserPlus },
];

// Tenant-level nav items (not marina-scoped)
const tenantNavItems = [
  { to: "/", label: "All Marinas", icon: LayoutDashboard, exact: true },
  { to: "/customers", label: "Customers", icon: Users },
  { to: "/invoices", label: "Invoices", icon: FileText },
];

// For marina staff who are scoped to a single marina, the dashboard just shows their marina
const marinaStaffGlobalItems = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard, exact: true },
  { to: "/customers", label: "Customers", icon: Users },
  { to: "/invoices", label: "Invoices", icon: FileText },
];

export function OperatorLayout() {
  const { user, token, logout } = useAuthStore();
  const router = useRouter();
  const navigate = useNavigate();
  const location = useLocation();

  // Detect current marina from URL (e.g. /marinas/abc123/docks → "abc123")
  const urlMarinaId = useMemo(() => {
    const m = location.pathname.match(/^\/marinas\/([^/]+)/);
    return m?.[1] ?? null;
  }, [location.pathname]);

  // For marina-scoped operators the JWT carries their single marina
  const jwtMarinaId = user?.marinaId ?? null;
  const isMarinaStaff = !!jwtMarinaId;

  // Active marina: URL takes precedence; JWT marinaId as fallback for marina staff
  const activeMarinaId = urlMarinaId ?? jwtMarinaId;

  // Fetch marinas for name display and the tenant-level switcher
  const { data: marinas = [] } = useQuery({ queryKey: ["marinas"], queryFn: getMarinas });
  const activeMarina = marinas.find(m => m.id === activeMarinaId);

  const handleLogout = () => {
    logout();
    router.navigate({ to: "/login" });
  };

  const handleSwitchContext = () => {
    logout();
    router.navigate({ to: "/login" });
  };

  const showSwitchContext = token && hasMultipleContexts(token);

  // For tenant operators inside a marina: switch to same relative path for another marina
  const switchToMarina = (newMarinaId: string) => {
    const currentPath = location.pathname;
    const subPath = currentPath.replace(/^\/marinas\/[^/]+/, "");
    navigate({ to: `/marinas/${newMarinaId}${subPath || "/profile"}` });
  };

  const globalItems = isMarinaStaff ? marinaStaffGlobalItems : tenantNavItems;

  return (
    <div className="flex h-screen overflow-hidden bg-background">
      {/* Sidebar */}
      <aside className="w-64 flex-shrink-0 border-r bg-card flex flex-col">
        {/* Logo */}
        <div className="flex items-center gap-2 px-6 py-5 border-b">
          <Anchor className="h-6 w-6 text-primary" />
          <span className="font-bold text-lg">MyMarina</span>
        </div>

        {/* Nav */}
        <nav className="flex-1 overflow-y-auto py-4 px-3 space-y-1">
          {/* Tenant-level / global items */}
          {globalItems.map(({ to, label, icon: Icon, exact }) => {
            const isActive = exact
              ? location.pathname === to
              : location.pathname.startsWith(to) && to !== "/";
            return (
              <NavItem key={to} to={to} label={label} icon={Icon} isActive={isActive} />
            );
          })}

          {/* Marina context section */}
          {activeMarinaId ? (
            <div className="pt-3">
              {/* Marina header / switcher */}
              <div className="px-3 mb-1">
                {isMarinaStaff || marinas.length <= 1 ? (
                  // Single marina — just show the name as a label
                  <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider truncate">
                    {activeMarina?.name ?? "Marina"}
                  </p>
                ) : (
                  // Tenant operator — dropdown to switch between marinas
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <button className="w-full flex items-center justify-between gap-1 text-xs font-semibold text-muted-foreground uppercase tracking-wider hover:text-foreground transition-colors rounded px-0 py-0.5 group">
                        <span className="truncate">{activeMarina?.name ?? "Select marina"}</span>
                        <ChevronsUpDown className="h-3 w-3 shrink-0 opacity-50 group-hover:opacity-100" />
                      </button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="start" className="w-56">
                      <div className="px-2 py-1.5 text-xs text-muted-foreground font-medium">Switch marina</div>
                      <DropdownMenuSeparator />
                      {marinas.map(m => (
                        <DropdownMenuItem
                          key={m.id}
                          onClick={() => switchToMarina(m.id)}
                          className={cn("cursor-pointer", m.id === activeMarinaId && "font-medium")}
                        >
                          {m.id === activeMarinaId && <ChevronRight className="h-3 w-3 mr-1 shrink-0" />}
                          {m.name}
                        </DropdownMenuItem>
                      ))}
                      <DropdownMenuSeparator />
                      <DropdownMenuItem asChild>
                        <Link to="/" className="cursor-pointer text-muted-foreground">
                          ← All Marinas
                        </Link>
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                )}
              </div>

              {/* Marina-scoped nav items */}
              {marinaNavItems(activeMarinaId).map(({ to, label, icon: Icon }) => {
                const isActive = location.pathname.startsWith(to);
                return (
                  <NavItem key={to} to={to} label={label} icon={Icon} isActive={isActive} />
                );
              })}
            </div>
          ) : (
            // Tenant operator not yet in a marina context
            <div className="pt-3 px-3">
              <p className="text-xs text-muted-foreground">
                Select a marina from the dashboard to manage it.
              </p>
            </div>
          )}
        </nav>

        {/* User menu */}
        <div className="border-t p-3">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="w-full justify-between px-3">
                <div className="flex items-center gap-2 min-w-0">
                  <div className="h-7 w-7 rounded-full bg-primary text-primary-foreground flex items-center justify-center text-xs font-semibold shrink-0">
                    {user?.firstName?.[0]}{user?.lastName?.[0]}
                  </div>
                  <span className="text-sm truncate">{user?.firstName} {user?.lastName}</span>
                </div>
                <ChevronDown className="h-4 w-4 opacity-50 shrink-0" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="w-56">
              <div className="px-2 py-1.5 text-xs text-muted-foreground">{user?.email}</div>
              <DropdownMenuSeparator />
              <DropdownMenuItem asChild>
                <Link to="/profile" className="flex items-center gap-2 cursor-pointer">
                  <UserCircle className="h-4 w-4" />
                  My Profile
                </Link>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              {showSwitchContext && (
                <>
                  <DropdownMenuItem onClick={handleSwitchContext}>
                    <Repeat className="h-4 w-4" />
                    Switch Context
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                </>
              )}
              <DropdownMenuItem onClick={handleLogout} className="text-destructive focus:text-destructive">
                <LogOut className="h-4 w-4" />
                Sign out
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 overflow-y-auto">
        <Outlet />
      </main>
    </div>
  );
}

function NavItem({ to, label, icon: Icon, isActive }: {
  to: string; label: string; icon: React.ElementType; isActive: boolean;
}) {
  return (
    <Link
      to={to}
      className={cn(
        "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
        isActive
          ? "bg-primary text-primary-foreground"
          : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
      )}
    >
      <Icon className="h-4 w-4" />
      {label}
    </Link>
  );
}
