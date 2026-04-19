export function decodeJWT(token: string): Record<string, any> | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    const decoded = atob(parts[1]);
    return JSON.parse(decoded);
  } catch {
    return null;
  }
}

export function hasMultipleContexts(token: string | null): boolean {
  if (!token) return false;
  const claims = decodeJWT(token);
  if (!claims) return false;
  return claims.has_multiple_contexts === 'true' || claims.has_multiple_contexts === true;
}

export function isDemoSession(token: string | null): boolean {
  if (!token) return false;
  const claims = decodeJWT(token);
  if (!claims) return false;
  return claims.is_demo === 'true' || claims.is_demo === true;
}

export function getDemoTier(token: string | null): string | null {
  if (!token) return null;
  const claims = decodeJWT(token);
  return claims?.subscription_tier ?? null;
}

/** Returns the token expiry as a Date, or null if missing/invalid. */
export function getTokenExpiry(token: string | null): Date | null {
  if (!token) return null;
  const claims = decodeJWT(token);
  if (!claims?.exp) return null;
  return new Date(claims.exp * 1000);
}

/** Extracts AuthUser fields from JWT claims. Returns null if token is invalid. */
export function userFromToken(token: string): import('@/store/authStore').AuthUser | null {
  const claims = decodeJWT(token);
  if (!claims) return null;
  return {
    userId: claims.sub ?? '',
    email: claims.email ?? '',
    firstName: claims.first_name ?? '',
    lastName: claims.last_name ?? '',
    role: claims.role ?? '',
    tenantId: claims.tenant_id ?? null,
    marinaId: claims.marina_id ?? null,
  };
}
