import { Anchor, ChevronDown } from 'lucide-react';
import { useNavigate } from '@tanstack/react-router';
import { useAuthStore } from '@/store/authStore';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Badge } from '@/components/ui/badge';

export function OperatorButton() {
  const { marinaMemberships } = useAuthStore();
  const marinaMems = marinaMemberships();
  const navigate = useNavigate();

  if (marinaMems.length === 0) return null;

  if (marinaMems.length === 1) {
    const m = marinaMems[0];
    return (
      <button
        type="button"
        onClick={() => navigate({ to: '/marina/$marinaId', params: { marinaId: m.marinaId! } })}
        className="flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm font-medium hover:bg-muted/50 transition-colors"
        title={m.marinaName ?? 'My Marina'}
        aria-label="Open marina dashboard"
      >
        <Anchor className="h-4 w-4" />
        <span className="hidden sm:inline">{m.marinaName ?? 'My Marina'}</span>
      </button>
    );
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          className="flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm font-medium hover:bg-muted/50 transition-colors"
          aria-label="Select marina"
        >
          <Anchor className="h-4 w-4" />
          <span className="hidden sm:inline">My Marinas</span>
          <ChevronDown className="h-3 w-3 opacity-60" />
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        {marinaMems.map((m) => (
          <DropdownMenuItem
            key={m.marinaId}
            onClick={() => navigate({ to: '/marina/$marinaId', params: { marinaId: m.marinaId! } })}
            className="flex items-center justify-between gap-2"
          >
            <span className="truncate">{m.marinaName ?? 'Marina'}</span>
            {m.tier && (
              <Badge variant="neutral" className="shrink-0 text-xs">
                {m.tier}
              </Badge>
            )}
          </DropdownMenuItem>
        ))}
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={() => navigate({ to: '/my-marinas' })}>
          View all →
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
