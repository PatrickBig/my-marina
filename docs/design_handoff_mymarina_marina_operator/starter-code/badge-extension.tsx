/**
 * Replacement for src/MyMarina.Web/src/components/ui/badge.tsx.
 *
 * Adds semantic variants (primary / accent / success / warning / destructive)
 * used throughout the operator workspace. Recipe uses color-mix() so each
 * variant works in light + dark without per-mode rules.
 *
 * The dot prop renders a 6px filled circle before the text — used in status
 * badges that need a stronger visual marker.
 */

import { cva, type VariantProps } from 'class-variance-authority';
import { type HTMLAttributes } from 'react';
import { cn } from '@/lib/utils';

const badgeVariants = cva(
  cn(
    'inline-flex items-center gap-1 px-2 h-[22px] rounded-full',
    'text-[11.5px] font-medium leading-none whitespace-nowrap',
    'border',
  ),
  {
    variants: {
      variant: {
        neutral:
          'border-border bg-card text-muted-foreground',
        primary:
          'border-[color-mix(in_oklch,var(--primary)_28%,transparent)] ' +
          'bg-[color-mix(in_oklch,var(--primary)_12%,transparent)] text-primary',
        accent:
          'border-[color-mix(in_oklch,var(--accent)_50%,transparent)] ' +
          'bg-[color-mix(in_oklch,var(--accent)_25%,transparent)] text-accent-foreground',
        success:
          'border-[color-mix(in_oklch,oklch(60%_0.13_155)_28%,transparent)] ' +
          'bg-[color-mix(in_oklch,oklch(60%_0.13_155)_12%,transparent)] ' +
          'text-[oklch(60%_0.13_155)]',
        warning:
          'border-[color-mix(in_oklch,oklch(75%_0.14_75)_32%,transparent)] ' +
          'bg-[color-mix(in_oklch,oklch(75%_0.14_75)_14%,transparent)] ' +
          'text-[color-mix(in_oklch,oklch(75%_0.14_75)_70%,black)]',
        destructive:
          'border-[color-mix(in_oklch,var(--destructive)_28%,transparent)] ' +
          'bg-[color-mix(in_oklch,var(--destructive)_10%,transparent)] text-destructive',
        solid:
          'border-primary bg-primary text-primary-foreground',
      },
    },
    defaultVariants: { variant: 'neutral' },
  },
);

export interface BadgeProps
  extends HTMLAttributes<HTMLSpanElement>,
    VariantProps<typeof badgeVariants> {
  /** Render a 6×6 filled circle before the label. */
  dot?: boolean;
}

export function Badge({ className, variant, dot, children, ...rest }: BadgeProps) {
  return (
    <span className={cn(badgeVariants({ variant }), className)} {...rest}>
      {dot && <span aria-hidden className="size-1.5 rounded-full bg-current shrink-0" />}
      {children}
    </span>
  );
}

export { badgeVariants };
