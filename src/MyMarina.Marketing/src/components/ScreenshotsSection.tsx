import { useState, useEffect, useCallback } from 'react';

const screenshots = [
  {
    src: '/screenshots/operator-dashboard.png',
    alt: 'MyMarina operator dashboard showing marina overview, slip occupancy, and quick stats',
    caption: 'Operator Dashboard',
  },
  {
    src: '/screenshots/customer-portal.png',
    alt: "Customer self-service portal showing slip details, boats, and upcoming bookings",
    caption: 'Customer Portal',
  },
  {
    src: '/screenshots/invoicing.png',
    alt: 'Invoicing screen with line items, payment status, and bulk actions',
    caption: 'Invoicing',
  },
  {
    src: '/screenshots/pricing-rules.png',
    alt: 'Pricing rules management page — create base rates and surcharges with size brackets, amenity filters, and scheduled future-dated price changes',
    caption: 'Pricing Rules',
  },
  {
    src: '/screenshots/slip-adjustments.png',
    alt: 'Per-slip price adjustments tab — apply named flat or per-foot deltas on top of marina-wide pricing rules for individual slips',
    caption: 'Per-Slip Price Adjustments',
  },
];

function ArrowLeftIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" className="w-8 h-8 drop-shadow-lg">
      <path d="m15 18-6-6 6-6" />
    </svg>
  );
}

function ArrowRightIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" className="w-8 h-8 drop-shadow-lg">
      <path d="m9 18 6-6-6-6" />
    </svg>
  );
}

function CloseIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" className="w-6 h-6 drop-shadow-lg">
      <path d="M18 6 6 18" />
      <path d="m6 6 12 12" />
    </svg>
  );
}

interface ImageDialogProps {
  isOpen: boolean;
  currentIndex: number;
  onClose: () => void;
  onNext: () => void;
  onPrev: () => void;
}

function ImageDialog({ isOpen, currentIndex, onClose, onNext, onPrev }: ImageDialogProps) {
  const [closing, setClosing] = useState(false);

  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
      return () => { document.body.style.overflow = ''; };
    }
  }, [isOpen]);

  const handleBackdropClick = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    if (e.target === e.currentTarget) {
      setClosing(true);
      setTimeout(onClose, 200);
    }
  }, [onClose]);

  useEffect(() => {
    if (!isOpen) return;
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setClosing(true);
        setTimeout(onClose, 200);
      }
      if (e.key === 'ArrowLeft') onPrev();
      if (e.key === 'ArrowRight') onNext();
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose, onPrev, onNext]);

  if (!isOpen) return null;

  const shot = screenshots[currentIndex];

  return (
    <div
      className="fixed inset-0 bg-black/90 z-50 flex items-center justify-center transition-opacity duration-200"
      onClick={handleBackdropClick}
      role="dialog"
      aria-modal="true"
      aria-label="Screenshot preview"
    >
      <div className={`relative w-[95vw] max-w-screen-xl transition-opacity duration-200 ${closing ? 'opacity-0' : 'opacity-100'}`}>
        <button
          onClick={() => { setClosing(true); setTimeout(onClose, 200); }}
          className="fixed absolute right-6 top-6 p-2 text-white/70 hover:text-white hover:bg-white/10 rounded-full transition-colors cursor-pointer z-[60]"
          aria-label="Close preview"
        >
          <CloseIcon />
        </button>

        {/* Image area with arrows spaced from image */}
        <div className="flex items-center justify-center relative">
          {/* Left arrow */}
          <button
            onClick={(e) => { e.stopPropagation(); onPrev(); }}
            className="absolute left-4 z-[60] p-3 text-white/90 hover:text-white hover:bg-white/10 rounded-full transition-colors cursor-pointer border border-white/20 bg-black/40 backdrop-blur-sm"
            aria-label="Previous screenshot"
          >
            <ArrowLeftIcon />
          </button>

          {/* Image */}
          <img
            src={shot.src}
            alt={shot.alt}
            className="object-contain"
            style={{ maxHeight: 'calc(100vh - 160px)', width: 'auto' }}
          />

          {/* Right arrow */}
          <button
            onClick={(e) => { e.stopPropagation(); onNext(); }}
            className="absolute right-4 z-[60] p-3 text-white/90 hover:text-white hover:bg-white/10 rounded-full transition-colors cursor-pointer border border-white/20 bg-black/40 backdrop-blur-sm"
            aria-label="Next screenshot"
          >
            <ArrowRightIcon />
          </button>
        </div>

        {/* Counter + Caption */}
        <div className="text-center pt-3">
          <p className="text-white/90 text-base font-medium">{shot.caption}</p>
          <p className="text-white/50 text-xs mt-1">{currentIndex + 1} / {screenshots.length}</p>
        </div>
      </div>
    </div>
  );
}

export function ScreenshotsSection() {
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);

  const openDialog = useCallback((index: number) => {
    setSelectedIndex(index);
    setDialogOpen(true);
  }, []);

  const handleClose = useCallback(() => {
    setDialogOpen(false);
  }, []);

  const handleNext = useCallback(() => {
    setSelectedIndex(prev => prev !== null ? (prev + 1) % screenshots.length : 0);
  }, []);

  const handlePrev = useCallback(() => {
    setSelectedIndex(prev => prev !== null ? (prev - 1 + screenshots.length) % screenshots.length : 0);
  }, []);

  const currentIndex = selectedIndex !== null ? selectedIndex : 0;

  return (
    <section id="screenshots" className="py-20 px-4 sm:px-6 bg-secondary/10">
      <div className="max-w-6xl mx-auto">
        <div className="text-center mb-14">
          <h2 className="text-3xl font-bold mb-3">See it in action</h2>
          <p className="text-muted-foreground text-lg max-w-xl mx-auto">
            A clean, modern UI designed for daily use by marina operators and their customers.
          </p>
        </div>

        <div className="mb-10 rounded-xl overflow-hidden border border-border shadow-sm bg-secondary/20 aspect-video flex items-center justify-center">
          <div className="text-center text-muted-foreground px-8">
            <div className="text-4xl mb-3" aria-hidden="true">&#9654;</div>
            <p className="font-medium">Product walkthrough coming soon</p>
            <p className="text-sm mt-1">Try the live demo above to experience MyMarina now.</p>
          </div>
        </div>

        <div className="grid sm:grid-cols-3 gap-6">
          {screenshots.map((shot, index) => (
            <figure
              key={shot.src}
              onClick={() => openDialog(index)}
              className={`rounded-xl overflow-hidden border border-border shadow-sm bg-secondary/20 cursor-pointer transition-all duration-150 ${
                selectedIndex === index ? 'ring-2 ring-primary scale-[1.02]' : 'hover:shadow-md hover:ring-1 hover:ring-primary/30'
              }`}
            >
              <img
                src={shot.src}
                alt={shot.alt}
                className="w-full aspect-video object-cover object-top"
                loading="lazy"
              />
              <figcaption className="px-4 py-3 text-sm font-medium text-center">{shot.caption}</figcaption>
            </figure>
          ))}
        </div>
      </div>

      {/* key prop forces remount on close so internal state resets */}
      <ImageDialog
        key={`dialog-${dialogOpen}`}
        isOpen={dialogOpen}
        currentIndex={currentIndex}
        onClose={handleClose}
        onNext={handleNext}
        onPrev={handlePrev}
      />
    </section>
  );
}
