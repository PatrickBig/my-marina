import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { render } from '@testing-library/react';
import { OperatorButton } from './OperatorButton';
import { useAuthStore } from '@/store/authStore';

vi.mock('@tanstack/react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@tanstack/react-router')>();
  return { ...actual, useNavigate: () => vi.fn() };
});

function setMemberships(count: 0 | 1 | 2) {
  const mems = Array.from({ length: count }, (_, i) => ({
    scope: 'Marina' as const,
    tenantId: `tenant-${i}`,
    marinaId: `marina-id-${i}`,
    marinaName: `Test Marina ${i + 1}`,
    role: 'Owner' as const,
    tier: 'Pro',
  }));
  useAuthStore.setState({ memberships: mems });
}

beforeEach(() => {
  useAuthStore.setState({ memberships: [] });
});

describe('OperatorButton', () => {
  it('renders nothing when user has 0 marina memberships', () => {
    setMemberships(0);
    const { container } = render(<OperatorButton />);
    expect(container.firstChild).toBeNull();
  });

  it('renders a direct link button when user has 1 marina membership', () => {
    setMemberships(1);
    render(<OperatorButton />);
    const btn = screen.getByRole('button', { name: /open marina dashboard/i });
    expect(btn).toBeTruthy();
  });

  it('renders a dropdown trigger when user has 2+ marina memberships', () => {
    setMemberships(2);
    render(<OperatorButton />);
    const btn = screen.getByRole('button', { name: /select marina/i });
    expect(btn).toBeTruthy();
  });
});
