import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';
import { StaffPage } from './staff';

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
    getMarinaStaff: () => Promise.resolve([]),
  };
});

describe('StaffPage', () => {
  it('renders the page heading', () => {
    renderWithProviders(<StaffPage />);
    expect(screen.getByRole('heading', { level: 1 })).toBeTruthy();
  });

  it('shows invite button', () => {
    renderWithProviders(<StaffPage />);
    expect(screen.getByRole('button', { name: /invite staff/i })).toBeTruthy();
  });

  it('shows empty state when no staff', async () => {
    renderWithProviders(<StaffPage />);
    const empty = await screen.findByText(/no staff yet/i);
    expect(empty).toBeTruthy();
  });
});
