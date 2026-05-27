import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';
import { MarinaWorkspaceLayout } from './MarinaWorkspaceLayout';
import { useAuthStore } from '@/store/authStore';

const TEST_MARINA_ID = 'marina-abc-123';

vi.mock('@/components/NavBar', () => ({
  NavBar: () => <header data-testid="mock-navbar" />,
}));

vi.mock('@tanstack/react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@tanstack/react-router')>();
  return {
    ...actual,
    useParams: () => ({ marinaId: TEST_MARINA_ID }),
    useLocation: () => ({ pathname: `/marina/${TEST_MARINA_ID}/dashboard` }),
    useNavigate: () => vi.fn(),
    Navigate: () => null,
    Link: ({ children, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement> & { children?: React.ReactNode }) => <a {...props}>{children}</a>,
    Outlet: () => <div data-testid="workspace-outlet">outlet content</div>,
  };
});

vi.mock('@/api/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/api')>();
  return {
    ...actual,
    getMarina: () => Promise.resolve({ id: TEST_MARINA_ID, name: 'Test Marina', marinaType: 'marina' }),
    getMarinaReservations: () => Promise.resolve([]),
    getMarinaInvoices: () => Promise.resolve([]),
    getMarinaWorkOrders: () => Promise.resolve([]),
  };
});

function setAuthorizedUser() {
  useAuthStore.setState({
    memberships: [{
      scope: 'Marina' as const,
      tenantId: 'tenant-1',
      marinaId: TEST_MARINA_ID,
      marinaName: 'Test Marina',
      role: 'Owner' as const,
      tier: 'Pro',
    }],
  });
}

beforeEach(() => {
  useAuthStore.setState({ memberships: [] });
});

describe('MarinaWorkspaceLayout', () => {
  it('renders shell with rail and outlet when user has access', () => {
    setAuthorizedUser();
    renderWithProviders(<MarinaWorkspaceLayout />);

    expect(screen.getByRole('complementary')).toBeTruthy(); // <aside> in MarinaRail
    expect(screen.getByTestId('workspace-outlet')).toBeTruthy();
  });

  it('redirects to / when user does not have marina access', () => {
    // memberships is empty — Navigate is mocked to return null
    renderWithProviders(<MarinaWorkspaceLayout />);

    expect(screen.queryByTestId('workspace-outlet')).toBeNull();
  });
});
