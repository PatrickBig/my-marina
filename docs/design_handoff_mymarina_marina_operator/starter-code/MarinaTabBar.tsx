/**
 * MarinaTabBar — bottom tab bar shown only at < 720px container width.
 *
 * Five fixed slots: Home · Reservations · Slips · Billing · More.
 * "More" opens a Radix Sheet listing the remaining 8 destinations.
 */

import { useState } from 'react';
import { Link, useLocation } from '@tanstack/react-router';
import {
  Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger,
} from '@/components/ui/sheet'; // add this primitive if not present
import { cn } from '@/lib/utils';
import { MOBILE_TABS, MARINA_NAV_GROUPS, type NavId } from './nav-config';
import { useMarinaCounters } from './useMarinaCounters';

export function MarinaTabBar({ marinaId }: { marinaId: string }) {
  const { pathname } = useLocation();
  const { data: counters } = useMarinaCounters(marinaId);
  const [moreOpen, setMoreOpen] = useState(false);

  const activeId = parseActive(pathname, marinaId);
  const inMore =
    activeId != null &&
    !['dashboard', 'reservations', 'slips', 'billing'].includes(activeId);

  return (
    <nav
      className={cn(
        'hidden grid-cols-5 border-t border-border bg-card flex-none',
        '@max-[719px]/workspace:grid',
      )}
      aria-label="Marina sections"
    >
      {MOBILE_TABS.map((tab) => {
        const Icon = tab.icon;
        const isActive =
          tab.id === 'more' ? inMore : activeId === tab.id;
        const count =
          tab.counter ? counters?.[tab.counter] : undefined;

        if (tab.id === 'more') {
          return (
            <Sheet key="more" open={moreOpen} onOpenChange={setMoreOpen}>
              <SheetTrigger asChild>
                <button className={tabButtonClass(isActive)}>
                  <Icon className="size-5" />
                  <span>{tab.label}</span>
                </button>
              </SheetTrigger>
              <SheetContent side="bottom" className="rounded-t-xl">
                <SheetHeader>
                  <SheetTitle>More</SheetTitle>
                </SheetHeader>
                <MoreSheetGrid marinaId={marinaId} onNavigate={() => setMoreOpen(false)} />
              </SheetContent>
            </Sheet>
          );
        }

        return (
          <Link
            key={tab.id}
            to={`/marina/$marinaId/${tab.id}`}
            params={{ marinaId }}
            className={tabButtonClass(isActive)}
          >
            <div className="relative">
              <Icon className="size-5" />
              {count != null && count > 0 && (
                <span className="absolute -top-1.5 -right-2.5 px-1.5 h-3.5 rounded-full bg-destructive text-destructive-foreground text-[10px] font-semibold inline-flex items-center justify-center">
                  {count}
                </span>
              )}
            </div>
            <span>{tab.label}</span>
          </Link>
        );
      })}
    </nav>
  );
}

function tabButtonClass(active: boolean) {
  return cn(
    'flex flex-col items-center justify-center gap-1 py-2.5 text-[10.5px] font-medium',
    active ? 'text-primary' : 'text-muted-foreground',
  );
}

function MoreSheetGrid({
  marinaId, onNavigate,
}: { marinaId: string; onNavigate: () => void }) {
  const restGroups = MARINA_NAV_GROUPS.map((g) => ({
    ...g,
    items: g.items.filter(
      (i) => !['dashboard', 'reservations', 'slips', 'billing'].includes(i.id),
    ),
  })).filter((g) => g.items.length > 0);

  return (
    <div className="py-4 space-y-6">
      {restGroups.map((group) => (
        <div key={group.label}>
          <div className="px-1 pb-2 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
            {group.label}
          </div>
          <div className="grid grid-cols-3 gap-2">
            {group.items.map((item) => {
              const Icon = item.icon;
              return (
                <Link
                  key={item.id}
                  to={`/marina/$marinaId/${item.id}`}
                  params={{ marinaId }}
                  onClick={onNavigate}
                  className="flex flex-col items-center gap-1.5 p-3 rounded-lg border border-border text-foreground/80 hover:bg-muted"
                >
                  <Icon className="size-5" />
                  <span className="text-xs">{item.label}</span>
                </Link>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
}

function parseActive(pathname: string, marinaId: string): NavId | null {
  const prefix = `/marina/${marinaId}/`;
  if (!pathname.startsWith(prefix)) return null;
  return (pathname.slice(prefix.length).split('/')[0] || null) as NavId | null;
}
