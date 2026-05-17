import { create } from 'zustand';
import { persist } from 'zustand/middleware';

type ThemePreference = 'light' | 'dark' | 'system';

function compute(pref: ThemePreference): 'light' | 'dark' {
  if (pref !== 'system') return pref;
  return typeof window !== 'undefined' && window.matchMedia('(prefers-color-scheme: dark)').matches
    ? 'dark'
    : 'light';
}

interface ThemeStore {
  preference: ThemePreference;
  resolvedTheme: 'light' | 'dark';
  setPreference: (p: ThemePreference) => void;
  cyclePreference: () => void;
  applySystemPreference: (systemIsDark: boolean) => void;
}

export const useThemeStore = create<ThemeStore>()(
  persist(
    (set, get) => ({
      preference: 'system',
      resolvedTheme: compute('system'),

      setPreference: (preference) =>
        set({ preference, resolvedTheme: compute(preference) }),

      cyclePreference: () => {
        const order: ThemePreference[] = ['light', 'dark', 'system'];
        const preference = order[(order.indexOf(get().preference) + 1) % order.length];
        set({ preference, resolvedTheme: compute(preference) });
      },

      applySystemPreference: (systemIsDark) => {
        if (get().preference === 'system') {
          set({ resolvedTheme: systemIsDark ? 'dark' : 'light' });
        }
      },
    }),
    {
      name: 'mymarina:theme',
      partialize: (s) => ({ preference: s.preference }),
      onRehydrateStorage: () => (state) => {
        if (state) state.resolvedTheme = compute(state.preference);
      },
    }
  )
);
