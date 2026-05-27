/**
 * <KPI /> — dashboard KPI tile. Optional onClick → navigates to a filtered
 * downstream screen.
 *
 *   <KPI
 *     label="Open work orders"
 *     value={counters.openWorkOrders}
 *     hint="2 on-hold"
 *     icon={<Wrench className="size-3.5" />}
 *     onClick={() => navigate({ to: '/marina/$marinaId/maintenance', search: { col: 'inprogress' } })}
 *   />
 */

import { ChevronRight, ArrowUp, ArrowDown } from 'lucide-react';
import { type ReactNode } from 'react';
import { Card } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';

export interface KPIProps {
  label: ReactNode;
  value: ReactNode;
  hint?: ReactNode;
  icon?: ReactNode;
  /** Tint the icon background as accent (used for "needs attention" tiles). */
  accent?: boolean;
  /** Optional delta badge. */
  delta?: {
    tone: 'success' | 'warning' | 'destructive';
    direction: 'up' | 'down';
    value: string;
  };
  /** When set, the tile is clickable and shows a "View →" affordance. */
  onClick?: () => void;
  className?: string;
}

export function KPI({
  label, value, hint, icon, accent, delta, onClick, className,
}: KPIProps) {
  const clickable = !!onClick;
  return (
    <Card
      onClick={onClick}
      className={cn(
        'p-4',
        clickable && [
          'cursor-pointer transition-[border-color,box-shadow]',
          'hover:border-primary/40 hover:shadow-[0_1px_4px_color-mix(in_oklch,var(--primary)_10%,transparent)]',
        ],
        className,
      )}
    >
      <div className="flex items-start justify-between">
        <div className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
          {label}
        </div>
        {icon && (
          <div className={cn(
            'size-7 rounded-md flex items-center justify-center',
            accent ? 'bg-accent/40 text-accent-foreground' : 'bg-muted text-muted-foreground',
          )}>
            {icon}
          </div>
        )}
      </div>
      <div className="text-[26px] font-semibold tabular-nums mt-2 leading-tight">
        {value}
      </div>
      <div className="flex items-center gap-1.5 mt-1.5">
        {delta && (
          <Badge variant={delta.tone} className="h-5 text-[11px] gap-0.5">
            {delta.direction === 'up' ? <ArrowUp className="size-3" /> : <ArrowDown className="size-3" />}
            {delta.value}
          </Badge>
        )}
        {hint && <span className="text-xs text-muted-foreground">{hint}</span>}
      </div>
      {clickable && (
        <div className="flex items-center gap-1 mt-2 text-[11.5px] font-medium text-primary">
          View <ChevronRight className="size-3" />
        </div>
      )}
    </Card>
  );
}
