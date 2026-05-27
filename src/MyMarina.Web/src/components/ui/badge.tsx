import { cva, type VariantProps } from "class-variance-authority";
import { type HTMLAttributes } from "react";
import { cn } from "@/lib/utils";

const badgeVariants = cva(
  cn(
    "inline-flex items-center gap-1 px-2 h-[22px] rounded-full",
    "text-[11.5px] font-medium leading-none whitespace-nowrap",
    "border",
  ),
  {
    variants: {
      variant: {
        neutral:
          "border-border bg-card text-muted-foreground",
        primary:
          "border-[color-mix(in_oklch,var(--primary)_28%,transparent)] " +
          "bg-[color-mix(in_oklch,var(--primary)_12%,transparent)] text-primary",
        accent:
          "border-[color-mix(in_oklch,var(--accent)_50%,transparent)] " +
          "bg-[color-mix(in_oklch,var(--accent)_25%,transparent)] text-accent-foreground",
        success:
          "border-[color-mix(in_oklch,oklch(60%_0.13_155)_28%,transparent)] " +
          "bg-[color-mix(in_oklch,oklch(60%_0.13_155)_12%,transparent)] " +
          "text-[oklch(60%_0.13_155)]",
        warning:
          "border-[color-mix(in_oklch,oklch(75%_0.14_75)_32%,transparent)] " +
          "bg-[color-mix(in_oklch,oklch(75%_0.14_75)_14%,transparent)] " +
          "text-[color-mix(in_oklch,oklch(75%_0.14_75)_70%,black)]",
        destructive:
          "border-[color-mix(in_oklch,var(--destructive)_28%,transparent)] " +
          "bg-[color-mix(in_oklch,var(--destructive)_10%,transparent)] text-destructive",
        solid:
          "border-primary bg-primary text-primary-foreground",
        // Legacy aliases kept for backwards compat with existing usages
        default:
          "border-transparent bg-primary text-primary-foreground shadow",
        secondary:
          "border-transparent bg-secondary text-secondary-foreground",
        outline:
          "text-foreground border-border bg-transparent",
      },
    },
    defaultVariants: { variant: "neutral" },
  },
);

export interface BadgeProps
  extends HTMLAttributes<HTMLSpanElement>,
    VariantProps<typeof badgeVariants> {
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
