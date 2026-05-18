import { useEffect, useRef, useState } from 'react';
import { MapContainer, TileLayer, Marker, Popup, useMap, useMapEvents } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { NavBar } from '@/components/NavBar';
import { VesselSelector, type VesselDimensions } from '@/components/VesselSelector';
import { VesselDimensionInputs } from '@/components/VesselDimensionInputs';
import { useAuthStore } from '@/store/authStore';
import { searchMarinaRollup, type MarinaRollupResultDto, type ListingKind, type LeaseTerm } from '@/api/api';

delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

const DEFAULT_LAT = 38.9784;
const DEFAULT_LON = -76.4922;

interface Bounds { north: number; south: number; east: number; west: number }

function MapController({
  center,
  onBoundsChange,
  onUserMoved,
}: {
  center: { lat: number; lon: number } | null;
  onBoundsChange: (b: Bounds) => void;
  onUserMoved: () => void;
}) {
  const map = useMap();
  const programmaticMove = useRef(false);

  useEffect(() => {
    if (!center) return;
    programmaticMove.current = true;
    map.setView([center.lat, center.lon], 10);
    setTimeout(() => { programmaticMove.current = false; }, 500);
  }, [center, map]);

  useMapEvents({
    moveend() {
      const b = map.getBounds();
      onBoundsChange({
        north: b.getNorth(), south: b.getSouth(),
        east: b.getEast(),  west: b.getWest(),
      });
      if (!programmaticMove.current) onUserMoved();
    },
  });

  return null;
}

// Photo placeholder shown when marina has no photo yet
function MarinaPhotoPlaceholder() {
  return (
    <div
      className="w-20 h-20 shrink-0 rounded-lg flex items-center justify-center text-white/70 text-xl"
      style={{ background: 'linear-gradient(135deg, oklch(52% 0.18 235) 0%, oklch(80% 0.10 185) 100%)' }}
    >
      ⚓
    </div>
  );
}

function AmenityBadge({ label, color }: { label: string; color: string }) {
  return (
    <span className={`text-xs px-1.5 py-0.5 rounded-full font-medium ${color}`}>
      {label}
    </span>
  );
}

function FilterChip({ label, active, onClick }: { label: string; active: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`text-xs px-3 py-1 rounded-full border font-medium transition-colors ${
        active
          ? 'bg-primary text-primary-foreground border-primary'
          : 'bg-background text-muted-foreground border-border hover:border-primary/50 hover:text-foreground'
      }`}
    >
      {label}
    </button>
  );
}

const inputCls = 'w-full rounded-lg border border-border bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring';

