import { ChevronLeft, ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";

export interface PaginationProps {
  total: number;
  pageSize?: number;
  page?: number;
  onChange?: (page: number) => void;
  compact?: boolean;
  className?: string;
}

export function Pagination({
  total,
  pageSize = 10,
  page = 1,
  onChange,
  compact = false,
  className,
}: PaginationProps) {
  const pages = Math.max(1, Math.ceil(total / pageSize));
  if (pages <= 1) return null;

  const windowSize = compact ? 3 : 5;
  let start = Math.max(1, page - Math.floor(windowSize / 2));
  const end = Math.min(pages, start + windowSize - 1);
  start = Math.max(1, Math.min(start, end - windowSize + 1));

  const range: number[] = [];
  for (let i = start; i <= end; i++) range.push(i);

  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);

  const go = (n: number) => {
    if (n < 1 || n > pages || n === page) return;
    onChange?.(n);
  };

  return (
    <div className={cn("flex flex-wrap items-center justify-between gap-2 pt-3 pb-1 px-1", className)}>
      <div className="text-xs text-muted-foreground tabular-nums">
        Showing <span className="font-mono font-semibold">{from}–{to}</span>{" "}
        of <span className="font-mono font-semibold">{total}</span>
      </div>
      <nav className="flex items-center gap-1" aria-label="Pagination">
        <PageBtn onClick={() => go(page - 1)} disabled={page === 1} aria-label="Previous page">
          <ChevronLeft className="size-3.5" />
        </PageBtn>
        {start > 1 && (
          <>
            <PageBtn onClick={() => go(1)}>1</PageBtn>
            {start > 2 && <Ellipsis />}
          </>
        )}
        {range.map((n) => (
          <PageBtn key={n} onClick={() => go(n)} active={n === page}>{n}</PageBtn>
        ))}
        {end < pages && (
          <>
            {end < pages - 1 && <Ellipsis />}
            <PageBtn onClick={() => go(pages)}>{pages}</PageBtn>
          </>
        )}
        <PageBtn onClick={() => go(page + 1)} disabled={page === pages} aria-label="Next page">
          <ChevronRight className="size-3.5" />
        </PageBtn>
      </nav>
    </div>
  );
}

interface PageBtnProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  active?: boolean;
}

function PageBtn({ active, className, children, ...rest }: PageBtnProps) {
  return (
    <button
      type="button"
      className={cn(
        "h-8 min-w-[2rem] px-2 rounded-md border text-sm tabular-nums",
        "border-border bg-card text-foreground",
        "hover:bg-muted disabled:opacity-50 disabled:hover:bg-card disabled:cursor-default",
        active && "bg-primary text-primary-foreground border-primary hover:bg-primary",
        className,
      )}
      {...rest}
    >
      {children}
    </button>
  );
}

function Ellipsis() {
  return <span className="px-1 text-muted-foreground text-sm">…</span>;
}
