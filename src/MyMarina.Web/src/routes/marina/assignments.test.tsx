import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';
import { AssignmentsPage } from './assignments';

vi.mock('@tanstack/react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@tanstack/react-router')>();
  return {
    ...actual,
    useParams: () => ({ marinaId: 'marina-1' }),
    useNavigate: () => vi.fn(),
    useRouter: () => ({ state: { location: { pathname: '/marina/marina-1/assignments' } } }),
    useSearch: () => ({ type: 'Annual' }),
  };
});

vi.mock('@/api/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/api')>();
  return {
    ...actual,
    getSlipAssignments: () => Promise.resolve([]),
  };
});

describe('AssignmentsPage', () => {
  it('renders all type filter chips', () => {
    renderWithProviders(<AssignmentsPage />);
    expect(screen.getByRole('button', { name: /^all$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^annual$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^seasonal$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^monthly$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^transient$/i })).toBeTruthy();
  });

  it('marks the Annual chip active when type=Annual', () => {
    renderWithProviders(<AssignmentsPage />);
    const chip = screen.getByRole('button', { name: /^annual$/i });
    expect(chip.className).toMatch(/border-primary/);
  });

  it('renders the page heading', () => {
    renderWithProviders(<AssignmentsPage />);
    expect(screen.getByRole('heading', { level: 1 })).toBeTruthy();
  });

  it('shows empty state when no assignments', async () => {
    renderWithProviders(<AssignmentsPage />);
    const empty = await screen.findByText(/no assignments found/i);
    expect(empty).toBeTruthy();
  });
});
