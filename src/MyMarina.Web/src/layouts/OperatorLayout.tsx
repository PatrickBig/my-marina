import { Outlet, Link, useRouter } from "@tanstack/react-router";
import {
  Anchor, LayoutDashboard, Building2, Package, Ship, Users, Calendar, UserPlus, LogOut, ChevronDown,
} from "lucide-react";
import { useAuthStore } from "@/store/authStore";
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem,
  DropdownMenuSeparator, DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useLocation } from "@tanstack/react-router";

const navItems = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard, exact: true },
  { to: "/marina", label: "Marina Profile", icon: Building2 },
  { to: "/docks", label: "Docks", icon: Package },
  { to: "/slips", label: "Slips", icon: Ship },
  { to: "/customers", label: "Customers", icon: Users },
  { to: "/assignments", label: "Slip Assignments", icon: Calendar },
  { to: "/staff", label: "Staff", icon: UserPlus },
];

export function OperatorLayout() {
  const { user, logout } = useAuthStore();
  const router = useRouter();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    router.navigate({ to: "/login" });
  };

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
        <nav className="flex-1 overflow-y-auto py-4 px-3">
          <ul className="space-y-1">
            {navItems.map(({ to, label, icon: Icon, exact }) => {
              const isActive = exact
                ? location.pathname === to
                : location.pathname.startsWith(to) && to !== "/";
              return (
                <li key={to}>
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
                </li>
              );
            })}
          </ul>
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
