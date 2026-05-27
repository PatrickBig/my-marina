import { useNavigate } from '@tanstack/react-router';
import { Anchor, Plus, ChevronRight } from 'lucide-react';
import { NavBar } from '@/components/NavBar';
import { Badge } from '@/components/ui/badge';
import { useAuthStore } from '@/store/authStore';

export function MyMarinasPage() {
  const navigate = useNavigate();
  const { marinaMemberships, isAuthenticated } = useAuthStore();

  if (!isAuthenticated()) {
    navigate({ to: '/login' });
    return null;
  }

  const marinas = marinaMemberships();

  return (
    <div className="min-h-screen bg-background">
      <NavBar />

      <main className="mx-auto max-w-4xl px-4 py-10">
        <div className="flex items-center justify-between mb-8">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">My Marinas</h1>
            <p className="text-sm text-muted-foreground mt-1">
              Select a marina to open its operator workspace.
            </p>
          </div>
          <button
            type="button"
            onClick={() => navigate({ to: '/marina/new' })}
            className="flex items-center gap-1.5 rounded-lg bg-primary text-primary-foreground px-4 py-2 text-sm font-medium hover:bg-primary/90 transition-colors"
          >
            <Plus className="h-4 w-4" />
            New marina
          </button>
        </div>

        {marinas.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-xl border border-dashed border-border py-20 text-center">
            <Anchor className="h-10 w-10 text-muted-foreground mb-4" />
            <p className="text-sm text-muted-foreground">
              You don't have any marina memberships yet.
            </p>
            <button
              type="button"
              onClick={() => navigate({ to: '/marina/new' })}
              className="mt-4 text-sm font-medium text-primary hover:underline"
            >
              Create your first marina →
            </button>
          </div>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2">
            {marinas.map((m) => (
              <div
                key={m.marinaId}
                className="group flex items-center gap-4 rounded-xl border border-border bg-card p-5 hover:border-primary/40 hover:shadow-sm transition-all cursor-pointer"
                onClick={() => navigate({ to: '/marina/$marinaId', params: { marinaId: m.marinaId! } })}
                role="button"
                tabIndex={0}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    navigate({ to: '/marina/$marinaId', params: { marinaId: m.marinaId! } });
                  }
                }}
              >
                <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
                  <Anchor className="h-5 w-5" />
                </div>

                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <span className="truncate font-medium">{m.marinaName ?? 'My Marina'}</span>
                    {m.tier && (
                      <Badge variant="neutral" className="text-xs shrink-0">
                        {m.tier}
                      </Badge>
                    )}
                  </div>
                  <p className="text-xs text-muted-foreground mt-0.5 capitalize">
                    {m.role?.toLowerCase() ?? 'member'}
                  </p>
                </div>

                <ChevronRight className="h-4 w-4 text-muted-foreground group-hover:text-foreground transition-colors shrink-0" />
              </div>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
