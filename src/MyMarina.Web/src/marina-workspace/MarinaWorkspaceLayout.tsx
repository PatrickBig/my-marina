import { Outlet, Navigate, useParams } from '@tanstack/react-router';
import { useAuthStore } from '@/store/authStore';
import { NavBar } from '@/components/NavBar';
import { MarinaRail } from './MarinaRail';
import { MarinaTabBar } from './MarinaTabBar';

export function MarinaWorkspaceLayout() {
  const { marinaId } = useParams({ strict: false }) as { marinaId: string };

  // Single permission guard — protects all child routes
  const { marinaMemberships } = useAuthStore();
  const hasAccess = marinaMemberships().some((m) => m.marinaId === marinaId);
  if (!hasAccess) return <Navigate to="/" />;

  return (
    <div className="flex flex-col min-h-screen bg-background">
      <NavBar />
      <div
        className="@container/workspace flex flex-col flex-1"
        style={{ height: 'calc(100vh - 56px)' }}
      >
        <div className="flex flex-1 min-h-0">
          <MarinaRail marinaId={marinaId} />
          <main className="flex-1 min-w-0 overflow-y-auto">
            <Outlet />
          </main>
        </div>
        <MarinaTabBar marinaId={marinaId} />
      </div>
    </div>
  );
}
