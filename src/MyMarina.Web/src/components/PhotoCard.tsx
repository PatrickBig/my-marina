import { useState } from 'react';
import type { MarinaPhotoDto } from '@/hooks/usePhotoUpload';

interface Props {
  photo: MarinaPhotoDto;
  isFirst: boolean;
  isLast: boolean;
  onMoveUp: () => void;
  onMoveDown: () => void;
  onDelete: () => void;
  caption?: string;
  onCaptionChange?: (caption: string) => void;
  showCaption?: boolean;
  latitude?: number | null;
  longitude?: number | null;
  onLatLngChange?: (lat: number | null, lng: number | null) => void;
  showLatLng?: boolean;
}

export function PhotoCard({
  photo,
  isFirst,
  isLast,
  onMoveUp,
  onMoveDown,
  onDelete,
  showCaption = false,
  onCaptionChange,
  showLatLng = false,
  onLatLngChange,
}: Props) {
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [captionDraft, setCaptionDraft] = useState(photo.caption ?? '');
  const [latDraft, setLatDraft] = useState(photo.latitude?.toString() ?? '');
  const [lngDraft, setLngDraft] = useState(photo.longitude?.toString() ?? '');

  const isProcessing = !photo.urlThumbnail;

  function handleDeleteClick() {
    if (confirmDelete) {
      onDelete();
    } else {
      setConfirmDelete(true);
    }
  }

  function handleCaptionBlur() {
    onCaptionChange?.(captionDraft);
  }

  function handleLatLngBlur() {
    const lat = latDraft !== '' ? parseFloat(latDraft) : null;
    const lng = lngDraft !== '' ? parseFloat(lngDraft) : null;
    onLatLngChange?.(lat, lng);
  }

  return (
    <div
      className="relative rounded-xl border border-border bg-card overflow-hidden group"
      onMouseLeave={() => setConfirmDelete(false)}
    >
      {/* Image or skeleton */}
      <div className="aspect-square w-full bg-muted relative">
        {isProcessing ? (
          <div className="absolute inset-0 flex flex-col items-center justify-center gap-2 animate-pulse">
            <div className="w-8 h-8 rounded-full bg-muted-foreground/20" />
            <span className="text-xs text-muted-foreground">Processing…</span>
          </div>
        ) : (
          <img
            src={photo.urlThumbnail!}
            alt={photo.caption ?? 'Marina photo'}
            className="w-full h-full object-cover"
          />
        )}
      </div>

      {/* Action bar */}
      <div className="absolute top-1 right-1 flex flex-col gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
        <button
          type="button"
          onClick={onMoveUp}
          disabled={isFirst}
          title="Move up"
          className="rounded-md bg-black/60 text-white p-1 disabled:opacity-30 hover:bg-black/80 text-xs"
        >
          ↑
        </button>
        <button
          type="button"
          onClick={onMoveDown}
          disabled={isLast}
          title="Move down"
          className="rounded-md bg-black/60 text-white p-1 disabled:opacity-30 hover:bg-black/80 text-xs"
        >
          ↓
        </button>
        <button
          type="button"
          onClick={handleDeleteClick}
          title={confirmDelete ? 'Click again to confirm' : 'Delete'}
          className={`rounded-md p-1 text-xs transition-colors ${
            confirmDelete
              ? 'bg-destructive text-destructive-foreground'
              : 'bg-black/60 text-white hover:bg-destructive hover:text-destructive-foreground'
          }`}
        >
          {confirmDelete ? '✓' : '✕'}
        </button>
      </div>

      {/* Caption / lat-lng inputs */}
      {(showCaption || showLatLng) && (
        <div className="p-2 space-y-1">
          {showCaption && (
            <input
              type="text"
              placeholder="Caption (optional)"
              value={captionDraft}
              onChange={(e) => setCaptionDraft(e.target.value)}
              onBlur={handleCaptionBlur}
              className="w-full rounded-md border border-border bg-background px-2 py-1 text-xs text-foreground placeholder:text-muted-foreground"
            />
          )}
          {showLatLng && (
            <div className="flex gap-1">
              <input
                type="number"
                placeholder="Lat"
                value={latDraft}
                onChange={(e) => setLatDraft(e.target.value)}
                onBlur={handleLatLngBlur}
                step="any"
                className="w-1/2 rounded-md border border-border bg-background px-2 py-1 text-xs text-foreground placeholder:text-muted-foreground"
              />
              <input
                type="number"
                placeholder="Lng"
                value={lngDraft}
                onChange={(e) => setLngDraft(e.target.value)}
                onBlur={handleLatLngBlur}
                step="any"
                className="w-1/2 rounded-md border border-border bg-background px-2 py-1 text-xs text-foreground placeholder:text-muted-foreground"
              />
            </div>
          )}
        </div>
      )}
    </div>
  );
}
