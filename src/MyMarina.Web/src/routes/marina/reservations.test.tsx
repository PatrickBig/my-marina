import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';
import { ReservationsPage } from './reservations';

vi.mock('@tanstack/react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@tanstack/react-router')>();
  return {
    ...actual,
    useParams: () => ({ marinaId: 'marina-1' }),
    useNavigate: () => vi.fn(),
    useRouter: () => ({ state: { location: { pathname: '/marina/marina-1/reservations' } } }),
    useSearch: () => ({ status: 'confirmed' }),
  };
});

vi.mock('@/api/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/api')>();
  return {
    ...actual,
    getMarinaReservations: () => Promise.resolve([]),
    getMarinaWorkOrders: () => Promise.resolve([]),
    getMarinaInvoices: () => Promise.resolve([]),
  };
});

describe('ReservationsPage', () => {
  it('renders all status filter chips', () => {
    renderWithProviders(<ReservationsPage />);
    expect(screen.getByRole('button', { name: /^all$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^pending$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^confirmed$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^today$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^past$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^cancelled$/i })).toBeTruthy();
  });

  it('marks the Confirmed chip as active when status=confirmed', () => {
    renderWithProviders(<ReservationsPage />);
    const confirmedChip = screen.getByRole('button', { name: /^confirmed$/i });
    // active chip has border-primary class via data-active styling
    expect(confirmedChip.className).toMatch(/border-primary/);
  });

  it('shows empty state when no reservations', async () => {
    renderWithProviders(<ReservationsPage />);
    // Empty state appears after loading resolves
    const empty = await screen.findByText(/no reservations found/i);
    expect(empty).toBeTruthy();
  });
});
