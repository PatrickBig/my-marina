import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { MembershipClaim } from '@/api/api';

export interface UserProfile {
  id: string;
  email: string;
  emailConfirmed: boolean;
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
  profilePhotoUrl?: string | null;
  marketingOptIn: boolean;
}

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  expiresAt: string | null;
  user: UserProfile | null;
  memberships: MembershipClaim[];
  isPlatformOperator: boolean;
  isDemo: boolean;

  setAuth: (accessToken: string, refreshToken: string | null, expiresAt: string, user: UserProfile, memberships?: MembershipClaim[], isPlatformOperator?: boolean, isDemo?: boolean) => void;
  clearAuth: () => void;
  isAuthenticated: () => boolean;
  hasMarinaAccess: () => boolean;
  marinaMemberships: () => MembershipClaim[];
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshToken: null,
      expiresAt: null,
      user: null,
      memberships: [],
      isPlatformOperator: false,
      isDemo: false,

      setAuth: (accessToken, refreshToken, expiresAt, user, memberships = [], isPlatformOperator = false, isDemo = false) =>
        set({ accessToken, refreshToken, expiresAt, user, memberships, isPlatformOperator, isDemo }),

      clearAuth: () =>
        set({ accessToken: null, refreshToken: null, expiresAt: null, user: null, memberships: [], isPlatformOperator: false, isDemo: false }),

      isAuthenticated: () => {
        const { accessToken, expiresAt } = get();
        if (!accessToken || !expiresAt) return false;
        return new Date(expiresAt) > new Date();
      },

      hasMarinaAccess: () =>
        get().memberships.some((m) => m.scope === 'Marina'),

      marinaMemberships: () =>
        get().memberships.filter((m) => m.scope === 'Marina'),
    }),
    {
      name: 'mymarina-auth',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        expiresAt: state.expiresAt,
        user: state.user,
        memberships: state.memberships,
        isPlatformOperator: state.isPlatformOperator,
        isDemo: state.isDemo,
      }),
    }
  )
);
