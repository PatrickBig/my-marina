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

const input = 'w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400';
const btn = 'rounded-lg bg-slate-800 text-white px-4 py-2 text-sm font-medium hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors';

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

  // Price range filter — only active when listingKind is selected
  const [priceMin, setPriceMin] = useState('');
  const [priceMax, setPriceMax] = useState('');

  // Vessel state — filled from VesselSelector or manual inputs
  const [vesselDims, setVesselDims]   = useState<VesselDimensions | null>(null);
  const [manualLength, setManualLength] = useState('');
  const [manualBeam, setManualBeam]   = useState('');
  const [manualDraft, setManualDraft] = useState('');
  // Anonymous users start in manual mode immediately (no flash); auth users wait for vessel load
  const [showManual, setShowManual]   = useState(!authed);
  // True once VesselSelector confirms vessels exist — controls "pick a boat" toggle in manual mode
  const [hasVessels, setHasVessels]   = useState(false);

  const [results, setResults]         = useState<MarinaRollupResultDto[]>([]);
  const [loading, setLoading]         = useState(false);
  const [error, setError]             = useState<string | null>(null);
  const [searched, setSearched]       = useState(false);
  const [hoveredId, setHoveredId]     = useState<string | null>(null);
  const [showSearchHere, setShowSearchHere] = useState(false);

  // Map state
  const [mapCenter, setMapCenter]     = useState<{ lat: number; lon: number } | null>(null);
  const boundsRef                     = useRef<Bounds | null>(null);
  const locating                      = useRef(false);
  const abortRef                      = useRef<AbortController | null>(null);

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
        arrivesAt:  !isLease ? arrivesAt : undefined,
        departsAt:  !isLease ? departsAt : undefined,
        leaseTerm:  isLease && leaseTerm ? leaseTerm : undefined,
        priceMin:   priceMin ? Number(priceMin) : undefined,
        priceMax:   priceMax ? Number(priceMax) : undefined,
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
    // Approximate a 25-mile bounding box for the initial zoom-10 view
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
    // Clear price filter when listing kind changes so stale values don't linger
    setPriceMin('');
    setPriceMax('');
    setResults([]);
    setSearched(false);
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <NavBar />

      <div className="bg-white border-b border-slate-200 shadow-sm">
        <div className="max-w-6xl mx-auto px-4 pt-3 pb-4">
          {/* Transient / Lease toggle */}
          <div className="flex gap-1 mb-3 w-fit rounded-lg bg-slate-100 p-1">
            {(['Transient', 'Lease'] as ListingKind[]).map((k) => (
              <button key={k} type="button"
                onClick={() => handleListingKindChange(k)}
                className={`px-4 py-1.5 rounded-md text-sm font-medium transition-colors ${
                  listingKind === k ? 'bg-white shadow text-slate-800' : 'text-slate-500 hover:text-slate-700'}`}>
                {k === 'Transient' ? 'Transient dockage' : 'Seasonal / annual lease'}
              </button>
            ))}
          </div>

          <form onSubmit={handleSearch} className="flex flex-wrap gap-3 items-end">
            {/* Location */}
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">Location</label>
              <div className="flex gap-1.5">
                <input type="text" value={locationText} onChange={(e) => setLocationText(e.target.value)}
                  placeholder="City, state or zip" className={`${input} w-48`} />
                <button type="button" title="Use GPS"
                  onClick={() => navigator.geolocation?.getCurrentPosition(
                    (p) => { setLocationText(''); runSearchAtCenter(p.coords.latitude, p.coords.longitude); },
                    () => alert('Location access denied.'))}
                  className="rounded-lg border border-slate-300 px-2 text-slate-500 hover:text-slate-800 hover:bg-slate-50 text-sm">
                  📍
                </button>
              </div>
            </div>

            {/* Dates */}
            {listingKind === 'Transient' && (
              <>
                <div>
                  <label className="block text-xs font-medium text-slate-600 mb-1">Arrival</label>
                  <input type="date" value={arrivesAt} onChange={(e) => setArrivesAt(e.target.value)} className={`${input} w-36`} />
                </div>
                <div>
                  <label className="block text-xs font-medium text-slate-600 mb-1">Departure</label>
                  <input type="date" value={departsAt} onChange={(e) => setDepartsAt(e.target.value)} className={`${input} w-36`} />
                </div>
              </>
            )}
            {listingKind === 'Lease' && (
              <>
                <div>
                  <label className="block text-xs font-medium text-slate-600 mb-1">Desired start</label>
                  <input type="date" value={arrivesAt} onChange={(e) => setArrivesAt(e.target.value)} className={`${input} w-36`} />
                </div>
                <div>
                  <label className="block text-xs font-medium text-slate-600 mb-1">Lease term</label>
                  <select value={leaseTerm} onChange={(e) => setLeaseTerm(e.target.value as LeaseTerm | '')}
                    className={`${input} w-32`}>
                    <option value="">Any</option>
                    <option value="Monthly">Monthly</option>
                    <option value="Seasonal">Seasonal</option>
                    <option value="Annual">Annual</option>
                  </select>
                </div>
              </>
            )}

            {/* Price range — visible only when a listing kind is active */}
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">Price min</label>
              <input
                type="number" min="0" step="1"
                value={priceMin}
                onChange={(e) => setPriceMin(e.target.value)}
                placeholder="$"
                className={`${input} w-24`}
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">Price max</label>
              <input
                type="number" min="0" step="1"
                value={priceMax}
                onChange={(e) => setPriceMax(e.target.value)}
                placeholder="$"
                className={`${input} w-24`}
              />
            </div>

            {/* Vessel picker or manual dimensions */}
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
                      className="text-xs text-slate-400 hover:text-slate-600 underline mb-2">
                      pick a boat
                    </button>
                  ) : authed ? (
                    <a href="/boats"
                      className="text-xs text-slate-500 hover:text-slate-700 underline mb-2">
                      Add a vessel →
                    </a>
                  ) : (
                    <a href="/login"
                      className="text-xs text-slate-500 hover:text-slate-700 underline mb-2">
                      Create an account to save your vessel
                    </a>
                  )}
                </div>
              </>
            )}

            <button type="submit" disabled={loading} className={btn}>
              {loading ? 'Searching…' : 'Search'}
            </button>
          </form>
          {error && <p className="text-sm text-red-600 mt-2">{error}</p>}
        </div>
      </div>

      {/* Map + results */}
      <div className="max-w-6xl mx-auto px-4 py-6">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Map */}
          <div className="relative h-[480px] rounded-xl overflow-hidden border border-slate-200 shadow-sm">
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
                      <p className="text-slate-500">{r.city}{r.state ? `, ${r.state}` : ''}</p>
                      <p className="mt-1">{r.availableCount} available</p>
                      <button
                        type="button"
                        onClick={() => navigateToMarina(r.marinaId)}
                        className="text-blue-600 underline mt-1 block text-left">
                        View slips
                      </button>
                    </div>
                  </Popup>
                </Marker>
              ))}
            </MapContainer>

            {showSearchHere && (
              <div className="absolute top-3 left-1/2 -translate-x-1/2 z-[1000]">
                <button
                  type="button"
                  onClick={() => { if (boundsRef.current) runSearchWithBounds(boundsRef.current); }}
                  className="rounded-full bg-white border border-slate-300 shadow-md px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 transition-colors">
                  Search this area
                </button>
              </div>
            )}
          </div>

          {/* Marina list */}
          <div className="space-y-3 overflow-y-auto max-h-[480px] pr-1">
            {!searched && (
              <p className="text-sm text-slate-400 pt-2">
                Enter a location or allow GPS access, then click Search to find marinas near you.
              </p>
            )}
            {searched && results.length === 0 && !loading && (
              <p className="text-sm text-slate-400 pt-2">
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
                className={`w-full text-left bg-white rounded-xl border p-4 hover:shadow-md transition-shadow ${
                  hoveredId === r.marinaId ? 'border-slate-400' : 'border-slate-200'}`}>
                <div className="flex justify-between items-start">
                  <div>
                    <p className="text-sm font-semibold text-slate-800">{r.marinaName}</p>
                    <p className="text-xs text-slate-500 mt-0.5">
                      {r.city}{r.state ? `, ${r.state}` : ''}
                      {' · '}{r.distanceMilesFromCenter} mi from center
                    </p>
                  </div>
                  <div className="text-right ml-4 shrink-0">
                    <p className="text-xs text-slate-500 mt-0.5">{r.availableCount} available</p>
                    {r.instantBookAvailable && (
                      <span className="inline-block mt-1 text-xs px-1.5 py-0.5 rounded-full bg-blue-50 text-blue-700">
                        Instant
                      </span>
                    )}
                  </div>
                </div>
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
