import { useEffect, useState } from "react";
import { getTokenExpiry, getDemoTier } from "@/lib/jwt";

interface Props {
  token: string;
}

function formatCountdown(ms: number): string {
  if (ms <= 0) return "Expired";
  const totalSeconds = Math.floor(ms / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${String(seconds).padStart(2, "0")}`;
}

export function DemoBanner({ token }: Props) {
  const expiry = getTokenExpiry(token);
  const tier = getDemoTier(token);
  const [remaining, setRemaining] = useState(() =>
    expiry ? Math.max(0, expiry.getTime() - Date.now()) : 0
  );

  useEffect(() => {
    if (!expiry) return;
    const id = setInterval(() => {
      setRemaining(Math.max(0, expiry.getTime() - Date.now()));
    }, 1000);
    return () => clearInterval(id);
  }, [expiry]);

  const marketingSiteUrl =
    (window as any).__CONFIG__?.marketingSiteUrl ?? "https://mymarina.org";

  return (
    <div className="bg-amber-500 text-amber-950 text-sm font-medium px-4 py-2 flex items-center justify-between gap-4">
      <span>
        Demo session
        {tier && (
          <>
            {" "}
            &mdash; <span className="font-semibold capitalize">{tier}</span> tier
          </>
        )}
      </span>
      <span className="tabular-nums">
        {remaining > 0 ? `Expires in ${formatCountdown(remaining)}` : "Session expired"}
      </span>
      <a
        href={marketingSiteUrl}
        className="underline underline-offset-2 hover:opacity-80 transition-opacity whitespace-nowrap"
      >
        Back to mymarina.org
      </a>
    </div>
  );
}
