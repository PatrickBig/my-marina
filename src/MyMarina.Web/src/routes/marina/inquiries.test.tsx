import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';
import { InquiriesPage } from './inquiries';

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
    getMarinaLeaseInquiries: () => Promise.resolve([]),
  };
});

describe('InquiriesPage', () => {
  it('renders the page heading', () => {
    renderWithProviders(<InquiriesPage />);
    expect(screen.getByRole('heading', { level: 1 })).toBeTruthy();
  });

  it('shows status filter chips', () => {
    renderWithProviders(<InquiriesPage />);
    expect(screen.getByRole('button', { name: /^pending$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^approved$/i })).toBeTruthy();
    expect(screen.getByRole('button', { name: /^declined$/i })).toBeTruthy();
  });

  it('shows empty state when no inquiries', async () => {
    renderWithProviders(<InquiriesPage />);
    const empty = await screen.findByText(/no inquiries found/i);
    expect(empty).toBeTruthy();
  });
});
