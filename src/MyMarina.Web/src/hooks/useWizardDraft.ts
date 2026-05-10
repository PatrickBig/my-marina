import { useCallback, useEffect, useRef, useState } from 'react';

interface DraftEnvelope<T> {
  data: T;
  savedAt: string; // ISO timestamp
}

function storageKey(marinaId: string) {
  return `marina-setup-${marinaId}`;
}

export function useWizardDraft<T>(marinaId: string | null, initialValue: T) {
  const [draft, setDraftState] = useState<T>(initialValue);
  const savedAtRef = useRef<string | null>(null);

  // Load from localStorage on mount
  useEffect(() => {
    if (!marinaId) return;
    const raw = localStorage.getItem(storageKey(marinaId));
    if (!raw) return;
    try {
      const envelope: DraftEnvelope<T> = JSON.parse(raw);
      setDraftState(envelope.data);
      savedAtRef.current = envelope.savedAt;
    } catch {
      // Corrupt data — ignore
    }
  }, [marinaId]);

  const setDraft = useCallback(
    (updater: T | ((prev: T) => T)) => {
      setDraftState((prev) => {
        const next = typeof updater === 'function' ? (updater as (p: T) => T)(prev) : updater;
        if (marinaId) {
          const envelope: DraftEnvelope<T> = {
            data: next,
            savedAt: new Date().toISOString(),
          };
          localStorage.setItem(storageKey(marinaId), JSON.stringify(envelope));
          savedAtRef.current = envelope.savedAt;
        }
        return next;
      });
    },
    [marinaId]
  );

  const clearDraft = useCallback(() => {
    if (!marinaId) return;
    localStorage.removeItem(storageKey(marinaId));
    savedAtRef.current = null;
  }, [marinaId]);

  // Returns the localStorage timestamp so callers can compare with backend updatedAt
  const localSavedAt = useCallback(() => savedAtRef.current, []);

  return { draft, setDraft, clearDraft, localSavedAt };
}
