import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';
import { AnnouncementsPage } from './announcements';

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
    getMarinaAnnouncements: () => Promise.resolve([]),
  };
});

describe('AnnouncementsPage', () => {
  it('renders the page heading', () => {
    renderWithProviders(<AnnouncementsPage />);
    expect(screen.getByRole('heading', { level: 1 })).toBeTruthy();
  });

  it('renders status filter chips', () => {
    renderWithProviders(<AnnouncementsPage />);
    // Two "All" chips exist: one for status filter, one for audience filter
    expect(screen.getAllByRole('button', { name: /^all$/i }).length).toBeGreaterThanOrEqual(1);
    expect(screen.getByRole('button', { name: /^draft$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^published$/i })).toBeTruthy();
  });

  it('shows empty state when no announcements', async () => {
    renderWithProviders(<AnnouncementsPage />);
    const empty = await screen.findByText(/no announcements found/i);
    expect(empty).toBeTruthy();
  });
});
