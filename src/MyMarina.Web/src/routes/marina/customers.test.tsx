import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';
import { CustomersPage } from './customers';

vi.mock('@tanstack/react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@tanstack/react-router')>();
  return {
    ...actual,
    useParams: () => ({ marinaId: 'marina-1' }),
    useNavigate: () => vi.fn(),
    useRouter: () => ({ state: { location: { pathname: '/marina/marina-1/accounts' } } }),
    useSearch: () => ({ status: 'active' }),
  };
});

vi.mock('@/api/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/api')>();
  return {
    ...actual,
    getBillingAccounts: () => Promise.resolve([]),
  };
});

describe('CustomersPage', () => {
  it('renders all status filter chips', () => {
    renderWithProviders(<CustomersPage />);
    expect(screen.getByRole('button', { name: /^all$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^active$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^inactive$/i })).toBeTruthy();
  });

  it('marks the Active chip as active when status=active', () => {
    renderWithProviders(<CustomersPage />);
    const chip = screen.getByRole('button', { name: /^active$/i });
    expect(chip.className).toMatch(/border-primary/);
  });

  it('renders the search input', () => {
    renderWithProviders(<CustomersPage />);
    expect(screen.getByPlaceholderText(/search accounts/i)).toBeTruthy();
  });

  it('shows empty state when no accounts', async () => {
    renderWithProviders(<CustomersPage />);
    const empty = await screen.findByText(/no accounts found/i);
    expect(empty).toBeTruthy();
  });
});
