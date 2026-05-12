interface Props {
  length: string;
  beam: string;
  draft: string;
  onLengthChange: (v: string) => void;
  onBeamChange: (v: string) => void;
  onDraftChange: (v: string) => void;
}

const inp = 'w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400';

export function VesselDimensionInputs({ length, beam, draft, onLengthChange, onBeamChange, onDraftChange }: Props) {
  return (
    <>
      <div>
        <label className="block text-xs font-medium text-slate-600 mb-1">LOA (ft)</label>
        <input type="number" step="0.1" value={length} onChange={(e) => onLengthChange(e.target.value)}
          placeholder="any" className={`${inp} w-20`} />
      </div>
      <div>
        <label className="block text-xs font-medium text-slate-600 mb-1">Beam (ft)</label>
        <input type="number" step="0.1" value={beam} onChange={(e) => onBeamChange(e.target.value)}
          placeholder="any" className={`${inp} w-20`} />
      </div>
      <div>
        <label className="block text-xs font-medium text-slate-600 mb-1">Draft (ft)</label>
        <input type="number" step="0.1" value={draft} onChange={(e) => onDraftChange(e.target.value)}
          placeholder="any" className={`${inp} w-20`} />
      </div>
    </>
  );
}
