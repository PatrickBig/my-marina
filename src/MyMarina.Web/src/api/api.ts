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
  startsAt: string;
  endsAt: string;
  instantBook: boolean;
  minNights?: number | null;
  maxNights?: number | null;
  basePricePerNight: number;
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

// ─── Slip Search (public) ─────────────────────────────────────────────────────

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
  bestWindowId: string;
  basePricePerNight: number;
  instantBook: boolean;
  cleaningFee?: number | null;
  minNights?: number | null;
  maxNights?: number | null;
  distanceMiles: number;
}

export interface PublicWindowSummaryDto {
  id: string;
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
  openWindows: PublicWindowSummaryDto[];
}

export interface SlipSearchParams {
  lat: number;
  lon: number;
  radiusMiles?: number;
  arrivesAt?: string;
  departsAt?: string;
  vesselLength?: number;
  vesselBeam?: number;
  vesselDraft?: number;
  slipType?: string;
  hasElectric?: boolean;
  hasWater?: boolean;
  page?: number;
  pageSize?: number;
}

export const searchSlips = (params: SlipSearchParams) =>
  apiClient.get<SlipSearchResultDto[]>('/slips/search', { params }).then((r) => r.data);

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
  availabilityWindowId: string;
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
