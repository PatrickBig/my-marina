import { useEffect, useState } from 'react';
import { useAuthStore } from '@/store/authStore';
import { getVessels, type VesselDto } from '@/api/api';

const STORAGE_KEY = 'mymarina:lastSelectedVesselId';

export interface VesselDimensions {
  length: number;
  beam: number;
  draft: number;
}

interface Props {
  onSelect: (vesselId: string | null, dims: VesselDimensions | null) => void;
  onUseDifferentDimensions: () => void;
  onNoVessels?: () => void;
}

const sel = 'w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring';

export function VesselSelector({ onSelect, onUseDifferentDimensions, onNoVessels }: Props) {
  const { isAuthenticated } = useAuthStore();
  // Subscribe to the token directly so the effect re-runs on login/logout
  const accessToken = useAuthStore((s) => s.accessToken);
  const [vessels, setVessels] = useState<VesselDto[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  useEffect(() => {
    if (!isAuthenticated()) { onNoVessels?.(); return; }
    getVessels().then((list) => {
      const active = list.filter((v) => !v.isArchived).sort(
        (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
      );
      if (active.length === 0) { onNoVessels?.(); return; }

      const stored = localStorage.getItem(STORAGE_KEY);
      const initial = stored && active.some((v) => v.id === stored)
        ? stored
        : active[0].id;

      setVessels(active);
      setSelectedId(initial);
      localStorage.setItem(STORAGE_KEY, initial);

      const v = active.find((x) => x.id === initial);
      if (!v) { localStorage.removeItem(STORAGE_KEY); onNoVessels?.(); return; }
      onSelect(v.id, { length: v.length, beam: v.beam, draft: v.draft });
    }).catch(() => {});
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accessToken]);

  if (!isAuthenticated() || vessels.length === 0) return null;

  function handleChange(id: string) {
    setSelectedId(id);
    localStorage.setItem(STORAGE_KEY, id);
    const v = vessels.find((x) => x.id === id);
    if (v) onSelect(v.id, { length: v.length, beam: v.beam, draft: v.draft });
  }

  const selected = vessels.find((v) => v.id === selectedId);

  return (
    <div>
      <label className="block text-xs font-medium text-foreground/80 mb-1">Boat</label>
      <div className="flex items-center gap-2">
        <select value={selectedId ?? ''} onChange={(e) => handleChange(e.target.value)} className={`${sel} w-52`}>
          {vessels.map((v) => (
            <option key={v.id} value={v.id}>
              {v.name} ({v.length}′×{v.beam}′×{v.draft}′)
            </option>
          ))}
        </select>
        <button
          type="button"
          onClick={onUseDifferentDimensions}
          className="text-xs text-muted-foreground/70 hover:text-foreground/80 underline whitespace-nowrap"
        >
          use different dimensions
        </button>
      </div>
      {selected && (
        <p className="text-xs text-muted-foreground/70 mt-0.5">
          LOA {selected.length}′ · Beam {selected.beam}′ · Draft {selected.draft}′
        </p>
      )}
    </div>
  );
}
