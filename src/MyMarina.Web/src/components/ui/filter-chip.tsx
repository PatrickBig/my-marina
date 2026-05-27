import { cn } from "@/lib/utils";

interface FilterChipProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  active?: boolean;
  count?: number;
}

export function FilterChip({ active, count, className, children, ...rest }: FilterChipProps) {
  return (
    <button
      type="button"
      className={cn(
        "inline-flex items-center gap-1.5 px-3 h-8 rounded-full border text-sm font-medium",
        "transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        "disabled:pointer-events-none disabled:opacity-50",
        active
          ? "border-primary bg-primary/10 text-primary"
          : "border-border bg-card text-muted-foreground hover:bg-muted hover:text-foreground",
        className,
      )}
      {...rest}
    >
      {children}
      {count != null && (
        <span className={cn(
          "inline-flex items-center justify-center h-4 min-w-[1rem] px-1 rounded-full text-[10px] font-semibold",
          active ? "bg-primary/20 text-primary" : "bg-muted-foreground/20 text-muted-foreground",
        )}>
          {count}
        </span>
      )}
    </button>
  );
}
