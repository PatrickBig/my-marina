import { apiClient } from './client';

// ─── Auth ─────────────────────────────────────────────────────────────────────

export interface AuthUserDto {
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

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: AuthUserDto;
}

export interface MeResponse extends AuthUserDto {
  memberships: MembershipClaim[];
  billingAccounts: BillingAccountMemberClaim[];
  isPlatformOperator: boolean;
}

export interface MembershipClaim {
  scope: 'Marina' | 'Tenant';
  tenantId: string;
  marinaId?: string | null;
  marinaName?: string | null;
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

// ─── Client Config ────────────────────────────────────────────────────────────

export interface ClientConfigDto {
  socialProviders: string[];
}

export const getClientConfig = () =>
  apiClient.get<ClientConfigDto>('/client-config').then((r) => r.data);

// ─── Linked Social Providers ──────────────────────────────────────────────────

export interface LinkedProviderDto {
  provider: string;
}

export const getLinkedProviders = () =>
  apiClient.get<LinkedProviderDto[]>('/auth/external/providers').then((r) => r.data);

export const unlinkProvider = (provider: string) =>
  apiClient.post(`/auth/external/${provider}/unlink`);

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

export const changePassword = (currentPassword: string, newPassword: string) =>
  apiClient.post('/me/change-password', { currentPassword, newPassword });

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
export type RateKind = 'Flat' | 'PerFoot';
export type LeaseTerm = 'Monthly' | 'Seasonal' | 'Annual';
export type ListingKind = 'Transient' | 'Lease';
export type LeaseInquiryStatus = 'Pending' | 'UnderReview' | 'Approved' | 'Declined' | 'Withdrawn';

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
  isSetupComplete: boolean;
  setupStep: number;
  createdAt: string;
  logoUrl?: string | null;
  bannerThumbnailUrl?: string | null;
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
  hasPumpOut: boolean;
  isCovered: boolean;
  isIndoor: boolean;
  amenities: string[];
  status: SlipStatus;
  defaultTransientRateKind?: RateKind | null;
  defaultTransientBaseRate?: number | null;
  defaultTransientMinCharge?: number | null;
  defaultLeaseRateKind?: RateKind | null;
  defaultLeaseBaseRate?: number | null;
  defaultLeaseTerm?: LeaseTerm | null;
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

export interface MyMarinaDto {
  id: string;
  tenantId: string;
  name: string;
  addressCity?: string | null;
  addressState?: string | null;
  marinaType: MarinaType;
  isListed: boolean;
  isSetupComplete: boolean;
  setupStep: number;
  latitude?: number | null;
  longitude?: number | null;
  userRole?: string | null;
  relationshipKind: 'Staff' | 'BillingAccount';
}

export interface MarinaSummaryDto {
  id: string;
  name: string;
  addressCity?: string | null;
  addressState?: string | null;
  marinaType: string;
}

export const getMyMarinas = () =>
  apiClient.get<MyMarinaDto[]>('/marinas').then((r) => r.data);

export const searchMarinas = (q: string, limit = 10) =>
  apiClient.get<MarinaSummaryDto[]>('/marinas/lookup', { params: { q, limit } }).then((r) => r.data);

export interface MarinaSignupData {
  tenantName: string;
  marinaName: string;
  marinaType: MarinaType;
  // Required for PrivateDock / Dockominium
  slipName?: string;
  maxLength?: number;
  maxBeam?: number;
  maxDraft?: number;
  slipType?: string;
  slipNotes?: string;
  // Dockominium-only
  hostMarinaId?: string;
  hostMarinaPolicy?: 'None' | 'NotifyOnly' | 'RequiresApproval';
}

export const signupMarina = (data: MarinaSignupData) =>
  apiClient.post<MarinaSignupResponse>('/marinas/signup', data).then((r) => r.data);

export const getMarina = (marinaId: string) =>
  apiClient.get<MarinaDto>(`/marinas/${marinaId}`).then((r) => r.data);

export const updateMarina = (marinaId: string, data: Partial<Omit<MarinaDto, 'id' | 'tenantId' | 'slug' | 'createdAt' | 'marinaType'> & { setupStep?: number; isSetupComplete?: boolean; isListed?: boolean }>) =>
  apiClient.patch<MarinaDto>(`/marinas/${marinaId}`, data).then((r) => r.data);

export const deleteDraftMarina = (marinaId: string) =>
  apiClient.delete(`/marinas/${marinaId}`);

export interface SetupDockSlipData {
  name: string;
  maxLength: number;
  maxBeam: number;
  maxDraft: number;
  slipType?: string;
  hasElectric?: boolean;
  electric?: number | null;
  hasWater?: boolean;
  hasPumpOut?: boolean;
  isCovered?: boolean;
  isIndoor?: boolean;
  amenities?: string[];
}

export interface SetupDockData {
  name: string;
  slips: SetupDockSlipData[];
}

export const setupDocks = (marinaId: string, docks: SetupDockData[]) =>
  apiClient.put(`/marinas/${marinaId}/setup/docks`, { docks });

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
  hasElectric: boolean; electric?: number | null; hasWater: boolean;
  hasPumpOut?: boolean; isCovered?: boolean; isIndoor?: boolean; amenities?: string[];
  notes?: string | null;
}) => apiClient.post<SlipDto>(`/marinas/${marinaId}/slips`, data).then((r) => r.data);

export const updateSlip = (marinaId: string, slipId: string, data: {
  dockId?: string | null; name?: string; slipType?: SlipType;
  maxLength?: number; maxBeam?: number; maxDraft?: number;
  hasElectric?: boolean; electric?: number | null; hasWater?: boolean;
  hasPumpOut?: boolean; isCovered?: boolean; isIndoor?: boolean; amenities?: string[];
  status?: SlipStatus;
  defaultTransientRateKind?: RateKind | null;
  defaultTransientBaseRate?: number | null;
  defaultTransientMinCharge?: number | null;
  clearTransientRate?: boolean;
  defaultLeaseRateKind?: RateKind | null;
  defaultLeaseBaseRate?: number | null;
  defaultLeaseTerm?: LeaseTerm | null;
  clearLeaseRate?: boolean;
  notes?: string | null;
}) => apiClient.patch<SlipDto>(`/marinas/${marinaId}/slips/${slipId}`, data).then((r) => r.data);

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

