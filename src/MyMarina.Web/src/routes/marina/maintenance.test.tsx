import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';
import { MaintenancePage } from './maintenance';

vi.mock('@tanstack/react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@tanstack/react-router')>();
  return {
    ...actual,
    useParams: () => ({ marinaId: 'marina-1' }),
    useNavigate: () => vi.fn(),
    useRouter: () => ({ state: { location: { pathname: '/marina/marina-1/maintenance' } } }),
    useSearch: () => ({ col: 'inprogress' }),
  };
});

vi.mock('@/api/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/api')>();
  return {
    ...actual,
    getMarinaMaintenanceRequests: () => Promise.resolve([]),
    getMarinaWorkOrders: () => Promise.resolve([]),
  };
});

describe('MaintenancePage', () => {
  it('renders board/list view toggle', () => {
    renderWithProviders(<MaintenancePage />);
    expect(screen.getByText(/board/i)).toBeTruthy();
    expect(screen.getByText(/list/i)).toBeTruthy();
  });

  it('renders col filter banner when col param is set', () => {
    renderWithProviders(<MaintenancePage />);
    expect(screen.getByText(/filtered to/i)).toBeTruthy();
    expect(screen.getByRole('button', { name: /clear/i })).toBeTruthy();
  });

  it('renders the Maintenance page header', () => {
    renderWithProviders(<MaintenancePage />);
    expect(screen.getByRole('heading', { level: 1 })).toBeTruthy();
  });
});
