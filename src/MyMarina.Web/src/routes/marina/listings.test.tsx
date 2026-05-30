import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';
import { ListingsPage } from './listings';

vi.mock('@tanstack/react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@tanstack/react-router')>();
  return {
    ...actual,
    useParams: () => ({ marinaId: 'marina-1' }),
    useNavigate: () => vi.fn(),
    useRouter: () => ({ state: { location: { pathname: '/marina/marina-1/listings' } } }),
    useSearch: () => ({}),
  };
});

vi.mock('@/api/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/api')>();
  return {
    ...actual,
    getSlips: () => Promise.resolve([]),
    getAvailabilityWindows: () => Promise.resolve([]),
  };
});

describe('ListingsPage', () => {
  it('renders the page heading', () => {
    renderWithProviders(<ListingsPage />);
    expect(screen.getByRole('heading', { level: 1 })).toBeTruthy();
  });

  it('renders the slip picker table when no slipId in URL', () => {
    renderWithProviders(<ListingsPage />);
    // SlipPickerTable is rendered (no back button visible)
    expect(screen.queryByRole('button', { name: /back/i })).toBeNull();
  });

  it('shows empty state when no slips', async () => {
    renderWithProviders(<ListingsPage />);
    const empty = await screen.findByText(/no slips found/i);
    expect(empty).toBeTruthy();
  });
});
