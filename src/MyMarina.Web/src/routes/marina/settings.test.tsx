import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';
import { SettingsPage } from './settings';

vi.mock('@tanstack/react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@tanstack/react-router')>();
  return {
    ...actual,
    useParams: () => ({ marinaId: 'marina-1' }),
    useNavigate: () => vi.fn(),
    useSearch: () => ({}),
    useRouter: () => ({ navigate: vi.fn() }),
  };
});

vi.mock('@/api/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/api')>();
  return {
    ...actual,
    getMarina: () =>
      Promise.resolve({
        id: 'marina-1',
        name: 'Test Marina',
        subscriptionTier: 'Pro',
        photos: [],
      }),
    updateMarina: () => Promise.resolve({}),
  };
});

vi.mock('@/hooks/usePhotoUpload', () => ({
  usePhotoUpload: () => ({
    upload: vi.fn(),
    uploading: false,
    photos: [],
    setPhotos: vi.fn(),
    deletePhoto: vi.fn(),
  }),
}));

describe('SettingsPage', () => {
  it('renders the page heading', () => {
    renderWithProviders(<SettingsPage />);
    expect(screen.getByRole('heading', { level: 1 })).toBeTruthy();
  });

  it('renders tab navigation', () => {
    renderWithProviders(<SettingsPage />);
    expect(screen.getByText('Profile')).toBeTruthy();
    expect(screen.getByText('Address & Location')).toBeTruthy();
    expect(screen.getByText('Photos')).toBeTruthy();
    expect(screen.getByText('Subscription')).toBeTruthy();
  });

  it('shows profile tab by default', () => {
    renderWithProviders(<SettingsPage />);
    expect(screen.getByText('Profile')).toBeTruthy();
  });
});
