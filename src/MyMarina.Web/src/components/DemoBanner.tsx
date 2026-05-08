import { useAuthStore } from "@/store/authStore";

export function DemoBanner() {
  const isDemo = useAuthStore((s) => s.isDemo);

  if (!isDemo) return null;

  const marketingSiteUrl =
    (window as any).__CONFIG__?.marketingSiteUrl ?? "https://mymarina.org";

  return (
    <div
      role="banner"
      className="bg-amber-500 text-amber-950 text-sm font-medium px-4 py-2 flex items-center justify-between gap-4"
    >
      <span>Demo — read-only preview</span>
      <div className="flex items-center gap-4 shrink-0">
        <a
          href={`${marketingSiteUrl}/register`}
          className="px-3 py-0.5 rounded-md bg-amber-900 text-amber-50 font-semibold hover:bg-amber-800 transition-colors whitespace-nowrap text-xs"
        >
          Sign up free →
        </a>
        <a
          href={marketingSiteUrl}
          className="underline underline-offset-2 hover:opacity-80 transition-opacity whitespace-nowrap"
        >
          Back to mymarina.org
        </a>
      </div>
    </div>
  );
}
