import { useCallback, useRef, useState } from 'react';
import ReactCrop, { type Crop, type PixelCrop, centerCrop, makeAspectCrop } from 'react-image-crop';
import 'react-image-crop/dist/ReactCrop.css';

interface Props {
  aspectRatio?: number;
  onComplete: (blob: Blob, width: number, height: number) => void;
  onCancel: () => void;
}

export function CropUploadModal({ aspectRatio, onComplete, onCancel }: Props) {
  const [src, setSrc] = useState<string | null>(null);
  const [crop, setCrop] = useState<Crop>();
  const [completedCrop, setCompletedCrop] = useState<PixelCrop>();
  const imgRef = useRef<HTMLImageElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  function onFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (ev) => setSrc(ev.target?.result as string);
    reader.readAsDataURL(file);
  }

  function onImageLoad(e: React.SyntheticEvent<HTMLImageElement>) {
    const { naturalWidth: width, naturalHeight: height } = e.currentTarget;
    const c = centerCrop(
      makeAspectCrop({ unit: '%', width: 90 }, aspectRatio ?? width / height, width, height),
      width, height
    );
    setCrop(c);
  }

  const handleConfirm = useCallback(() => {
    if (!completedCrop || !imgRef.current) return;
    const image = imgRef.current;
    const canvas = document.createElement('canvas');
    const scaleX = image.naturalWidth / image.width;
    const scaleY = image.naturalHeight / image.height;
    canvas.width = completedCrop.width * scaleX;
    canvas.height = completedCrop.height * scaleY;
    const ctx = canvas.getContext('2d')!;
    ctx.drawImage(
      image,
      completedCrop.x * scaleX,
      completedCrop.y * scaleY,
      completedCrop.width * scaleX,
      completedCrop.height * scaleY,
      0, 0, canvas.width, canvas.height
    );
    canvas.toBlob((blob) => {
      if (blob) onComplete(blob, Math.round(canvas.width), Math.round(canvas.height));
    }, 'image/jpeg', 0.85);
  }, [completedCrop, onComplete]);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-card rounded-2xl border border-border shadow-xl p-6 w-full max-w-lg space-y-4">
        <h2 className="text-sm font-semibold text-foreground">Upload photo</h2>

        {!src && (
          <div className="flex flex-col items-center justify-center border-2 border-dashed border-border rounded-xl py-10 gap-3">
            <p className="text-sm text-muted-foreground">Select an image to crop and upload</p>
            <input ref={fileInputRef} type="file" accept="image/*" className="hidden" onChange={onFileChange} />
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              className="rounded-lg bg-primary text-primary-foreground px-4 py-2 text-sm font-medium"
            >
              Choose file
            </button>
          </div>
        )}

        {src && (
          <div className="overflow-auto max-h-96">
            <ReactCrop
              crop={crop}
              onChange={(c) => setCrop(c)}
              onComplete={(c) => setCompletedCrop(c)}
              aspect={aspectRatio}
              keepSelection
            >
              <img ref={imgRef} src={src} onLoad={onImageLoad} className="max-w-full" />
            </ReactCrop>
          </div>
        )}

        <div className="flex justify-between pt-2">
          <button type="button" onClick={onCancel}
            className="rounded-lg border border-border bg-card px-4 py-2 text-sm font-medium hover:bg-muted/30">
            Cancel
          </button>
          {src && (
            <button type="button" onClick={handleConfirm} disabled={!completedCrop}
              className="rounded-lg bg-primary text-primary-foreground px-4 py-2 text-sm font-medium disabled:opacity-50">
              Crop & upload
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
