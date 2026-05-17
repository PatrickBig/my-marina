import { create } from 'zustand';
import { persist } from 'zustand/middleware';

type ThemePreference = 'light' | 'dark' | 'system';

interface ThemeStore {
  preference: ThemePreference;
  setPreference: (p: ThemePreference) => void;
  cyclePreference: () => void;
  resolvedTheme: () => 'light' | 'dark';
}

export const useThemeStore = create<ThemeStore>()(
  persist(
    (set, get) => ({
      preference: 'system',
      setPreference: (preference) => set({ preference }),
      cyclePreference: () => {
        const order: ThemePreference[] = ['light', 'dark', 'system'];
        const next = order[(order.indexOf(get().preference) + 1) % order.length];
        set({ preference: next });
      },
      resolvedTheme: () => {
        const { preference } = get();
        if (preference === 'system') {
          return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        }
        return preference;
      },
    }),
    { name: 'mymarina:theme', partialize: (s) => ({ preference: s.preference }) }
  )
);
