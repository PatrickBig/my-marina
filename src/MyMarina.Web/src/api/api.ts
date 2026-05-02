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

// ─── Marinas ──────────────────────────────────────────────────────────────────

export type MarinaType = 'Commercial' | 'YachtClub' | 'PrivateCommunity' | 'Dockominium' | 'PrivateDock';
export type SlipType = 'Floating' | 'Fixed' | 'Mooring' | 'DryStorage' | 'Anchorage';
export type SlipStatus = 'Active' | 'UnderMaintenance' | 'Inactive';
export type MembershipRole = 'Staff' | 'Manager' | 'Owner';

export interface TenantDto {
  id: string;
  name: string;
  slug: string;
  subscriptionTier: string;
  isActive: boolean;
  createdAt: string;
}

export interface MarinaDto {
  id: string;
  tenantId: string;
  name: string;
  slug: string;
  addressStreet?: string | null;
  addressCity?: string | null;
  addressState?: string | null;
  addressZip?: string | null;
  addressCountry?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  phoneNumber?: string | null;
  email?: string | null;
  website?: string | null;
  description?: string | null;
  timeZoneId: string;
  marinaType: MarinaType;
  isListed: boolean;
  createdAt: string;
}

export interface DockDto {
  id: string;
  marinaId: string;
  name: string;
  description?: string | null;
  sortOrder: number;
  createdAt: string;
}

export interface SlipDto {
  id: string;
  marinaId: string;
  dockId?: string | null;
  name: string;
  slipType: SlipType;
  maxLength: number;
  maxBeam: number;
  maxDraft: number;
  hasElectric: boolean;
  electric?: number | null;
  hasWater: boolean;
  status: SlipStatus;
  notes?: string | null;
  createdAt: string;
}

export interface MarinaSignupResponse {
  tenant: TenantDto;
  marina: MarinaDto;
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface MembershipDto {
  id: string;
  userId: string;
  userEmail?: string | null;
  userFirstName?: string | null;
  userLastName?: string | null;
  tenantId: string;
  marinaId?: string | null;
  scope: string;
  role: MembershipRole;
  invitedAt: string;
  acceptedAt?: string | null;
  isPending: boolean;
}

export const signupMarina = (data: { tenantName: string; marinaName: string; marinaType: MarinaType }) =>
  apiClient.post<MarinaSignupResponse>('/marinas/signup', data).then((r) => r.data);

export const getMarina = (marinaId: string) =>
  apiClient.get<MarinaDto>(`/marinas/${marinaId}`).then((r) => r.data);

export const updateMarina = (marinaId: string, data: Partial<Omit<MarinaDto, 'id' | 'tenantId' | 'slug' | 'createdAt' | 'isListed' | 'marinaType'>>) =>
  apiClient.patch<MarinaDto>(`/marinas/${marinaId}`, data).then((r) => r.data);

export const getDocks = (marinaId: string) =>
  apiClient.get<DockDto[]>(`/marinas/${marinaId}/docks`).then((r) => r.data);

export const createDock = (marinaId: string, data: { name: string; description?: string | null; sortOrder: number }) =>
  apiClient.post<DockDto>(`/marinas/${marinaId}/docks`, data).then((r) => r.data);

export const updateDock = (marinaId: string, dockId: string, data: { name?: string; description?: string | null; sortOrder?: number }) =>
  apiClient.patch<DockDto>(`/marinas/${marinaId}/docks/${dockId}`, data).then((r) => r.data);

export const deleteDock = (marinaId: string, dockId: string) =>
  apiClient.delete(`/marinas/${marinaId}/docks/${dockId}`);

export const getSlips = (marinaId: string, dockId?: string) =>
  apiClient.get<SlipDto[]>(`/marinas/${marinaId}/slips`, { params: dockId ? { dockId } : undefined }).then((r) => r.data);

export const createSlip = (marinaId: string, data: {
  dockId?: string | null; name: string; slipType?: SlipType;
  maxLength: number; maxBeam: number; maxDraft: number;
  hasElectric: boolean; electric?: number | null; hasWater: boolean; notes?: string | null;
}) => apiClient.post<SlipDto>(`/marinas/${marinaId}/slips`, data).then((r) => r.data);

export const deleteSlip = (marinaId: string, slipId: string) =>
  apiClient.delete(`/marinas/${marinaId}/slips/${slipId}`);

export const getMarinaStaff = (marinaId: string) =>
  apiClient.get<MembershipDto[]>(`/marinas/${marinaId}/staff`).then((r) => r.data);

export const inviteStaff = (marinaId: string, data: { email: string; role: MembershipRole }) =>
  apiClient.post<MembershipDto>(`/marinas/${marinaId}/staff/invite`, data).then((r) => r.data);

export const revokeStaff = (marinaId: string, membershipId: string) =>
  apiClient.delete(`/marinas/${marinaId}/staff/${membershipId}`);

export const getMyMemberships = () =>
  apiClient.get<MembershipDto[]>('/me/memberships').then((r) => r.data);

export const acceptMembership = (membershipId: string) =>
  apiClient.post(`/memberships/${membershipId}/accept`);
