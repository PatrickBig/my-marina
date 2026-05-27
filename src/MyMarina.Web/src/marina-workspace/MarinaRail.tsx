import { Link, useLocation } from '@tanstack/react-router';
import { Anchor } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { cn } from '@/lib/utils';
import { getMarina } from '@/api/api';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { MARINA_NAV_GROUPS, type NavId } from './nav-config';
import { useMarinaCounters } from './useMarinaCounters';

export function MarinaRail({ marinaId }: { marinaId: string }) {
  const { pathname } = useLocation();
  const activeId = parseActive(pathname, marinaId);
  const { data: counters } = useMarinaCounters(marinaId);

  return (
    <aside className={cn(
      'flex-none border-r border-border bg-card',
      'flex flex-col min-h-0 w-60',
      '@max-[1023px]/workspace:w-16',
      '@max-[719px]/workspace:hidden',
    )}>
      <RailHeader marinaId={marinaId} />
      <TooltipProvider delayDuration={300}>
        <nav className="flex-1 min-h-0 overflow-y-auto py-2">
          {MARINA_NAV_GROUPS.map((group, gi) => (
            <div key={group.label} className="px-2">
              <div className={cn(
                'px-3 pt-2.5 pb-1 text-[10px] font-semibold uppercase tracking-wider',
                'text-muted-foreground',
                '@max-[1023px]/workspace:hidden',
              )}>
                {group.label}
              </div>

              {group.items.map((item) => {
                const Icon = item.icon;
                const isActive = activeId === item.id;
                const count = item.counter ? counters?.[item.counter] : undefined;

                const linkEl = (
                  <Link
                    key={item.id}
                    to={`/marina/$marinaId/${item.id}`}
                    params={{ marinaId }}
                    title={undefined}
                    className={cn(
                      'flex items-center gap-2.5 px-3 py-2 rounded-md text-sm font-medium',
                      'text-muted-foreground hover:bg-muted hover:text-foreground',
                      'relative',
                      isActive && 'bg-primary/10 text-primary hover:bg-primary/10',
                      '@max-[1023px]/workspace:justify-center @max-[1023px]/workspace:px-0',
                    )}
                  >
                    <Icon className="size-4 shrink-0" />
                    <span className="flex-1 @max-[1023px]/workspace:hidden">{item.label}</span>
                    {count != null && count > 0 && (
                      <CounterBadge
                        active={isActive}
                        className="@max-[1023px]/workspace:absolute @max-[1023px]/workspace:top-1 @max-[1023px]/workspace:right-1.5"
                      >
                        {count}
                      </CounterBadge>
                    )}
                  </Link>
                );

                return (
                  <Tooltip key={item.id}>
                    <TooltipTrigger asChild>{linkEl}</TooltipTrigger>
                    <TooltipContent side="right" className="@min-[1024px]/workspace:hidden">
                      {item.label}
                    </TooltipContent>
                  </Tooltip>
                );
              })}

              {gi < MARINA_NAV_GROUPS.length - 1 && (
                <div className="mx-1 my-2 h-px bg-border" />
              )}
            </div>
          ))}
        </nav>
      </TooltipProvider>
    </aside>
  );
}

function RailHeader({ marinaId }: { marinaId: string }) {
  const { data: marina } = useQuery({
    queryKey: ['marina', marinaId],
    queryFn: () => getMarina(marinaId),
    staleTime: Infinity,
  });

  return (
    <div className="flex items-center gap-2.5 px-4 py-3.5 border-b border-border @max-[1023px]/workspace:justify-center @max-[1023px]/workspace:px-0">
      <div className="size-9 rounded-lg bg-primary/15 text-primary flex items-center justify-center shrink-0">
        <Anchor className="size-[18px]" />
      </div>
      <div className="min-w-0 @max-[1023px]/workspace:hidden">
        <div className="text-[13.5px] font-semibold truncate">
          {marina?.name ?? '…'}
        </div>
        <div className="text-xs text-muted-foreground capitalize">
          {marina?.marinaType ?? ''}
        </div>
      </div>
    </div>
  );
}

function CounterBadge({
  children, active, className,
}: { children: React.ReactNode; active?: boolean; className?: string }) {
  return (
    <span
      className={cn(
        'min-w-[18px] h-[18px] px-1.5 rounded-full inline-flex items-center justify-center',
        'text-[11px] font-semibold',
        active ? 'bg-primary text-primary-foreground' : 'bg-muted-foreground/70 text-background',
        className,
      )}
    >
      {children}
    </span>
  );
}

function parseActive(pathname: string, marinaId: string): NavId | null {
  const prefix = `/marina/${marinaId}/`;
  if (!pathname.startsWith(prefix)) return null;
  const segment = pathname.slice(prefix.length).split('/')[0];
  return (segment || null) as NavId | null;
}