// ─── Billing Accounts ─────────────────────────────────────────────────────────

export interface BillingAccountDto {
  id: string;
  marinaId: string;
  displayName: string;
  billingEmail: string;
  billingPhone?: string | null;
  billingAddressStreet?: string | null;
  billingAddressCity?: string | null;
  billingAddressState?: string | null;
  billingAddressZip?: string | null;
  billingAddressCountry?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface BillingAccountMemberDto {
  id: string;
  billingAccountId: string;
  userId: string;
  userEmail: string;
  userName?: string | null;
  role: string;
  invitedAt: string;
  acceptedAt?: string | null;
}

export const getBillingAccounts = (marinaId: string) =>
  apiClient.get<BillingAccountDto[]>(`/marinas/${marinaId}/billing-accounts`).then((r) => r.data);

export const createBillingAccount = (marinaId: string, data: {
  displayName: string; billingEmail: string; billingPhone?: string | null;
  billingAddressStreet?: string | null; billingAddressCity?: string | null;
  billingAddressState?: string | null; billingAddressZip?: string | null;
  emergencyContactName?: string | null; emergencyContactPhone?: string | null;
  notes?: string | null;
}) => apiClient.post<BillingAccountDto>(`/marinas/${marinaId}/billing-accounts`, data).then((r) => r.data);

export const updateBillingAccount = (marinaId: string, accountId: string, data: Partial<{
  displayName: string; billingEmail: string; billingPhone: string | null;
  billingAddressStreet: string | null; billingAddressCity: string | null;
  billingAddressState: string | null; billingAddressZip: string | null;
  emergencyContactName: string | null; emergencyContactPhone: string | null;
  notes: string | null; isActive: boolean;
}>) => apiClient.patch<BillingAccountDto>(`/marinas/${marinaId}/billing-accounts/${accountId}`, data).then((r) => r.data);

export const getBillingAccountMembers = (marinaId: string, accountId: string) =>
  apiClient.get<BillingAccountMemberDto[]>(`/marinas/${marinaId}/billing-accounts/${accountId}/members`).then((r) => r.data);

export const inviteAccountMember = (marinaId: string, accountId: string, email: string, role = 'Member') =>
  apiClient.post<BillingAccountMemberDto>(`/marinas/${marinaId}/billing-accounts/${accountId}/members/invite`, { email, role }).then((r) => r.data);

export const removeAccountMember = (marinaId: string, accountId: string, memberId: string) =>
  apiClient.delete(`/marinas/${marinaId}/billing-accounts/${accountId}/members/${memberId}`);

// ─── Vessel Records ───────────────────────────────────────────────────────────

export interface VesselRecordDto {
  id: string;
  marinaId: string;
  vesselId: string;
  billingAccountId?: string | null;
  vesselName: string;
  vesselMake?: string | null;
  vesselModel?: string | null;
  vesselYear?: number | null;
  vesselLength: number;
  vesselBoatType: string;
  vesselIsGhost: boolean;
  insuranceProvider?: string | null;
  insurancePolicyNumber?: string | null;
  insuranceExpiresOn?: string | null;
  insuranceVerifiedAt?: string | null;
  notes?: string | null;
  createdAt: string;
}

export const getVesselRecords = (marinaId: string, billingAccountId?: string) =>
  apiClient.get<VesselRecordDto[]>(`/marinas/${marinaId}/vessel-records`, {
    params: billingAccountId ? { billingAccountId } : undefined,
  }).then((r) => r.data);

export const createVesselRecord = (marinaId: string, data: {
  billingAccountId?: string | null;
  vesselId?: string | null;
  claimEmail?: string | null;
  vesselName?: string | null;
  vesselMake?: string | null;
  vesselModel?: string | null;
  vesselYear?: number | null;
  vesselLength?: number | null;
  vesselBeam?: number | null;
  vesselDraft?: number | null;
  vesselBoatType?: string | null;
  insuranceProvider?: string | null;
  insurancePolicyNumber?: string | null;
  insuranceExpiresOn?: string | null;
  notes?: string | null;
}) => apiClient.post<VesselRecordDto>(`/marinas/${marinaId}/vessel-records`, data).then((r) => r.data);

// ─── Slip Assignments ─────────────────────────────────────────────────────────

export type AssignmentType = 'Transient' | 'Monthly' | 'Seasonal' | 'Annual';

export interface SlipAssignmentDto {
  id: string;
  slipId: string;
  slipName: string;
  billingAccountId: string;
  billingAccountDisplayName: string;
  vesselId: string;
  vesselName: string;
  assignmentType: AssignmentType;
  startDate: string;
  endDate?: string | null;
  baseRate: number;
  allowOwnerSubletWhenAway: boolean;
  allowHolderSublet: boolean;
  ownerSubletShareToHolder: number;
  holderSubletShareToOwner: number;
  notes?: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreateSlipAssignmentData {
  slipId: string;
  billingAccountId: string;
  vesselId: string;
  assignmentType: AssignmentType;
  startDate: string;
  endDate?: string | null;
  baseRate: number;
  allowOwnerSubletWhenAway: boolean;
  allowHolderSublet: boolean;
  ownerSubletShareToHolder: number;
  holderSubletShareToOwner: number;
  notes?: string | null;
}

export const getSlipAssignments = (marinaId: string, params?: { slipId?: string; billingAccountId?: string; activeOnly?: boolean }) =>
  apiClient.get<SlipAssignmentDto[]>(`/marinas/${marinaId}/slip-assignments`, { params }).then((r) => r.data);

export const createSlipAssignment = (marinaId: string, data: CreateSlipAssignmentData) =>
  apiClient.post<SlipAssignmentDto>(`/marinas/${marinaId}/slip-assignments`, data).then((r) => r.data);

export const updateSlipAssignment = (marinaId: string, id: string, data: Partial<CreateSlipAssignmentData>) =>
  apiClient.patch<SlipAssignmentDto>(`/marinas/${marinaId}/slip-assignments/${id}`, data).then((r) => r.data);

export const endSlipAssignment = (marinaId: string, id: string, endDate?: string) =>
  apiClient.post<SlipAssignmentDto>(`/marinas/${marinaId}/slip-assignments/${id}/end`, { endDate: endDate ?? null }).then((r) => r.data);

// ─── Availability Windows ─────────────────────────────────────────────────────

export type ListedByKind = 'Owner' | 'Holder' | 'OwnerForHolder';
export type AvailabilityWindowStatus = 'Open' | 'Paused' | 'FullyBooked' | 'Closed';

export interface RevenueSplitEntryDto {
  payeeKind: string;
  payeeId?: string | null;
  percent: number;
}

export interface AvailabilityWindowDto {
  id: string;
  slipId: string;
  slipName: string;
  listedByKind: ListedByKind;
  listedByMarinaId?: string | null;
  listedByBillingAccountId?: string | null;
  relatedAssignmentId?: string | null;
  listingKind: ListingKind;
  leaseTerm?: LeaseTerm | null;
  rateKind: RateKind;
  startsAt: string;
  endsAt: string;
  instantBook: boolean;
  minNights?: number | null;
  maxNights?: number | null;
  basePricePerNight: number;
  minCharge?: number | null;
  weeklyDiscount?: number | null;
  monthlyDiscount?: number | null;
  cleaningFee?: number | null;
  revenueSplit: RevenueSplitEntryDto[];
  status: AvailabilityWindowStatus;
  createdAt: string;
}

export interface CreateAvailabilityWindowData {
  slipId: string;
  listedByKind: ListedByKind;
  listedByMarinaId?: string | null;
  listedByBillingAccountId?: string | null;
  relatedAssignmentId?: string | null;
  listingKind?: ListingKind;
  leaseTerm?: LeaseTerm | null;
  rateKind?: RateKind;
  startsAt: string;
  endsAt: string;
  instantBook: boolean;
  minNights?: number | null;
  maxNights?: number | null;
  basePricePerNight: number;
  minCharge?: number | null;
  weeklyDiscount?: number | null;
  monthlyDiscount?: number | null;
  cleaningFee?: number | null;
}

export const getAvailabilityWindows = (marinaId: string, params?: { slipId?: string; status?: string }) =>
  apiClient.get<AvailabilityWindowDto[]>(`/marinas/${marinaId}/availability-windows`, { params }).then((r) => r.data);

export const getAvailabilityWindow = (marinaId: string, id: string) =>
  apiClient.get<AvailabilityWindowDto>(`/marinas/${marinaId}/availability-windows/${id}`).then((r) => r.data);

export const createAvailabilityWindow = (marinaId: string, data: CreateAvailabilityWindowData) =>
  apiClient.post<AvailabilityWindowDto>(`/marinas/${marinaId}/availability-windows`, data).then((r) => r.data);

export const updateAvailabilityWindow = (marinaId: string, id: string, data: Partial<Omit<CreateAvailabilityWindowData, 'slipId' | 'listedByKind'>>) =>
  apiClient.patch<AvailabilityWindowDto>(`/marinas/${marinaId}/availability-windows/${id}`, data).then((r) => r.data);

export const setAvailabilityWindowStatus = (marinaId: string, id: string, status: AvailabilityWindowStatus) =>
  apiClient.post<AvailabilityWindowDto>(`/marinas/${marinaId}/availability-windows/${id}/status`, { status }).then((r) => r.data);

// ─── Boater Search (public, two-step) ────────────────────────────────────────

export interface MarinaRollupResultDto {
  marinaId: string;
  marinaName: string;
  city?: string | null;
  state?: string | null;
  latitude: number;
  longitude: number;
  availableCount: number;
  instantBookAvailable: boolean;
  distanceMilesFromCenter: number;
  photoUrl?: string | null;
  logoUrl?: string | null;
  bannerThumbnailUrl?: string | null;
  hasPumpOut: boolean;
  hasElectric: boolean;
  isAnyCovered: boolean;
}

export interface SlipSearchResultDto {
  slipId: string;
  slipName: string;
  slipType: string;
  maxLength: number;
  maxBeam: number;
  maxDraft: number;
  hasElectric: boolean;
  hasWater: boolean;
  latitude: number;
  longitude: number;
  marinaId: string;
  marinaName: string;
  marinaCity?: string | null;
  marinaState?: string | null;
  bestWindowId?: string | null;
  listingKind: ListingKind;
  rateKind: RateKind;
  basePricePerNight: number;
  minCharge?: number | null;
  leaseTerm?: LeaseTerm | null;
  instantBook: boolean;
  cleaningFee?: number | null;
  minNights?: number | null;
  maxNights?: number | null;
  distanceMiles: number;
}

export interface PublicWindowSummaryDto {
  id: string;
  listingKind: ListingKind;
  leaseTerm?: LeaseTerm | null;
  rateKind: RateKind;
  startsAt: string;
  endsAt: string;
  instantBook: boolean;
  minNights?: number | null;
  maxNights?: number | null;
  basePricePerNight: number;
  minCharge?: number | null;
  weeklyDiscount?: number | null;
  monthlyDiscount?: number | null;
  cleaningFee?: number | null;
}

export interface SlipDetailDto {
  id: string;
  name: string;
  slipType: string;
  maxLength: number;
  maxBeam: number;
  maxDraft: number;
  hasElectric: boolean;
  electric?: number | null;
  hasWater: boolean;
  latitude?: number | null;
  longitude?: number | null;
  addressCity?: string | null;
  addressState?: string | null;
  marinaId: string;
  marinaName: string;
  marinaDescription?: string | null;
  marinaPhoneNumber?: string | null;
  defaultTransientRateKind?: RateKind | null;
  defaultTransientBaseRate?: number | null;
  defaultTransientMinCharge?: number | null;
  transientBookingAvailable: boolean;
  defaultLeaseRateKind?: RateKind | null;
  defaultLeaseBaseRate?: number | null;
  defaultLeaseTerm?: LeaseTerm | null;
  leaseInquiryAvailable: boolean;
  openWindows: PublicWindowSummaryDto[];
}

interface BaseSearchParams {
  listingKind?: ListingKind;
  arrivesAt?: string;
  departsAt?: string;
  leaseTerm?: LeaseTerm;
  vesselLength?: number;
  vesselBeam?: number;
  vesselDraft?: number;
  slipType?: string;
  hasElectric?: boolean;
  hasWater?: boolean;
  page?: number;
  pageSize?: number;
}

export interface MarinaRollupSearchParams extends BaseSearchParams {
  north: number;
  south: number;
  east: number;
  west: number;
  priceMin?: number;
  priceMax?: number;
  instantBookOnly?: boolean;
  hasPumpOut?: boolean;
  isAnyCovered?: boolean;
}

export type SlipsAtMarinaParams = BaseSearchParams;

export const searchMarinaRollup = (params: MarinaRollupSearchParams, signal?: AbortSignal) =>
  apiClient.get<MarinaRollupResultDto[]>('/marinas/search', { params, signal }).then((r) => r.data);

export const searchSlipsAtMarina = (marinaId: string, params: SlipsAtMarinaParams) =>
  apiClient.get<SlipSearchResultDto[]>(`/marinas/${marinaId}/slips/search`, { params }).then((r) => r.data);

export const getPublicSlipDetail = (slipId: string) =>
  apiClient.get<SlipDetailDto>(`/slips/${slipId}`).then((r) => r.data);

// ─── Reservations ─────────────────────────────────────────────────────────────

export interface ReservationDto {
  id: string;
  boaterUserId: string;
  vesselId: string;
  vesselName: string;
  slipId: string;
  slipName: string;
  marinaId: string;
  marinaName: string;
  hostMarinaId?: string | null;
  availabilityWindowId: string;
  arrivesAt: string;
  departsAt: string;
  nights: number;
  status: string;
  basePrice: number;
  fees: number;
  taxes: number;
  total: number;
  paymentStatus: string;
  instantBook: boolean;
  requestedAt: string;
  confirmedAt?: string | null;
  declinedAt?: string | null;
  cancelledAt?: string | null;
  cancelledByUserId?: string | null;
  notes?: string | null;
}

export interface CreateReservationData {
  vesselId: string;
  availabilityWindowId?: string | null;  // null when using direct slip rate
  slipId?: string | null;                // set when booking via direct default rate
  arrivesAt: string;
  departsAt: string;
  notes?: string | null;
}

export const createReservation = (data: CreateReservationData) =>
  apiClient.post<ReservationDto>('/reservations', data).then((r) => r.data);

export const getMyTrips = (status?: string) =>
  apiClient.get<ReservationDto[]>('/reservations/my-trips', { params: status ? { status } : undefined }).then((r) => r.data);

export const getReservation = (id: string) =>
  apiClient.get<ReservationDto>(`/reservations/${id}`).then((r) => r.data);

export const getMarinaReservations = (marinaId: string, status?: string) =>
  apiClient.get<ReservationDto[]>(`/marinas/${marinaId}/reservations`, { params: status ? { status } : undefined }).then((r) => r.data);

export const approveReservation = (marinaId: string, id: string) =>
  apiClient.post<ReservationDto>(`/marinas/${marinaId}/reservations/${id}/approve`).then((r) => r.data);

export const declineReservation = (marinaId: string, id: string) =>
  apiClient.post<ReservationDto>(`/marinas/${marinaId}/reservations/${id}/decline`).then((r) => r.data);

export const cancelReservation = (id: string) =>
  apiClient.post<ReservationDto>(`/reservations/${id}/cancel`).then((r) => r.data);

export const markNoShow = (marinaId: string, id: string) =>
  apiClient.post<ReservationDto>(`/marinas/${marinaId}/reservations/${id}/no-show`).then((r) => r.data);

// ─── Sublet / Owner Absences ──────────────────────────────────────────────────

export interface MySlipAssignmentDto {
  id: string;
  slipId: string;
  slipName: string;
  slipType: string;
  marinaId: string;
  marinaName: string;
  billingAccountId: string;
  vesselId: string;
  vesselName: string;
  assignmentType: string;
  startDate: string;
  endDate?: string | null;
  baseRate: number;
  allowHolderSublet: boolean;
  allowOwnerSubletWhenAway: boolean;
  isActive: boolean;
}

export interface OwnerAbsenceDto {
  id: string;
  slipAssignmentId: string;
  slipId: string;
  slipName: string;
  startsOn: string;
  endsOn: string;
  notes?: string | null;
  createdAt: string;
}

export interface CreateSubletWindowData {
  startsAt: string;
  endsAt: string;
  instantBook: boolean;
  minNights?: number | null;
  maxNights?: number | null;
  basePricePerNight: number;
  weeklyDiscount?: number | null;
  monthlyDiscount?: number | null;
  cleaningFee?: number | null;
}

export const getMySlipAssignments = () =>
  apiClient.get<MySlipAssignmentDto[]>('/me/slip-assignments').then((r) => r.data);

export const getAssignmentAbsences = (assignmentId: string) =>
  apiClient.get<OwnerAbsenceDto[]>(`/slip-assignments/${assignmentId}/absences`).then((r) => r.data);

export const createOwnerAbsence = (assignmentId: string, data: { startsOn: string; endsOn: string; notes?: string | null }) =>
  apiClient.post<OwnerAbsenceDto>(`/slip-assignments/${assignmentId}/away`, data).then((r) => r.data);

export const deleteOwnerAbsence = (assignmentId: string, absenceId: string) =>
  apiClient.delete(`/slip-assignments/${assignmentId}/absences/${absenceId}`);

export const createSubletWindow = (assignmentId: string, data: CreateSubletWindowData) =>
  apiClient.post<AvailabilityWindowDto>(`/slip-assignments/${assignmentId}/sublet-window`, data).then((r) => r.data);

export const getMarinaAbsences = (marinaId: string, slipId?: string) =>
  apiClient.get<OwnerAbsenceDto[]>(`/marinas/${marinaId}/absences`, {
    params: slipId ? { slipId } : undefined,
  }).then((r) => r.data);

// ─── Invoicing ────────────────────────────────────────────────────────────────

export interface InvoiceLineItemDto {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  slipAssignmentId?: string | null;
  reservationId?: string | null;
}

export interface PaymentDto {
  id: string;
  amount: number;
  paidOn: string;
  method: string;
  referenceNumber?: string | null;
  notes?: string | null;
  createdAt: string;
}

export interface InvoiceDto {
  id: string;
  marinaId: string;
  marinaName: string;
  billingAccountId: string;
  billingAccountName: string;
  reservationId?: string | null;
  slipAssignmentId?: string | null;
  invoiceNumber: string;
  status: string;
  issuedDate: string;
  dueDate: string;
  subTotal: number;
  taxAmount: number;
  totalAmount: number;
  amountPaid: number;
  balanceDue: number;
  notes?: string | null;
  createdAt: string;
  lineItems: InvoiceLineItemDto[];
  payments: PaymentDto[];
}

export interface InvoiceSummaryDto {
  id: string;
  invoiceNumber: string;
  status: string;
  billingAccountName: string;
  issuedDate: string;
  dueDate: string;
  totalAmount: number;
  amountPaid: number;
  balanceDue: number;
}

export interface CreateInvoiceLineItemData {
  description: string;
  quantity: number;
  unitPrice: number;
  slipAssignmentId?: string | null;
  reservationId?: string | null;
}

export interface CreateInvoiceData {
  billingAccountId: string;
  reservationId?: string | null;
  slipAssignmentId?: string | null;
  issuedDate: string;
  dueDate: string;
  taxAmount: number;
  notes?: string | null;
  lineItems: CreateInvoiceLineItemData[];
}

export interface RecordPaymentData {
  amount: number;
  paidOn: string;
  method: string;
  referenceNumber?: string | null;
  notes?: string | null;
}

export const getMarinaInvoices = (marinaId: string, params?: { status?: string; billingAccountId?: string }) =>
  apiClient.get<InvoiceSummaryDto[]>(`/marinas/${marinaId}/invoices`, { params }).then((r) => r.data);

export const getMarinaInvoice = (marinaId: string, invoiceId: string) =>
  apiClient.get<InvoiceDto>(`/marinas/${marinaId}/invoices/${invoiceId}`).then((r) => r.data);

export const createInvoice = (marinaId: string, data: CreateInvoiceData) =>
  apiClient.post<InvoiceDto>(`/marinas/${marinaId}/invoices`, data).then((r) => r.data);

export const sendInvoice = (marinaId: string, invoiceId: string) =>
  apiClient.post(`/marinas/${marinaId}/invoices/${invoiceId}/send`);

export const voidInvoice = (marinaId: string, invoiceId: string) =>
  apiClient.post(`/marinas/${marinaId}/invoices/${invoiceId}/void`);

export const recordPayment = (marinaId: string, invoiceId: string, data: RecordPaymentData) =>
  apiClient.post<PaymentDto>(`/marinas/${marinaId}/invoices/${invoiceId}/payments`, data).then((r) => r.data);

export const getMyInvoices = () =>
  apiClient.get<InvoiceSummaryDto[]>('/me/invoices').then((r) => r.data);

// ─── Maintenance & Announcements ──────────────────────────────────────────────

export interface WorkOrderSummaryDto {
  id: string;
  title: string;
  status: string;
  priority: string;
  scheduledDate: string | null;
  completedAt: string | null;
}

export interface MaintenanceRequestDto {
  id: string;
  marinaId: string;
  boaterUserId: string;
  boaterName: string;
  billingAccountId: string | null;
  vesselId: string | null;
  slipId: string | null;
  reservationId: string | null;
  title: string;
  description: string;
  status: string;
  priority: string;
  submittedAt: string;
  resolvedAt: string | null;
  workOrder: WorkOrderSummaryDto | null;
}

export interface WorkOrderDto {
  id: string;
  marinaId: string;
  maintenanceRequestId: string | null;
  title: string;
  description: string;
  assignedToUserId: string | null;
  assignedToName: string | null;
  status: string;
  priority: string;
  scheduledDate: string | null;
  completedAt: string | null;
  notes: string | null;
  createdAt: string;
}

export interface AnnouncementDto {
  id: string;
  marinaId: string;
  title: string;
  body: string;
  audience: string;
  publishedAt: string | null;
  expiresAt: string | null;
  isPinned: boolean;
  createdByUserId: string;
  createdAt: string;
}

// Maintenance requests
export const submitMaintenanceRequest = (
  marinaId: string,
  data: { title: string; description: string; priority?: string; vesselId?: string; slipId?: string }
) => apiClient.post<MaintenanceRequestDto>(`/marinas/${marinaId}/maintenance-requests`, data).then((r) => r.data);

export const getMarinaMaintenanceRequests = (marinaId: string, status?: string) =>
  apiClient.get<MaintenanceRequestDto[]>(`/marinas/${marinaId}/maintenance-requests`, { params: { status } }).then((r) => r.data);

export const updateMaintenanceRequestStatus = (
  marinaId: string, requestId: string, data: { status: string; priority: string }
) => apiClient.patch<MaintenanceRequestDto>(`/marinas/${marinaId}/maintenance-requests/${requestId}`, data).then((r) => r.data);

export const getMyMaintenanceRequests = () =>
  apiClient.get<MaintenanceRequestDto[]>('/me/maintenance-requests').then((r) => r.data);

// Work orders
export const getMarinaWorkOrders = (marinaId: string, status?: string) =>
  apiClient.get<WorkOrderDto[]>(`/marinas/${marinaId}/work-orders`, { params: { status } }).then((r) => r.data);

export const createWorkOrder = (
  marinaId: string,
  data: { maintenanceRequestId?: string; title: string; description: string; priority?: string; scheduledDate?: string }
) => apiClient.post<WorkOrderDto>(`/marinas/${marinaId}/work-orders`, data).then((r) => r.data);

export const updateWorkOrder = (marinaId: string, workOrderId: string, data: Partial<WorkOrderDto>) =>
  apiClient.put<WorkOrderDto>(`/marinas/${marinaId}/work-orders/${workOrderId}`, data).then((r) => r.data);

// Announcements
export const getMarinaAnnouncements = (marinaId: string) =>
  apiClient.get<AnnouncementDto[]>(`/marinas/${marinaId}/announcements`).then((r) => r.data);

export const createAnnouncement = (
  marinaId: string,
  data: { title: string; body: string; audience?: string; isPinned?: boolean; expiresAt?: string }
) => apiClient.post<AnnouncementDto>(`/marinas/${marinaId}/announcements`, data).then((r) => r.data);

export const updateAnnouncement = (
  marinaId: string, announcementId: string,
  data: { title: string; body: string; audience?: string; isPinned?: boolean; expiresAt?: string }
) => apiClient.put<AnnouncementDto>(`/marinas/${marinaId}/announcements/${announcementId}`, data).then((r) => r.data);

export const publishAnnouncement = (marinaId: string, announcementId: string) =>
  apiClient.post<AnnouncementDto>(`/marinas/${marinaId}/announcements/${announcementId}/publish`).then((r) => r.data);

export const deleteAnnouncement = (marinaId: string, announcementId: string) =>
  apiClient.delete(`/marinas/${marinaId}/announcements/${announcementId}`);

export const getMyAnnouncements = () =>
  apiClient.get<AnnouncementDto[]>('/me/announcements').then((r) => r.data);

// ─── Lease Inquiries ──────────────────────────────────────────────────────────

export interface LeaseInquiryDto {
  id: string;
  slipId: string;
  slipName: string;
  marinaId: string;
  marinaName: string;
  requestingUserId: string;
  requestingUserName: string;
  requestingUserEmail: string;
  vesselId?: string | null;
  vesselName?: string | null;
  desiredTerm: LeaseTerm;
  desiredStartDate?: string | null;
  message?: string | null;
  agreedRateKind?: RateKind | null;
  agreedBaseRate?: number | null;
  assignmentStartDate?: string | null;
  assignmentEndDate?: string | null;
  marinaNote?: string | null;
  status: LeaseInquiryStatus;
  reviewedByUserName?: string | null;
  reviewedAt?: string | null;
  approvedByUserName?: string | null;
  approvedAt?: string | null;
  declinedByUserName?: string | null;
  declinedAt?: string | null;
  slipAssignmentId?: string | null;
  billingAccountId?: string | null;
  createdAt: string;
}

export const createLeaseInquiry = (slipId: string, data: {
  vesselId?: string | null;
  desiredTerm: LeaseTerm;
  desiredStartDate?: string | null;
  message?: string | null;
}) => apiClient.post<LeaseInquiryDto>(`/slips/${slipId}/lease-inquiries`, data).then((r) => r.data);

export const getMyLeaseInquiries = () =>
  apiClient.get<LeaseInquiryDto[]>('/me/lease-inquiries').then((r) => r.data);

export const getMarinaLeaseInquiries = (marinaId: string, params?: { slipId?: string; status?: string }) =>
  apiClient.get<LeaseInquiryDto[]>(`/marinas/${marinaId}/lease-inquiries`, { params }).then((r) => r.data);

export const updateLeaseInquiry = (marinaId: string, id: string, data: {
  agreedRateKind?: string | null;
  agreedBaseRate?: number | null;
  assignmentStartDate?: string | null;
  assignmentEndDate?: string | null;
  marinaNote?: string | null;
  markUnderReview?: boolean;
}) => apiClient.patch<LeaseInquiryDto>(`/marinas/${marinaId}/lease-inquiries/${id}`, data).then((r) => r.data);

export const approveLeaseInquiry = (marinaId: string, id: string) =>
  apiClient.post<LeaseInquiryDto>(`/marinas/${marinaId}/lease-inquiries/${id}/approve`).then((r) => r.data);

export const declineLeaseInquiry = (marinaId: string, id: string, reason?: string) =>
  apiClient.post<LeaseInquiryDto>(`/marinas/${marinaId}/lease-inquiries/${id}/decline`, { reason }).then((r) => r.data);

export const withdrawLeaseInquiry = (id: string) =>
  apiClient.post<LeaseInquiryDto>(`/lease-inquiries/${id}/withdraw`).then((r) => r.data);

// ─── Platform Operator ────────────────────────────────────────────────────────

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface TenantSummaryDto {
  id: string;
  name: string;
  slug: string;
  subscriptionTier: string;
  isActive: boolean;
  isDemo: boolean;
  suspendedAt: string | null;
  createdAt: string;
  marinaCount: number;
}

export interface UserSummaryDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  emailConfirmed: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  isPlatformOperator: boolean;
}

export interface AuditLogEntryDto {
  id: string;
  tenantId: string | null;
  actorUserId: string | null;
  actorName: string;
  action: string;
  targetType: string | null;
  targetId: string | null;
  details: string | null;
  occurredAt: string;
}

export interface ListingModerationDto {
  id: string;
  slipId: string;
  slipName: string;
  marinaId: string;
  marinaName: string;
  tenantId: string;
  tenantName: string;
  status: string;
  startsAt: string;
  endsAt: string;
  basePricePerNight: number;
  listedByKind: string;
  createdAt: string;
}

export interface VesselSummaryDto {
  id: string;
  name: string;
  make: string | null;
  model: string | null;
  year: number | null;
  length: number;
  beam: number;
  draft: number;
  boatType: string;
  hullColor: string | null;
  registrationNumber: string | null;
  registrationState: string | null;
  isArchived: boolean;
  createdAt: string;
}

export interface ReservationSummaryDto {
  id: string;
  vesselId: string;
  vesselName: string;
  slipId: string;
  slipName: string;
  marinaId: string;
  marinaName: string;
  arrivesAt: string;
  departsAt: string;
  status: string;
  basePrice: number;
  fees: number;
  taxes: number;
  total: number;
  requestedAt: string;
  confirmedAt: string | null;
  cancelledAt: string | null;
}

export interface MembershipSummaryDto {
  id: string;
  userId: string;
  tenantId: string;
  marinaId: string | null;
  scope: string;
  role: string;
  invitedAt: string;
  acceptedAt: string | null;
  isPending: boolean;
}

export interface UserProfileDto {
  user: UserSummaryDto;
  vessels: VesselSummaryDto[];
  reservations: ReservationSummaryDto[];
  memberships: MembershipSummaryDto[];
  recentActivity: AuditLogEntryDto[];
}

// Tenants
export const getPlatformTenants = (search?: string, page = 1, pageSize = 25) =>
  apiClient.get<PagedResult<TenantSummaryDto>>('/platform/tenants', { params: { search, page, pageSize } }).then((r) => r.data);

export const createPlatformTenant = (data: { name: string; slug: string; billingEmail?: string; subscriptionTier?: string }) =>
  apiClient.post<{ id: string; name: string; slug: string }>('/platform/tenants', data).then((r) => r.data);

export const suspendTenant = (tenantId: string, reason?: string) =>
  apiClient.patch(`/platform/tenants/${tenantId}/suspend`, { reason });

export const reactivateTenant = (tenantId: string) =>
  apiClient.patch(`/platform/tenants/${tenantId}/reactivate`, {});

// Users
export const searchPlatformUsers = (q?: string, page = 1, pageSize = 25) =>
  apiClient.get<PagedResult<UserSummaryDto>>('/platform/users', { params: { q, page, pageSize } }).then((r) => r.data);

export const forceSignOut = (userId: string) =>
  apiClient.post(`/platform/users/${userId}/force-signout`);

export const deactivatePlatformUser = (userId: string, reason?: string) =>
  apiClient.patch(`/platform/users/${userId}/deactivate`, { reason });

export const activatePlatformUser = (userId: string) =>
  apiClient.patch(`/platform/users/${userId}/activate`, {});

export const getUserProfile = (userId: string) =>
  apiClient.get<UserProfileDto>(`/platform/users/${userId}`).then((r) => r.data);

export const changeUserEmail = (userId: string, newEmail: string) =>
  apiClient.patch(`/platform/users/${userId}/email`, { newEmail });

export const changeUserName = (userId: string, firstName?: string | null, lastName?: string | null) =>
  apiClient.patch(`/platform/users/${userId}/name`, { firstName, lastName });

export const initiatePasswordReset = (userId: string) =>
  apiClient.post(`/platform/users/${userId}/password-reset`);

// Audit log
export const getPlatformAuditLog = (tenantId?: string, page = 1, pageSize = 50) =>
  apiClient.get<PagedResult<AuditLogEntryDto>>('/platform/audit-log', { params: { tenantId, page, pageSize } }).then((r) => r.data);

// Listings
export const getPlatformListings = (page = 1, pageSize = 50) =>
  apiClient.get<PagedResult<ListingModerationDto>>('/platform/listings', { params: { page, pageSize } }).then((r) => r.data);

export const removePlatformListing = (listingId: string, reason?: string) =>
  apiClient.delete(`/platform/listings/${listingId}`, { data: { reason } });

// ─── Pricing Plans ───────────────────────────────────────────────────────────

export interface AmenityAddOnDto {
  amenity: 'Covered' | 'Electric30A' | 'Electric50A' | 'HasWater' | 'HasPumpOut';
  transientAmount?: number | null;
  leaseAmount?: number | null;
}

export interface PricingPlanDto {
  id: string;
  marinaId: string;
  name: string;
  isDefault: boolean;
  transientRateKind?: 'Flat' | 'PerFoot' | 'PerArea' | null;
  transientAmount?: number | null;
  transientMinCharge?: number | null;
  leaseRateKind?: 'Flat' | 'PerFoot' | 'PerArea' | null;
  leaseAmount?: number | null;
  leaseMinCharge?: number | null;
  amenityAddOns: AmenityAddOnDto[];
  createdAt: string;
  updatedAt: string;
}

export interface BulkAssignResultDto {
  assignedCount: number;
}

export type PricingPlanUpsertData = {
  name: string;
  isDefault: boolean;
  transientRateKind?: string | null;
  transientAmount?: number | null;
  transientMinCharge?: number | null;
  leaseRateKind?: string | null;
  leaseAmount?: number | null;
  leaseMinCharge?: number | null;
  amenityAddOns: AmenityAddOnDto[];
};

export const getPricingPlans = (marinaId: string) =>
  apiClient.get<PricingPlanDto[]>(`/marinas/${marinaId}/pricing/plans`).then((r) => r.data);

export const getPricingPlan = (marinaId: string, planId: string) =>
  apiClient.get<PricingPlanDto>(`/marinas/${marinaId}/pricing/plans/${planId}`).then((r) => r.data);

export const createPricingPlan = (marinaId: string, data: PricingPlanUpsertData) =>
  apiClient.post<PricingPlanDto>(`/marinas/${marinaId}/pricing/plans`, data).then((r) => r.data);

export const updatePricingPlan = (marinaId: string, planId: string, data: Partial<PricingPlanUpsertData>) =>
  apiClient.put<PricingPlanDto>(`/marinas/${marinaId}/pricing/plans/${planId}`, data).then((r) => r.data);

export const deletePricingPlan = (marinaId: string, planId: string) =>
  apiClient.delete(`/marinas/${marinaId}/pricing/plans/${planId}`);

export const setDefaultPricingPlan = (marinaId: string, planId: string) =>
  apiClient.post<PricingPlanDto>(`/marinas/${marinaId}/pricing/plans/${planId}/set-default`).then((r) => r.data);

export const bulkAssignPricingPlan = (
  marinaId: string,
  data: {
    pricingPlanId: string;
    dockId?: string | null;
    minLength?: number | null;
    maxLength?: number | null;
    minBeam?: number | null;
    maxBeam?: number | null;
    amenities?: string[] | null;
    currentPlanId?: string | null;
  }
) => apiClient.post<BulkAssignResultDto>(`/marinas/${marinaId}/pricing/plans/bulk-assign`, data).then((r) => r.data);

// ─── Pricing Rules (legacy — retained for schema.d.ts compatibility) ──────────

export interface PricingRulePredicateDto {
  listingKind: string;
  leaseTerm?: string | null;
  minLength?: number | null;
  maxLength?: number | null;
  minBeam?: number | null;
  maxBeam?: number | null;
  requiresElectric?: boolean | null;
  requiresWater?: boolean | null;
  requiresPumpOut?: boolean | null;
  requiresCovered?: boolean | null;
  requiresIndoor?: boolean | null;
}

export interface PricingRuleDto {
  id: string;
  marinaId: string;
  name: string;
  contributionKind: 'Base' | 'Surcharge';
  priority: number;
  predicate: PricingRulePredicateDto;
  rateKind: 'Flat' | 'PerFoot';
  amount: number;
  minCharge?: number | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface PriceBreakdownRuleDto {
  ruleId: string;
  name: string;
  contributionKind: string;
  contribution: number;
}

export interface PriceBreakdownAdjustmentDto {
  adjustmentId: string;
  label: string;
  amount: number;
}

export interface PriceBreakdownDto {
  base?: PriceBreakdownRuleDto | null;
  surcharges: PriceBreakdownRuleDto[];
  adjustments: PriceBreakdownAdjustmentDto[];
  minChargeApplied: boolean;
  total?: number | null;
}

export interface SlipPriceAdjustmentDto {
  id: string;
  slipId: string;
  listingKind: string;
  label: string;
  amount: number;
  createdAt: string;
  updatedAt: string;
}

export const getPricingRules = (marinaId: string) =>
  apiClient.get<PricingRuleDto[]>(`/marinas/${marinaId}/pricing/rules`).then((r) => r.data);

export const getPricingRule = (marinaId: string, ruleId: string) =>
  apiClient.get<PricingRuleDto>(`/marinas/${marinaId}/pricing/rules/${ruleId}`).then((r) => r.data);

export const previewPrice = (marinaId: string, slipId: string, listingKind: string, asOf?: string) =>
  apiClient.get<PriceBreakdownDto>(`/marinas/${marinaId}/pricing/preview`, {
    params: { slipId, listingKind, asOf },
  }).then((r) => r.data);

export const createPricingRule = (
  marinaId: string,
  data: {
    name: string;
    contributionKind: string;
    priority: number;
    predicate: PricingRulePredicateDto;
    rateKind: string;
    amount: number;
    minCharge?: number | null;
    effectiveFrom: string;
    effectiveTo?: string | null;
  }
) => apiClient.post<PricingRuleDto>(`/marinas/${marinaId}/pricing/rules`, data).then((r) => r.data);

export const updatePricingRule = (marinaId: string, ruleId: string, data: Partial<{
  name: string;
  contributionKind: string;
  priority: number;
  predicate: PricingRulePredicateDto;
  rateKind: string;
  amount: number;
  minCharge: number | null;
  effectiveFrom: string;
  effectiveTo: string | null;
}>) => apiClient.patch<PricingRuleDto>(`/marinas/${marinaId}/pricing/rules/${ruleId}`, data).then((r) => r.data);

export const deletePricingRule = (marinaId: string, ruleId: string) =>
  apiClient.delete(`/marinas/${marinaId}/pricing/rules/${ruleId}`);

export const getSlipPriceAdjustments = (slipId: string, marinaId: string) =>
  apiClient.get<SlipPriceAdjustmentDto[]>(`/slips/${slipId}/price-adjustments`, { params: { marinaId } }).then((r) => r.data);

export const createSlipPriceAdjustment = (slipId: string, marinaId: string, data: { listingKind: string; label: string; amount: number }) =>
  apiClient.post<SlipPriceAdjustmentDto>(`/slips/${slipId}/price-adjustments`, data, { params: { marinaId } }).then((r) => r.data);

export const updateSlipPriceAdjustment = (slipId: string, adjustmentId: string, marinaId: string, data: { label?: string; amount?: number }) =>
  apiClient.patch<SlipPriceAdjustmentDto>(`/slips/${slipId}/price-adjustments/${adjustmentId}`, data, { params: { marinaId } }).then((r) => r.data);

export const deleteSlipPriceAdjustment = (slipId: string, adjustmentId: string, marinaId: string) =>
  apiClient.delete(`/slips/${slipId}/price-adjustments/${adjustmentId}`, { params: { marinaId } });
