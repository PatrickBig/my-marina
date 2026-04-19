import { create } from "zustand";
import { persist } from "zustand/middleware";
import { decodeJWT } from "@/lib/jwt";

export interface AuthUser {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  tenantId: string | null;
  marinaId: string | null;
}

interface AuthState {
  token: string | null;
  user: AuthUser | null;
  isDemo: boolean;
  login: (token: string, user: AuthUser) => void;
  loginDemo: (token: string, user: AuthUser) => void;
  logout: () => void;
}

export const DEMO_TOKEN_KEY = "demo_token";

function userFromClaims(token: string): AuthUser | null {
  const claims = decodeJWT(token);
  if (!claims) return null;
  return {
    userId: claims.sub ?? "",
    email: claims.email ?? "",
    firstName: claims.first_name ?? "",
    lastName: claims.last_name ?? "",
    role: claims.role ?? "",
    tenantId: claims.tenant_id ?? null,
    marinaId: claims.marina_id ?? null,
  };
}

/**
 * Synchronously consume ?demo_token= from the URL (or sessionStorage fallback)
 * before any React renders. This runs at module-load time so the router's
 * beforeLoad guards see a populated token and never redirect to /login.
 */
function bootstrapDemoSession(): { token: string | null; user: AuthUser | null; isDemo: boolean } {
  // 1. Check URL param first
  const params = new URLSearchParams(window.location.search);
  const urlToken = params.get("demo_token");
  if (urlToken) {
    const user = userFromClaims(urlToken);
    if (user) {
      // Persist to sessionStorage and scrub the URL
      sessionStorage.setItem(DEMO_TOKEN_KEY, urlToken);
      localStorage.removeItem("mymarina-auth"); // prevent persist rehydration overwrite
      params.delete("demo_token");
      const newSearch = params.toString();
      window.history.replaceState(null, "", window.location.pathname + (newSearch ? `?${newSearch}` : ""));
      return { token: urlToken, user, isDemo: true };
    }
  }

  // 2. Restore an existing demo session from sessionStorage (page refresh)
  const stored = sessionStorage.getItem(DEMO_TOKEN_KEY);
  if (stored) {
    const user = userFromClaims(stored);
    if (user) {
      localStorage.removeItem("mymarina-auth");
      return { token: stored, user, isDemo: true };
    }
    sessionStorage.removeItem(DEMO_TOKEN_KEY); // stale / invalid — clear it
  }

  return { token: null, user: null, isDemo: false };
}

const initialDemo = bootstrapDemoSession();

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      ...initialDemo,
      login: (token, user) => {
        sessionStorage.removeItem(DEMO_TOKEN_KEY);
        localStorage.setItem("access_token", token);
        set({ token, user, isDemo: false });
      },
      loginDemo: (token, user) => {
        localStorage.removeItem("access_token");
        localStorage.removeItem("mymarina-auth");
        sessionStorage.setItem(DEMO_TOKEN_KEY, token);
        set({ token, user, isDemo: true });
      },
      logout: () => {
        localStorage.removeItem("access_token");
        sessionStorage.removeItem(DEMO_TOKEN_KEY);
        set({ token: null, user: null, isDemo: false });
      },
    }),
    {
      name: "mymarina-auth",
      // Only persist non-demo sessions to localStorage
      partialize: (state) => (state.isDemo ? {} : { token: state.token, user: state.user }),
    }
  )
);