export function SearchPage() {
  const today    = new Date().toISOString().slice(0, 10);
  const tomorrow = new Date(Date.now() + 86400000).toISOString().slice(0, 10);

  const { isAuthenticated } = useAuthStore();
  const authed = isAuthenticated();

  const [listingKind, setListingKind] = useState<ListingKind>('Transient');
  const [arrivesAt, setArrivesAt]     = useState(today);
  const [departsAt, setDepartsAt]     = useState(tomorrow);
  const [leaseTerm, setLeaseTerm]     = useState<LeaseTerm | ''>('');
  const [locationText, setLocationText] = useState('');

  const [priceMin, setPriceMin] = useState('');
  const [priceMax, setPriceMax] = useState('');

  // Amenity filter chips
  const [chipInstant,  setChipInstant]  = useState(false);
  const [chipElectric, setChipElectric] = useState(false);
  const [chipPumpOut,  setChipPumpOut]  = useState(false);
  const [chipCovered,  setChipCovered]  = useState(false);

  const [vesselDims, setVesselDims]     = useState<VesselDimensions | null>(null);
  const [manualLength, setManualLength] = useState('');
  const [manualBeam, setManualBeam]     = useState('');
  const [manualDraft, setManualDraft]   = useState('');
  const [showManual, setShowManual]     = useState(!authed);
  const [hasVessels, setHasVessels]     = useState(false);

  const [results, setResults]           = useState<MarinaRollupResultDto[]>([]);
  const [loading, setLoading]           = useState(false);
  const [error, setError]               = useState<string | null>(null);
  const [searched, setSearched]         = useState(false);
  const [hoveredId, setHoveredId]       = useState<string | null>(null);
  const [showSearchHere, setShowSearchHere] = useState(false);

  const [mapCenter, setMapCenter]       = useState<{ lat: number; lon: number } | null>(null);
  const boundsRef                       = useRef<Bounds | null>(null);
  const locating                        = useRef(false);
  const abortRef                        = useRef<AbortController | null>(null);

  useEffect(() => {
    if (locating.current) return;
    locating.current = true;
    navigator.geolocation?.getCurrentPosition(
      (pos) => runSearchAtCenter(pos.coords.latitude, pos.coords.longitude),
      () => {}
    );
  }, []);

  async function geocodeLocation(query: string) {
    try {
      const url = `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(query)}&format=json&limit=1&countrycodes=us`;
      const res  = await fetch(url, { headers: { 'Accept-Language': 'en' } });
      const data = await res.json();
      if (data.length === 0) return null;
      return { lat: parseFloat(data[0].lat), lon: parseFloat(data[0].lon) };
    } catch { return null; }
  }

  function vesselParams() {
    if (showManual) {
      return {
        vesselLength: manualLength ? Number(manualLength) : undefined,
        vesselBeam:   manualBeam   ? Number(manualBeam)   : undefined,
        vesselDraft:  manualDraft  ? Number(manualDraft)  : undefined,
      };
    }
    return vesselDims
      ? { vesselLength: vesselDims.length, vesselBeam: vesselDims.beam, vesselDraft: vesselDims.draft }
      : {};
  }

  async function runSearchWithBounds(bounds: Bounds) {
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setLoading(true);
    setError(null);
    setSearched(true);
    setShowSearchHere(false);
    try {
      const vp = vesselParams();
      const isLease = listingKind === 'Lease';
      const data = await searchMarinaRollup({
        north: bounds.north, south: bounds.south,
        east:  bounds.east,  west:  bounds.west,
        listingKind,
        arrivesAt:      !isLease ? arrivesAt : undefined,
        departsAt:      !isLease ? departsAt : undefined,
        leaseTerm:      isLease && leaseTerm ? leaseTerm : undefined,
        priceMin:       priceMin ? Number(priceMin) : undefined,
        priceMax:       priceMax ? Number(priceMax) : undefined,
        instantBookOnly: chipInstant   || undefined,
        hasPumpOut:      chipPumpOut   || undefined,
        isAnyCovered:    chipCovered   || undefined,
        hasElectric:     chipElectric  || undefined,
        ...vp,
      }, controller.signal);
      setResults(data);
    } catch (err) {
      if (err instanceof Error && err.name === 'CanceledError') return;
      setError('Search failed. Make sure the API is running.');
    } finally {
      setLoading(false);
    }
  }

  function runSearchAtCenter(lat: number, lon: number) {
    setMapCenter({ lat, lon });
    const delta = 0.4;
    runSearchWithBounds({ north: lat + delta, south: lat - delta, east: lon + delta, west: lon - delta });
  }

  async function handleSearch(e?: React.FormEvent) {
    e?.preventDefault();
    setError(null);

    if (locationText.trim()) {
      setLoading(true);
      const geo = await geocodeLocation(locationText.trim());
      setLoading(false);
      if (!geo) {
        setError(`Could not find "${locationText}". Try a city, state or zip.`);
        return;
      }
      runSearchAtCenter(geo.lat, geo.lon);
    } else if (boundsRef.current) {
      runSearchWithBounds(boundsRef.current);
    }
  }

  function navigateToMarina(marinaId: string) {
    const params = new URLSearchParams({
      listingKind,
      arrivesAt,
      departsAt,
      ...(leaseTerm ? { leaseTerm } : {}),
      ...(vesselDims && !showManual
        ? { vesselLength: String(vesselDims.length), vesselBeam: String(vesselDims.beam), vesselDraft: String(vesselDims.draft) }
        : {}),
      ...(showManual
        ? {
            ...(manualLength ? { vesselLength: manualLength } : {}),
            ...(manualBeam   ? { vesselBeam:   manualBeam   } : {}),
            ...(manualDraft  ? { vesselDraft:  manualDraft  } : {}),
          }
        : {}),
    });
    window.location.href = `/search/marinas/${marinaId}?${params.toString()}`;
  }

  function handleListingKindChange(k: ListingKind) {
    setListingKind(k);
    setPriceMin('');
    setPriceMax('');
    setResults([]);
    setSearched(false);
  }

  const summaryText = searched
    ? `${results.length} marina${results.length !== 1 ? 's' : ''} in view`
    : null;

  return (
    <div className="flex flex-col h-screen bg-background overflow-hidden">
      <NavBar />

      {/* Filter bar */}
      <div className="bg-card border-b border-border shadow-sm shrink-0">
        <div className="px-4 pt-3 pb-3">
          {/* Transient / Lease toggle */}
          <div className="flex gap-1 mb-3 w-fit rounded-lg bg-muted p-1">
            {(['Transient', 'Lease'] as ListingKind[]).map((k) => (
              <button key={k} type="button"
                onClick={() => handleListingKindChange(k)}
                className={`px-4 py-1.5 rounded-md text-sm font-medium transition-colors ${
                  listingKind === k
                    ? 'bg-card shadow text-foreground'
                    : 'text-muted-foreground hover:text-foreground'}`}>
                {k === 'Transient' ? 'Transient dockage' : 'Seasonal / annual lease'}
              </button>
            ))}
          </div>

          <form onSubmit={handleSearch} className="flex flex-wrap gap-3 items-end">
            {/* Location */}
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">Location</label>
              <div className="flex gap-1.5">
                <input type="text" value={locationText} onChange={(e) => setLocationText(e.target.value)}
                  placeholder="City, state or zip" className={`${inputCls} w-48`} />
                <button type="button" title="Use GPS"
                  onClick={() => navigator.geolocation?.getCurrentPosition(
                    (p) => { setLocationText(''); runSearchAtCenter(p.coords.latitude, p.coords.longitude); },
                    () => alert('Location access denied.'))}
                  className="rounded-lg border border-border px-2 text-muted-foreground hover:text-foreground hover:bg-muted text-sm transition-colors">
                  📍
                </button>
              </div>
            </div>

            {/* Dates */}
            {listingKind === 'Transient' && (
              <>
                <div>
                  <label className="block text-xs font-medium text-muted-foreground mb-1">Arrival</label>
                  <input type="date" value={arrivesAt} onChange={(e) => setArrivesAt(e.target.value)} className={`${inputCls} w-36`} />
                </div>
                <div>
                  <label className="block text-xs font-medium text-muted-foreground mb-1">Departure</label>
                  <input type="date" value={departsAt} onChange={(e) => setDepartsAt(e.target.value)} className={`${inputCls} w-36`} />
                </div>
              </>
            )}
            {listingKind === 'Lease' && (
              <>
                <div>
                  <label className="block text-xs font-medium text-muted-foreground mb-1">Desired start</label>
                  <input type="date" value={arrivesAt} onChange={(e) => setArrivesAt(e.target.value)} className={`${inputCls} w-36`} />
                </div>
                <div>
                  <label className="block text-xs font-medium text-muted-foreground mb-1">Lease term</label>
                  <select value={leaseTerm} onChange={(e) => setLeaseTerm(e.target.value as LeaseTerm | '')} className={`${inputCls} w-32`}>
                    <option value="">Any</option>
                    <option value="Monthly">Monthly</option>
                    <option value="Seasonal">Seasonal</option>
                    <option value="Annual">Annual</option>
                  </select>
                </div>
              </>
            )}

            {/* Price range */}
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">Price min</label>
              <input type="number" min="0" step="1" value={priceMin} onChange={(e) => setPriceMin(e.target.value)}
                placeholder="$" className={`${inputCls} w-24`} />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted-foreground mb-1">Price max</label>
              <input type="number" min="0" step="1" value={priceMax} onChange={(e) => setPriceMax(e.target.value)}
                placeholder="$" className={`${inputCls} w-24`} />
            </div>

            {/* Vessel */}
            {!showManual ? (
              <VesselSelector
                onSelect={(_id, dims) => { setVesselDims(dims); setHasVessels(true); }}
                onUseDifferentDimensions={() => { setShowManual(true); setVesselDims(null); }}
                onNoVessels={() => setShowManual(true)}
              />
            ) : (
              <>
                <VesselDimensionInputs
                  length={manualLength} beam={manualBeam} draft={manualDraft}
                  onLengthChange={setManualLength} onBeamChange={setManualBeam} onDraftChange={setManualDraft}
                />
                <div className="flex items-end">
                  {hasVessels ? (
                    <button type="button" onClick={() => { setShowManual(false); setVesselDims(null); }}
                      className="text-xs text-muted-foreground hover:text-foreground underline mb-2">
                      pick a boat
                    </button>
                  ) : authed ? (
                    <a href="/boats" className="text-xs text-muted-foreground hover:text-foreground underline mb-2">
                      Add a vessel →
                    </a>
                  ) : (
                    <a href="/login" className="text-xs text-muted-foreground hover:text-foreground underline mb-2">
                      Create an account to save your vessel
                    </a>
                  )}
                </div>
              </>
            )}

            <button type="submit" disabled={loading}
              className="rounded-lg bg-primary text-primary-foreground px-4 py-2 text-sm font-medium hover:opacity-90 disabled:opacity-50 transition-opacity">
              {loading ? 'Searching…' : 'Search'}
            </button>
          </form>
          {error && <p className="text-sm text-destructive mt-2">{error}</p>}
        </div>

        {/* Filter chips row */}
        <div className="flex gap-2 px-4 pb-3 flex-wrap">
          <FilterChip label="Instant Book" active={chipInstant}  onClick={() => setChipInstant(!chipInstant)} />
          <FilterChip label="Electric"     active={chipElectric} onClick={() => setChipElectric(!chipElectric)} />
          <FilterChip label="Pump-out"     active={chipPumpOut}  onClick={() => setChipPumpOut(!chipPumpOut)} />
          <FilterChip label="Covered"      active={chipCovered}  onClick={() => setChipCovered(!chipCovered)} />
        </div>
      </div>

      {/* Split panel: list (left) + map (right) */}
      <div className="flex flex-1 overflow-hidden flex-col md:flex-row">

        {/* Marina list panel */}
        <div className="w-full md:w-96 shrink-0 flex flex-col border-r border-border overflow-hidden md:h-full h-80">
          <div className="flex items-center justify-between px-4 py-2 border-b border-border bg-muted/30 shrink-0">
            <span className="text-xs font-medium text-muted-foreground">
              {summaryText ?? 'Enter a location or allow GPS access to find marinas'}
            </span>
          </div>
          <div className="overflow-y-auto flex-1 p-3 space-y-2">
            {searched && results.length === 0 && !loading && (
              <p className="text-sm text-muted-foreground pt-2">
                No marinas with available {listingKind === 'Lease' ? 'lease listings' : 'slips'} found.
                Try panning the map and clicking "Search this area."
              </p>
            )}
            {results.map((r) => (
              <button
                key={r.marinaId}
                type="button"
                onClick={() => navigateToMarina(r.marinaId)}
                onMouseEnter={() => setHoveredId(r.marinaId)}
                onMouseLeave={() => setHoveredId(null)}
                className={`w-full text-left bg-card rounded-xl border overflow-hidden hover:shadow-md transition-all ${
                  hoveredId === r.marinaId ? 'border-primary/50 shadow-sm' : 'border-border'}`}>
                {/* Banner hero (when available) */}
                {r.bannerThumbnailUrl && (
                  <img src={r.bannerThumbnailUrl} alt="" className="w-full h-24 object-cover" />
                )}
                <div className="flex gap-3 p-3">
                  {/* Logo avatar or anchor placeholder */}
                  {r.logoUrl
                    ? <img src={r.logoUrl} alt={r.marinaName} className="w-12 h-12 rounded-full object-cover shrink-0 border border-border -mt-6 ring-2 ring-card" />
                    : r.bannerThumbnailUrl
                      ? <div className="w-12 h-12 rounded-full shrink-0 -mt-6 ring-2 ring-card flex items-center justify-center bg-primary/10 text-primary text-lg">⚓</div>
                      : <MarinaPhotoPlaceholder />}

                  <div className="flex-1 min-w-0">
                    <div className="flex justify-between items-start gap-2">
                      <p className="text-sm font-semibold text-foreground truncate">{r.marinaName}</p>
                      <p className="text-xs text-muted-foreground shrink-0">{r.distanceMilesFromCenter} mi</p>
                    </div>
                    <p className="text-xs text-muted-foreground mt-0.5">
                      {r.city}{r.state ? `, ${r.state}` : ''} · {r.availableCount} slip{r.availableCount !== 1 ? 's' : ''} fit
                    </p>
                    {/* Amenity badges */}
                    <div className="flex flex-wrap gap-1 mt-2">
                      {r.instantBookAvailable && <AmenityBadge label="Instant" color="bg-primary/10 text-primary" />}
                      {r.hasElectric           && <AmenityBadge label="Electric" color="bg-amber-50 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400" />}
                      {r.hasPumpOut            && <AmenityBadge label="Pump-out" color="bg-sky-50 text-sky-700 dark:bg-sky-900/30 dark:text-sky-400" />}
                      {r.isAnyCovered          && <AmenityBadge label="Covered"  color="bg-muted text-muted-foreground" />}
                    </div>
                    <p className="text-xs text-primary mt-2 font-medium">View slips →</p>
                  </div>
                </div>
              </button>
            ))}
          </div>
        </div>

        {/* Map panel */}
        <div className="relative flex-1">
          <MapContainer center={[DEFAULT_LAT, DEFAULT_LON]} zoom={10} style={{ height: '100%', width: '100%' }}>
            <MapController
              center={mapCenter}
              onBoundsChange={(b) => { boundsRef.current = b; }}
              onUserMoved={() => setShowSearchHere(true)}
            />
            <TileLayer
              attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
              url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            />
            {results.map((r) => (
              <Marker key={r.marinaId} position={[r.latitude, r.longitude]}>
                <Popup>
                  <div className="text-sm">
                    <p className="font-semibold">{r.marinaName}</p>
                    <p className="text-muted-foreground text-xs">{r.city}{r.state ? `, ${r.state}` : ''}</p>
                    <p className="mt-1">{r.availableCount} available</p>
                    <button type="button" onClick={() => navigateToMarina(r.marinaId)}
                      className="text-primary underline mt-1 block text-left text-xs">
                      View slips
                    </button>
                  </div>
                </Popup>
              </Marker>
            ))}
          </MapContainer>

          {showSearchHere && (
            <div className="absolute top-3 left-1/2 -translate-x-1/2 z-[1000]">
              <button type="button"
                onClick={() => { if (boundsRef.current) runSearchWithBounds(boundsRef.current); }}
                className="rounded-full bg-card border border-border shadow-md px-4 py-2 text-sm font-medium text-foreground hover:bg-muted transition-colors">
                Search this area
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
