import { apiClient } from './client';

// ─── Auth ─────────────────────────────────────────────────────────────────────

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserProfileDto;
}

export interface UserProfileDto {
  id: string;
  email: string;
  emailConfirmed: boolean;
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
  profilePhotoUrl?: string | null;
  marketingOptIn: boolean;
  createdAt: string;
  lastLoginAt?: string | null;
}

export interface MeResponse extends UserProfileDto {
  memberships: MembershipClaim[];
  billingAccounts: BillingAccountMemberClaim[];
}

export interface MembershipClaim {
  scope: 'Marina' | 'Tenant';
  tenantId: string;
  marinaId?: string | null;
  role: 'Owner' | 'Manager' | 'Staff';
  tier?: string | null;
}

export interface BillingAccountMemberClaim {
  billingAccountId: string;
  marinaId: string;
  role: 'Owner' | 'CoOwner' | 'Member';
}

export const login = (email: string, password: string) =>
  apiClient.post<AuthResponse>('/auth/login', { email, password }).then((r) => r.data);

export const register = (data: {
  email: string; password: string;
  firstName: string; lastName: string;
  marketingOptIn: boolean; termsAccepted: boolean;
}) => apiClient.post('/auth/register', data);

export const refresh = (refreshToken: string) =>
  apiClient.post<AuthResponse>('/auth/refresh', { refreshToken }).then((r) => r.data);

export const logout = (refreshToken: string) =>
  apiClient.post('/auth/logout', { refreshToken });

export const forgotPassword = (email: string) =>
  apiClient.post('/auth/forgot-password', { email });

export const resetPassword = (email: string, token: string, newPassword: string) =>
  apiClient.post('/auth/reset-password', { email, token, newPassword });

export const confirmEmail = (userId: string, token: string) =>
  apiClient.post('/auth/confirm-email', { userId, token });

export const resendConfirmation = (email: string) =>
  apiClient.post('/auth/resend-confirmation', { email });

// ─── Me ───────────────────────────────────────────────────────────────────────

export const getMe = () =>
  apiClient.get<MeResponse>('/me').then((r) => r.data);

export const updateProfile = (data: {
  firstName?: string | null;
  lastName?: string | null;
  phoneNumber?: string | null;
  marketingOptIn?: boolean | null;
}) => apiClient.patch('/me', data);

// ─── Vessels ──────────────────────────────────────────────────────────────────

export type BoatType = 'Sailboat' | 'Powerboat' | 'Catamaran' | 'Dinghy' | 'Pwc' | 'Other';

export interface VesselDto {
  id: string;
  name: string;
  make?: string | null;
  model?: string | null;
  year?: number | null;
  length: number;
  beam: number;
  draft: number;
  boatType: BoatType;
  hullColor?: string | null;
  registrationNumber?: string | null;
  registrationState?: string | null;
  isArchived: boolean;
  createdAt: string;
}

export interface CreateVesselData {
  name: string;
  make?: string | null;
  model?: string | null;
  year?: number | null;
  length: number;
  beam: number;
  draft: number;
  boatType: BoatType;
  hullColor?: string | null;
  registrationNumber?: string | null;
  registrationState?: string | null;
}

export const getVessels = () =>
  apiClient.get<VesselDto[]>('/vessels').then((r) => r.data);

export const getVessel = (id: string) =>
  apiClient.get<VesselDto>(`/vessels/${id}`).then((r) => r.data);

export const createVessel = (data: CreateVesselData) =>
  apiClient.post<VesselDto>('/vessels', data).then((r) => r.data);

export const updateVessel = (id: string, data: Partial<CreateVesselData>) =>
  apiClient.patch(`/vessels/${id}`, data);

export const archiveVessel = (id: string) =>
  apiClient.delete(`/vessels/${id}`);
