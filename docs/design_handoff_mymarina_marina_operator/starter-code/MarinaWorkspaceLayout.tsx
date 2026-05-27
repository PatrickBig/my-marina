/**
 * MarinaWorkspaceLayout — parent route component for every operator screen.
 *
 * Render at /marina/$marinaId via a TanStack Router layout route. Children
 * (Dashboard, Reservations, Slips, …) render through <Outlet />.
 *
 * Composition:
 *   <__root__>
 *     <NavBar />                                  (existing)
 *     <MarinaWorkspaceLayout>                     (this file)
 *       <MarinaRail />        ← sidebar / icon rail
 *       <main>
 *         <Outlet />          ← active child route
 *       </main>
 *       <MarinaTabBar />      ← bottom tabs (mobile)
 *     </MarinaWorkspaceLayout>
 *   </__root__>
 *
 * The CSS container query "workspace" on the wrapper controls which form the
 * rail / tab bar takes. See shell.md for the breakpoints.
 */

import { Outlet, Navigate, useParams } from '@tanstack/react-router';
import { useAuthStore } from '@/store/authStore';
import { MarinaRail } from './MarinaRail';
import { MarinaTabBar } from './MarinaTabBar';

export function MarinaWorkspaceLayout() {
  const { marinaId } = useParams({ strict: false }) as { marinaId: string };

  // Permission guard — every child route is protected by this single check.
  const memberships = useAuthStore((s) => s.marinaMemberships());
  const hasAccess = memberships.some((m) => m.marinaId === marinaId);
  if (!hasAccess) return <Navigate to="/" />;

  return (
    <div
      // `@container/workspace` is Tailwind 4 container-query syntax.
      // If you're not on Tailwind 4 yet, replace with `containerType: 'inline-size'`
      // in a style attribute.
      className="@container/workspace flex flex-col h-[calc(100vh-56px)] bg-background"
    >
      <div className="flex flex-1 min-h-0">
        <MarinaRail marinaId={marinaId} />
        <main className="flex-1 min-w-0 overflow-y-auto">
          <Outlet />
        </main>
      </div>
      <MarinaTabBar marinaId={marinaId} />
    </div>
  );
}
