import { useCallback } from 'react';
import { useSearch, useNavigate, useRouter } from '@tanstack/react-router';

/**
 * Read/write a single TanStack Router search param as UI state with a default fallback.
 * Set the value to the default (or empty/undefined) to drop the param from the URL.
 */
export function useUrlState<T extends string>(
  key: string,
  defaultValue: T,
): [T, (next: T) => void] {
  const search = useSearch({ strict: false }) as Record<string, unknown>;
  const navigate = useNavigate();
  const router = useRouter();

  const raw = search[key];
  const value = (typeof raw === 'string' ? raw : defaultValue) as T;

  const setValue = useCallback(
    (next: T) => {
      navigate({
        to: router.state.location.pathname,
        search: (prev: Record<string, unknown>) => {
          const merged = { ...prev };
          if (next === defaultValue || next === '' || next == null) {
            delete merged[key];
          } else {
            merged[key] = next;
          }
          // Reset pagination whenever a filter-style param changes
          if (key !== 'page' && 'page' in merged) {
            delete merged.page;
          }
          return merged;
        },
        replace: false,
      });
    },
    [key, defaultValue, navigate, router],
  );

  return [value, setValue];
}

/**
 * Pair the page number to URL state. Default is 1; when on page 1 the param is
 * dropped from the URL to keep URLs clean.
 */
export function usePaginationState(): [number, (n: number) => void] {
  const [raw, setRaw] = useUrlState('page', '1');
  const page = Math.max(1, parseInt(raw as string, 10) || 1);
  const setPage = useCallback((n: number) => setRaw(String(n) as '1'), [setRaw]);
  return [page, setPage];
}
