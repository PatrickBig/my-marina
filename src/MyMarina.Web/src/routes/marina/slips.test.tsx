import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';
import { SlipsPage } from './slips';

vi.mock('@tanstack/react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@tanstack/react-router')>();
  return {
    ...actual,
    useParams: () => ({ marinaId: 'marina-1' }),
    useNavigate: () => vi.fn(),
    useRouter: () => ({ state: { location: { pathname: '/marina/marina-1/slips' } } }),
    useSearch: () => ({}),
  };
});

vi.mock('@/api/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/api')>();
  return {
    ...actual,
    getDocks: () => Promise.resolve([]),
    getSlips: () => Promise.resolve([]),
  };
});

describe('SlipsPage', () => {
  it('renders the page heading', () => {
    renderWithProviders(<SlipsPage />);
    expect(screen.getByRole('heading', { level: 1 })).toBeTruthy();
  });

  it('renders dock rail with All slips entry', () => {
    renderWithProviders(<SlipsPage />);
    expect(screen.getByText(/all slips/i)).toBeTruthy();
  });

  it('renders status filter chips', () => {
    renderWithProviders(<SlipsPage />);
    expect(screen.getByRole('button', { name: /^all$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^active$/i })).toBeTruthy();
  });

  it('shows empty state when no slips', async () => {
    renderWithProviders(<SlipsPage />);
    const empty = await screen.findByText(/no slips found/i);
    expect(empty).toBeTruthy();
  });
});
