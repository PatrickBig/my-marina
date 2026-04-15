import { create } from "zustand";
import { persist } from "zustand/middleware";

export interface AuthUser {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: number;
  tenantId: string | null;
  marinaId: string | null;
}

interface AuthState {
  token: string | null;
  user: AuthUser | null;
  login: (token: string, user: AuthUser) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      user: null,
      login: (token, user) => {
        localStorage.setItem("access_token", token);
        set({ token, user });
      },
      logout: () => {
        localStorage.removeItem("access_token");
        set({ token: null, user: null });
      },
    }),
    {
      name: "mymarina-auth",
      partialize: (state) => ({ token: state.token, user: state.user }),
    }
  )
);
