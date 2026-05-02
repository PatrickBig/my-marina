import { create } from 'zustand';
import { persist } from 'zustand/middleware';

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

  setAuth: (accessToken: string, refreshToken: string, expiresAt: string, user: UserProfile) => void;
  clearAuth: () => void;
  isAuthenticated: () => boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshToken: null,
      expiresAt: null,
      user: null,

      setAuth: (accessToken, refreshToken, expiresAt, user) =>
        set({ accessToken, refreshToken, expiresAt, user }),

      clearAuth: () =>
        set({ accessToken: null, refreshToken: null, expiresAt: null, user: null }),

      isAuthenticated: () => {
        const { accessToken, expiresAt } = get();
        if (!accessToken || !expiresAt) return false;
        return new Date(expiresAt) > new Date();
      },
    }),
    {
      name: 'mymarina-auth',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        expiresAt: state.expiresAt,
        user: state.user,
      }),
    }
  )
);
