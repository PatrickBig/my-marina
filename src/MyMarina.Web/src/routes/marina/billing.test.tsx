import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';
import { BillingPage } from './billing';

vi.mock('@tanstack/react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@tanstack/react-router')>();
  return {
    ...actual,
    useParams: () => ({ marinaId: 'marina-1' }),
    useNavigate: () => vi.fn(),
    useRouter: () => ({ state: { location: { pathname: '/marina/marina-1/billing' } } }),
    useSearch: () => ({ status: 'open' }),
  };
});

vi.mock('@/api/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/api')>();
  return {
    ...actual,
    getBillingSummary: () => Promise.resolve({
      totalOutstanding: 4200,
      overdueCount: 2,
      totalOverdue: 1500,
      collectedThisMonth: 8000,
      draftCount: 1,
      sentCount: 3,
    }),
    getMarinaInvoices: () => Promise.resolve([]),
  };
});

describe('BillingPage', () => {
  it('renders all status filter chips', () => {
    renderWithProviders(<BillingPage />);
    expect(screen.getByRole('button', { name: /^all$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^open$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^overdue$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^paid$/i })).toBeTruthy();
  });

  it('marks the Open chip as active when status=open', () => {
    renderWithProviders(<BillingPage />);
    const chip = screen.getByRole('button', { name: /^open$/i });
    expect(chip.className).toMatch(/border-primary/);
  });

  it('renders KPI tiles', async () => {
    renderWithProviders(<BillingPage />);
    const outstanding = await screen.findByText(/outstanding/i);
    expect(outstanding).toBeTruthy();
  });

  it('shows empty state when no invoices', async () => {
    renderWithProviders(<BillingPage />);
    const empty = await screen.findByText(/no invoices found/i);
    expect(empty).toBeTruthy();
  });
});
