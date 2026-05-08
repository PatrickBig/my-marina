import { useState } from 'react';

const cfg = () => (window as any).__CONFIG__ ?? {};
const API_BASE = () => cfg().apiBaseUrl ?? '/api';
const APP_URL  = () => cfg().appUrl ?? 'https://app.mymarina.org';

export function HeroSection() {
  const [loading, setLoading] = useState(false);
  const [error, setError]     = useState('');

  async function startDemo() {
    setLoading(true);
    setError('');
    try {
      const res = await fetch(`${API_BASE()}/demo/session`, { method: 'POST' });
      if (!res.ok) throw new Error('Could not create demo session');
      const data = await res.json();
      const params = new URLSearchParams({
        accessToken: data.accessToken,
        expiresAt:   data.expiresAt,
        isDemo:      'true',
      });
      window.location.href = `${APP_URL()}/auth/callback?${params.toString()}`;
    } catch {
      setError('Could not start the demo — please try again shortly.');
      setLoading(false);
    }
  }

  return (
    <section id="demo" className="py-24 px-4 sm:px-6 text-center max-w-4xl mx-auto">
      <h1 className="text-4xl sm:text-5xl lg:text-6xl font-bold tracking-tight leading-tight mb-6">
        Marina management,{' '}
        <span style={{ color: 'var(--brand)' }}>simplified.</span>
      </h1>
      <p className="text-lg sm:text-xl text-muted-foreground max-w-2xl mx-auto mb-10">
        MyMarina gives marina operators a single platform to manage slips, bookings, customers,
        invoices, and maintenance — from any device.
      </p>

      <div className="flex flex-col sm:flex-row gap-3 justify-center">
        <button
          onClick={startDemo}
          disabled={loading}
          className="px-6 py-3 rounded-lg text-white font-semibold text-base hover:opacity-90 transition-opacity disabled:opacity-60"
          style={{ backgroundColor: 'var(--brand)' }}
          aria-label="Try the live demo"
        >
          {loading ? 'Starting demo…' : 'Try the live demo'}
        </button>
        <a
          href="/register"
          className="px-6 py-3 rounded-lg border border-border font-semibold text-base hover:bg-secondary transition-colors inline-flex items-center justify-center"
        >
          Get started free
        </a>
      </div>

      {error && (
        <p className="mt-4 text-sm text-red-600" role="alert">{error}</p>
      )}

      <p className="mt-4 text-sm text-muted-foreground">
        No account required &mdash; explore a fully-loaded marina dashboard instantly.
      </p>
    </section>
  );
}
