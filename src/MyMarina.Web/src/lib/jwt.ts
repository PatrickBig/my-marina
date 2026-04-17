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
