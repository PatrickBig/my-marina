import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';
import { DashboardPage } from './dashboard';

vi.mock('@tanstack/react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@tanstack/react-router')>();
  return {
    ...actual,
    useParams: () => ({ marinaId: 'marina-1' }),
    useNavigate: () => vi.fn(),
  };
});

vi.mock('@/api/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/api')>();
  return {
    ...actual,
    getMarinaComposition: () => Promise.resolve({
      total: 10, annual: 4, seasonal: 2, monthly: 1, transient: 1,
      listed: 1, maintenance: 0, vacant: 1,
    }),
    getBillingSummary: () => Promise.resolve({
      totalOutstanding: 8420, overdueCount: 3, totalOverdue: 2100,
      collectedThisMonth: 3180, draftCount: 1, sentCount: 4,
    }),
    getMarinaReservations: () => Promise.resolve([]),
    getMarinaWorkOrders: () => Promise.resolve([]),
    getMarinaInvoices: () => Promise.resolve([]),
  };
});

describe('DashboardPage', () => {
  it('renders KPI labels', () => {
    renderWithProviders(<DashboardPage />);
    expect(screen.getByText(/pending requests/i)).toBeTruthy();
    expect(screen.getByText(/open invoices/i)).toBeTruthy();
    expect(screen.getByText(/mtd earnings/i)).toBeTruthy();
    expect(screen.getByText(/open work orders/i)).toBeTruthy();
  });

  it('renders inbox tab strip', () => {
    renderWithProviders(<DashboardPage />);
    expect(screen.getByRole('tab', { name: /reservations/i })).toBeTruthy();
    expect(screen.getByRole('tab', { name: /work orders/i })).toBeTruthy();
    expect(screen.getByRole('tab', { name: /billing/i })).toBeTruthy();
    expect(screen.getByRole('tab', { name: /sublets/i })).toBeTruthy();
  });

  it('renders By dock navigation link', () => {
    renderWithProviders(<DashboardPage />);
    expect(screen.getByText(/by dock/i)).toBeTruthy();
  });
});
