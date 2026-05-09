export interface GeocodeResult {
  lat: number;
  lng: number;
  precisionLabel: string;
}

interface NominatimHit {
  lat: string;
  lon: string;
  display_name: string;
}

const NOMINATIM_BASE = 'https://nominatim.openstreetmap.org/search';
const HEADERS = {
  'Accept-Language': 'en',
  'User-Agent': 'MyMarina/1.0 (mymarina.org)',
};

async function query(q: string): Promise<NominatimHit | null> {
  try {
    const url = `${NOMINATIM_BASE}?q=${encodeURIComponent(q)}&format=json&limit=1&countrycodes=us`;
    const res = await fetch(url, { headers: HEADERS });
    const data: NominatimHit[] = await res.json();
    return data[0] ?? null;
  } catch {
    return null;
  }
}

// Progressive fallback: full address → city+state+zip → city+state → state only → null
export async function nominatimGeocode(address: {
  street?: string;
  city?: string;
  state?: string;
  zip?: string;
}): Promise<GeocodeResult | null> {
  const { street, city, state, zip } = address;

  const attempts: Array<{ q: string; label: string }> = [];

  if (street && city && state && zip)
    attempts.push({ q: `${street}, ${city}, ${state} ${zip}`, label: 'Street address' });
  if (city && state && zip)
    attempts.push({ q: `${city}, ${state} ${zip}`, label: `${city}, ${state}` });
  if (city && state)
    attempts.push({ q: `${city}, ${state}`, label: `${city}, ${state}` });
  if (state)
    attempts.push({ q: state, label: state });

  for (const attempt of attempts) {
    const hit = await query(attempt.q);
    if (hit) {
      return {
        lat: parseFloat(hit.lat),
        lng: parseFloat(hit.lon),
        precisionLabel: attempt.label,
      };
    }
  }

  return null;
}
