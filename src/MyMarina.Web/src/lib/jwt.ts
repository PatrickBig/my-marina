export function decodeJWT(token: string): Record<string, any> | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    return JSON.parse(atob(parts[1]));
  } catch {
    return null;
  }
}

export function isDemoSession(token: string | null): boolean {
  if (!token) return false;
  const claims = decodeJWT(token);
  return claims?.is_demo === 'true' || claims?.is_demo === true;
}

export function getTokenExpiry(token: string | null): Date | null {
  if (!token) return null;
  const claims = decodeJWT(token);
  if (!claims?.exp) return null;
  return new Date(claims.exp * 1000);
}

export type SubscriptionTier = 'Free' | 'Pro' | 'Premium';
const tierOrder: SubscriptionTier[] = ['Free', 'Pro', 'Premium'];

export function getSubscriptionTier(token: string | null): SubscriptionTier {
  if (!token) return 'Free';
  const claims = decodeJWT(token);
  const raw = claims?.subscription_tier as string | undefined;
  return (tierOrder.includes(raw as SubscriptionTier) ? raw : 'Free') as SubscriptionTier;
}

export function hasTier(token: string | null, required: SubscriptionTier): boolean {
  const current = getSubscriptionTier(token);
  return tierOrder.indexOf(current) >= tierOrder.indexOf(required);
}
