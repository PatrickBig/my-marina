import { useEffect, useRef, useState } from 'react';
import { MapContainer, TileLayer, Marker, Popup, useMap } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { NavBar } from '@/components/NavBar';
import { searchSlips, type SlipSearchResultDto, type ListingKind, type LeaseTerm } from '@/api/api';

delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

const DEFAULT_LAT = 38.9784;
const DEFAULT_LON = -76.4922;

function MapRecenter({ lat, lon }: { lat: number; lon: number }) {
  const map = useMap();
  useEffect(() => { map.setView([lat, lon], map.getZoom()); }, [lat, lon, map]);
  return null;
}

const input = 'w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400';
const btn = 'rounded-lg bg-slate-800 text-white px-4 py-2 text-sm font-medium hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors';

function formatRate(r: SlipSearchResultDto, nights: number): { primary: string; secondary: string } {
  const isLease = r.listingKind === 'Lease';
  const perFoot = r.rateKind === 'PerFoot';

  if (isLease) {
    const termLabel = r.leaseTerm === 'Monthly' ? '/mo' : r.leaseTerm === 'Seasonal' ? '/season' : '/yr';
    const rateStr = perFoot ? `$${r.basePricePerNight}/ft${termLabel}` : `$${r.basePricePerNight.toFixed(0)}${termLabel}`;
    return { primary: rateStr, secondary: 'Lease — inquiry required' };
  }

  const perNight = perFoot
    ? `$${r.basePricePerNight}/ft/night`
    : `$${r.basePricePerNight.toFixed(2)}/night`;
  const total = perFoot ? '' : `$${(r.basePricePerNight * nights + (r.cleaningFee ?? 0)).toFixed(2)} total`;
  return { primary: perNight, secondary: total };
}

