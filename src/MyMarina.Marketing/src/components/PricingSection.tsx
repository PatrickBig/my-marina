import { useEffect, useState } from 'react';

const cfg = () => (window as any).__CONFIG__ ?? {};
const API_BASE = () => cfg().apiBaseUrl ?? '/api';
const APP_URL = () => cfg().appUrl ?? 'https://app.mymarina.org';

const TIERS = ['Free', 'Pro', 'Premium'] as const;
type Tier = typeof TIERS[number];

interface TierData {
  tier: Tier;
  capabilities: string[];
}

function formatCapability(cap: string): string {
  return cap.replace(/([A-Z])/g, ' $1').trim();
}

export function PricingSection() {
  const [data, setData] = useState<TierData[]>([]);
  const [loading, setLoading] = useState(true);
  const [launching, setLaunching] = useState<Tier | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    fetch(`${API_BASE()}/demo/capabilities/public`)
      .then(res => res.ok ? res.json() : Promise.reject())
      .then(json => {
        // Response: { tiers: [{ tier: "Free", capabilities: [...] }, ...] }
        const mapped: TierData[] = TIERS.map(t => {
          const match = (json.tiers as { tier: string; capabilities: string[] }[])
            .find(r => r.tier.toLowerCase() === t.toLowerCase());
          return { tier: t, capabilities: match?.capabilities ?? [] };
        });
        setData(mapped);
        setLoading(false);
      })
      .catch(() => setLoading(false));
  }, []);

  async function startDemo(tier: Tier) {
    setLaunching(tier);
    setError('');
    try {
      const res = await fetch(
        `${API_BASE()}/demo/session?role=operator&tier=${tier.toLowerCase()}`,
        { method: 'POST' }
      );
      if (!res.ok) throw new Error();
      const json = await res.json();
      window.location.href = `${APP_URL()}?demo_token=${encodeURIComponent(json.token)}`;
    } catch {
      setError('Could not start demo session — please try again.');
      setLaunching(null);
    }
  }

  return (
    <section id="pricing" className="py-20 px-4 sm:px-6">
      <div className="max-w-6xl mx-auto">
        <div className="text-center mb-14">
          <h2 className="text-3xl font-bold mb-3">Simple, transparent pricing</h2>
          <p className="text-muted-foreground text-lg max-w-xl mx-auto">
            Start free. Upgrade when you need more.
          </p>
        </div>

        {error && (
          <p className="text-center text-sm text-red-600 mb-6" role="alert">{error}</p>
        )}

        <div className="grid sm:grid-cols-3 gap-6">
          {loading
            ? TIERS.map(t => (
                <div key={t} className="rounded-xl border border-border p-6 animate-pulse bg-secondary/20 h-64" />
              ))
            : data.map(({ tier, capabilities }) => (
                <div
                  key={tier}
                  className={`rounded-xl border p-6 flex flex-col gap-4 ${tier === 'Pro' ? 'border-[var(--brand)] shadow-md' : 'border-border'}`}
                >
                  {tier === 'Pro' && (
                    <span className="text-xs font-semibold uppercase tracking-wide text-[var(--brand)]">
                      Most popular
                    </span>
                  )}
                  <h3 className="text-xl font-bold">{tier}</h3>
                  <ul className="space-y-1.5 flex-1">
                    {capabilities.length === 0 ? (
                      <li className="text-sm text-muted-foreground italic">No capabilities listed yet</li>
                    ) : (
                      capabilities.map(cap => (
                        <li key={cap} className="flex items-center gap-2 text-sm">
                          <span className="text-green-600" aria-hidden="true">✓</span>
                          {formatCapability(cap)}
                        </li>
                      ))
                    )}
                  </ul>
                  <button
                    onClick={() => startDemo(tier)}
                    disabled={!!launching}
                    className={`w-full py-2.5 rounded-lg font-medium text-sm transition-opacity disabled:opacity-60 ${
                      tier === 'Pro'
                        ? 'text-white hover:opacity-90'
                        : 'border border-border hover:bg-secondary'
                    }`}
                    style={tier === 'Pro' ? { backgroundColor: 'var(--brand)' } : {}}
                    aria-label={`Try ${tier} tier demo`}
                  >
                    {launching === tier ? 'Starting…' : `Try ${tier} Demo`}
                  </button>
                </div>
              ))}
        </div>
      </div>
    </section>
  );
}
