import { type ReactNode } from 'react';
import { cn } from '@/lib/utils';

export interface PageHeaderProps {
  title: ReactNode;
  subtitle?: ReactNode;
  actions?: ReactNode;
  /** Slot rendered as a tab strip below the title row. */
  tabs?: ReactNode;
  className?: string;
}

export function PageHeader({ title, subtitle, actions, tabs, className }: PageHeaderProps) {
  return (
    <header className={cn('px-6 pt-5', tabs && 'border-b border-border', className)}>
      <div className="flex items-start justify-between flex-wrap gap-3">
        <div className="min-w-0">
          <h1 className="text-[22px] font-semibold tracking-[-0.01em] m-0">{title}</h1>
          {subtitle && (
            <p className="text-xs text-muted-foreground mt-1">{subtitle}</p>
          )}
        </div>
        {actions && <div className="flex items-center gap-2 flex-wrap">{actions}</div>}
      </div>
      {tabs && <div className="flex items-center gap-0 mt-4 overflow-x-auto">{tabs}</div>}
    </header>
  );
}

export function PageBody({
  children, className, padding = 'default',
}: {
  children: ReactNode;
  className?: string;
  padding?: 'default' | 'none';
}) {
  return (
    <div className={cn(padding === 'default' ? 'p-6' : '', className)}>
      {children}
    </div>
  );
}