export function SearchPage() {
  const today = new Date().toISOString().slice(0, 10);
  const tomorrow = new Date(Date.now() + 86400000).toISOString().slice(0, 10);

  const [listingKind, setListingKind] = useState<ListingKind>('Transient');
  const [lat, setLat] = useState(DEFAULT_LAT);
  const [lon, setLon] = useState(DEFAULT_LON);
  const [locationText, setLocationText] = useState('');
  const [locationLabel, setLocationLabel] = useState('');
  const [arrivesAt, setArrivesAt] = useState(today);
  const [departsAt, setDepartsAt] = useState(tomorrow);
  const [leaseTerm, setLeaseTerm] = useState<LeaseTerm | ''>('');
  const [radiusMiles, setRadiusMiles] = useState(25);
  const [vesselLength, setVesselLength] = useState('');
  const [vesselBeam, setVesselBeam] = useState('');
  const [vesselDraft, setVesselDraft] = useState('');
  const [results, setResults] = useState<SlipSearchResultDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searched, setSearched] = useState(false);
  const [hoveredSlipId, setHoveredSlipId] = useState<string | null>(null);
  const locating = useRef(false);

  useEffect(() => {
    if (locating.current) return;
    locating.current = true;
    navigator.geolocation?.getCurrentPosition(
      (pos) => { setLat(pos.coords.latitude); setLon(pos.coords.longitude); setLocationLabel('Current location'); },
      () => {}
    );
  }, []);

  async function geocodeLocation(query: string): Promise<{ lat: number; lon: number; label: string } | null> {
    try {
      const url = `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(query)}&format=json&limit=1&countrycodes=us`;
      const res = await fetch(url, { headers: { 'Accept-Language': 'en' } });
      const data = await res.json();
      if (data.length === 0) return null;
      return { lat: parseFloat(data[0].lat), lon: parseFloat(data[0].lon), label: data[0].display_name };
    } catch { return null; }
  }

  async function handleSearch(e?: React.FormEvent) {
    e?.preventDefault();
    setError(null);
    setLoading(true);
    setSearched(true);

    let searchLat = lat;
    let searchLon = lon;

    if (locationText.trim()) {
      const geo = await geocodeLocation(locationText.trim());
      if (!geo) {
        setError(`Could not find "${locationText}". Try a city, state or zip.`);
        setLoading(false);
        return;
      }
      searchLat = geo.lat; searchLon = geo.lon;
      setLat(geo.lat); setLon(geo.lon); setLocationLabel(geo.label);
    }

    try {
      const data = await searchSlips({
        lat: searchLat, lon: searchLon, radiusMiles,
        listingKind,
        arrivesAt: listingKind === 'Transient' ? arrivesAt : undefined,
        departsAt: listingKind === 'Transient' ? departsAt : undefined,
        leaseTerm: listingKind === 'Lease' && leaseTerm ? leaseTerm : undefined,
        vesselLength: vesselLength ? Number(vesselLength) : undefined,
        vesselBeam:   vesselBeam   ? Number(vesselBeam)   : undefined,
        vesselDraft:  vesselDraft  ? Number(vesselDraft)  : undefined,
      });
      setResults(data);
    } catch {
      setError('Search failed. Make sure the API is running.');
    } finally {
      setLoading(false);
    }
  }

  const nights = Math.max(1, Math.round((new Date(departsAt).getTime() - new Date(arrivesAt).getTime()) / 86400000));

  return (
    <div className="min-h-screen bg-slate-50">
      <NavBar />

      {/* Mode toggle + search form */}
      <div className="bg-white border-b border-slate-200 shadow-sm">
        <div className="max-w-6xl mx-auto px-4 pt-3 pb-4">
          {/* Transient / Lease toggle */}
          <div className="flex gap-1 mb-3 w-fit rounded-lg bg-slate-100 p-1">
            {(['Transient', 'Lease'] as ListingKind[]).map((k) => (
              <button
                key={k}
                type="button"
                onClick={() => { setListingKind(k); setResults([]); setSearched(false); }}
                className={`px-4 py-1.5 rounded-md text-sm font-medium transition-colors ${
                  listingKind === k ? 'bg-white shadow text-slate-800' : 'text-slate-500 hover:text-slate-700'
                }`}
              >
                {k === 'Transient' ? 'Transient dockage' : 'Seasonal / annual lease'}
              </button>
            ))}
          </div>

          <form onSubmit={handleSearch} className="flex flex-wrap gap-3 items-end">
            {/* Location */}
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">Location</label>
              <div className="flex gap-1.5">
                <input
                  type="text" value={locationText} onChange={(e) => setLocationText(e.target.value)}
                  placeholder="City, state or zip" className={`${input} w-48`}
                />
                <button
                  type="button" title="Use GPS"
                  onClick={() => navigator.geolocation?.getCurrentPosition(
                    (p) => { setLat(p.coords.latitude); setLon(p.coords.longitude); setLocationText(''); setLocationLabel('Current location'); },
                    () => alert('Location access denied.')
                  )}
                  className="rounded-lg border border-slate-300 px-2 text-slate-500 hover:text-slate-800 hover:bg-slate-50 text-sm"
                >📍</button>
              </div>
              {locationLabel && !locationText && (
                <p className="text-xs text-slate-400 mt-0.5 truncate max-w-[13rem]">{locationLabel}</p>
              )}
            </div>

            {/* Transient: dates */}
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

            {/* Lease: desired start + term */}
            {listingKind === 'Lease' && (
              <>
                <div>
                  <label className="block text-xs font-medium text-slate-600 mb-1">Desired start</label>
                  <input type="date" value={arrivesAt} onChange={(e) => setArrivesAt(e.target.value)}
                    placeholder="optional" className={`${input} w-36`} />
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

            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">Radius (mi)</label>
              <input type="number" value={radiusMiles} min={1} max={100}
                onChange={(e) => setRadiusMiles(Number(e.target.value))} className={`${input} w-20`} />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">LOA (ft)</label>
              <input type="number" step="0.1" value={vesselLength}
                onChange={(e) => setVesselLength(e.target.value)} placeholder="any" className={`${input} w-20`} />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">Beam (ft)</label>
              <input type="number" step="0.1" value={vesselBeam}
                onChange={(e) => setVesselBeam(e.target.value)} placeholder="any" className={`${input} w-20`} />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">Draft (ft)</label>
              <input type="number" step="0.1" value={vesselDraft}
                onChange={(e) => setVesselDraft(e.target.value)} placeholder="any" className={`${input} w-20`} />
            </div>
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
          <div className="h-[480px] rounded-xl overflow-hidden border border-slate-200 shadow-sm">
            <MapContainer center={[lat, lon]} zoom={10} style={{ height: '100%', width: '100%' }}>
              <MapRecenter lat={lat} lon={lon} />
              <TileLayer
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
              />
              {results.map((r) => (
                <Marker key={r.slipId} position={[r.latitude, r.longitude]}>
                  <Popup>
                    <div className="text-sm">
                      <p className="font-semibold">{r.slipName}</p>
                      <p className="text-slate-500">{r.marinaName}</p>
                      <p className="mt-1">{formatRate(r, nights).primary}</p>
                      <a href={`/slips/${r.slipId}`} className="text-blue-600 underline mt-1 block">View details</a>
                    </div>
                  </Popup>
                </Marker>
              ))}
            </MapContainer>
          </div>

          <div className="space-y-3 overflow-y-auto max-h-[480px] pr-1">
            {!searched && (
              <p className="text-sm text-slate-400 pt-2">
                {listingKind === 'Transient'
                  ? 'Enter dates and click Search to find available slips near you.'
                  : 'Click Search to find slips available for lease near you.'}
              </p>
            )}
            {searched && results.length === 0 && !loading && (
              <p className="text-sm text-slate-400 pt-2">
                No {listingKind === 'Lease' ? 'lease listings' : 'available slips'} found. Try expanding the radius.
              </p>
            )}
            {results.map((r) => {
              const { primary, secondary } = formatRate(r, nights);
              return (
                <a
                  key={r.slipId}
                  href={`/slips/${r.slipId}`}
                  onMouseEnter={() => setHoveredSlipId(r.slipId)}
                  onMouseLeave={() => setHoveredSlipId(null)}
                  className={`block bg-white rounded-xl border p-4 hover:shadow-md transition-shadow ${
                    hoveredSlipId === r.slipId ? 'border-slate-400' : 'border-slate-200'
                  }`}
                >
                  <div className="flex justify-between items-start">
                    <div>
                      <div className="flex items-center gap-2">
                        <p className="text-sm font-semibold text-slate-800">{r.slipName}</p>
                        {r.listingKind === 'Lease' && (
                          <span className="text-xs px-1.5 py-0.5 rounded-full bg-purple-50 text-purple-700">
                            {r.leaseTerm ?? 'Lease'}
                          </span>
                        )}
                      </div>
                      <p className="text-xs text-slate-500 mt-0.5">
                        {r.marinaName}
                        {r.marinaCity && ` · ${r.marinaCity}${r.marinaState ? `, ${r.marinaState}` : ''}`}
                      </p>
                      <p className="text-xs text-slate-400 mt-1">
                        {r.slipType} · {r.maxLength}′ L × {r.maxBeam}′ B × {r.maxDraft}′ D
                        {r.hasWater && ' · Water'}{r.hasElectric && ' · Electric'}
                      </p>
                      <p className="text-xs text-slate-400 mt-0.5">{r.distanceMiles} mi away</p>
                    </div>
                    <div className="text-right ml-4 shrink-0">
                      <p className="text-base font-bold text-slate-800">{primary}</p>
                      {secondary && <p className="text-xs text-slate-500 mt-0.5">{secondary}</p>}
                      {r.listingKind === 'Transient' && r.instantBook && (
                        <span className="inline-block mt-1 text-xs px-1.5 py-0.5 rounded-full bg-blue-50 text-blue-700">Instant</span>
                      )}
                      {r.listingKind === 'Lease' && (
                        <span className="inline-block mt-1 text-xs px-1.5 py-0.5 rounded-full bg-amber-50 text-amber-700">Inquiry</span>
                      )}
                    </div>
                  </div>
                </a>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
}
