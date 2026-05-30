import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';
import { PricingPlansPage } from '@/pages/PricingPlansPage';

vi.mock('@tanstack/react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@tanstack/react-router')>();
  return {
    ...actual,
    useParams: () => ({ marinaId: 'marina-1' }),
    useNavigate: () => vi.fn(),
    useSearch: () => ({}),
  };
});

vi.mock('@/api/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/api')>();
  return {
    ...actual,
    getPricingPlans: () => Promise.resolve([]),
    getDocks: () => Promise.resolve([]),
  };
});

describe('PricingPlansPage', () => {
  it('renders the page heading', () => {
    renderWithProviders(<PricingPlansPage />);
    expect(screen.getByRole('heading', { level: 1 })).toBeTruthy();
  });

  it('shows the new plan form by default when no plans exist', () => {
    renderWithProviders(<PricingPlansPage />);
    // "Create plan" appears as both an h2 and a button; target the heading
    expect(screen.getByRole('heading', { name: /create plan/i })).toBeTruthy();
  });
});
