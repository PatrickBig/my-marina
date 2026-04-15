import { apiClient } from "./client";
import type { components } from "./schema";

export type LoginResult = components["schemas"]["LoginResult"];
export type MarinaDto = components["schemas"]["MarinaDto"];
export type DockDto = components["schemas"]["DockDto"];
export type SlipDto = components["schemas"]["SlipDto"];
export type CustomerAccountDto = components["schemas"]["CustomerAccountDto"];
export type BoatDto = components["schemas"]["BoatDto"];
export type SlipAssignmentDto = components["schemas"]["SlipAssignmentDto"];
export type AddressDto = components["schemas"]["AddressDto"];
export type InviteStaffResult = components["schemas"]["InviteStaffResult"];

// ─── Auth ────────────────────────────────────────────────────────────────────

export const login = (email: string, password: string) =>
  apiClient.post<LoginResult>("/auth/login", { email, password }).then((r) => r.data);

// ─── Marinas ─────────────────────────────────────────────────────────────────

export const getMarinas = () =>
  apiClient.get<MarinaDto[]>("/marinas").then((r) => r.data);

export const getMarina = (id: string) =>
  apiClient.get<MarinaDto>(`/marinas/${id}`).then((r) => r.data);

export const createMarina = (data: {
  name: string; address: AddressDto; phoneNumber: string; email: string;
  timeZoneId: string; website?: string | null; description?: string | null;
}) => apiClient.post<string>("/marinas", data).then((r) => r.data);

export const updateMarina = (id: string, data: {
  name: string; address: AddressDto; phoneNumber: string; email: string;
  timeZoneId: string; website?: string | null; description?: string | null;
}) => apiClient.put(`/marinas/${id}`, data);

// ─── Docks ───────────────────────────────────────────────────────────────────

export const getDocks = (marinaId: string) =>
  apiClient.get<DockDto[]>(`/marinas/${marinaId}/docks`).then((r) => r.data);

export const createDock = (marinaId: string, data: { name: string; description?: string | null; sortOrder?: number }) =>
  apiClient.post<string>(`/marinas/${marinaId}/docks`, data).then((r) => r.data);

export const updateDock = (dockId: string, data: { name: string; description?: string | null; sortOrder?: number }) =>
  apiClient.put(`/docks/${dockId}`, data);

export const deleteDock = (dockId: string) =>
  apiClient.delete(`/docks/${dockId}`);

// ─── Slips ───────────────────────────────────────────────────────────────────

export type CreateSlipData = components["schemas"]["CreateSlipRequest"];
export type UpdateSlipData = components["schemas"]["UpdateSlipRequest"];

export const getSlips = (marinaId: string) =>
  apiClient.get<SlipDto[]>(`/marinas/${marinaId}/slips`).then((r) => r.data);

export const createSlip = (marinaId: string, data: CreateSlipData) =>
  apiClient.post<string>(`/marinas/${marinaId}/slips`, data).then((r) => r.data);

export const updateSlip = (slipId: string, data: UpdateSlipData) =>
  apiClient.put(`/slips/${slipId}`, data);

export const deleteSlip = (slipId: string) =>
  apiClient.delete(`/slips/${slipId}`);

export const getAvailableSlips = (
  marinaId: string,
  params: { boatLength: number; boatBeam: number; boatDraft: number; startDate: string; endDate?: string }
) => apiClient.get<SlipDto[]>(`/marinas/${marinaId}/slips/available`, { params }).then((r) => r.data);

// ─── Customers ───────────────────────────────────────────────────────────────

export type CreateCustomerData = components["schemas"]["CreateCustomerAccountCommand"];
export type UpdateCustomerData = components["schemas"]["UpdateCustomerRequest"];

export const getCustomers = () =>
  apiClient.get<CustomerAccountDto[]>("/customers").then((r) => r.data);

export const getCustomer = (id: string) =>
  apiClient.get<CustomerAccountDto>(`/customers/${id}`).then((r) => r.data);

export const createCustomer = (data: CreateCustomerData) =>
  apiClient.post<string>("/customers", data).then((r) => r.data);

export const updateCustomer = (id: string, data: UpdateCustomerData) =>
  apiClient.put(`/customers/${id}`, data);

export const deactivateCustomer = (id: string) =>
  apiClient.post(`/customers/${id}/deactivate`);

// ─── Boats ───────────────────────────────────────────────────────────────────

export type CreateBoatData = components["schemas"]["CreateBoatRequest"];
export type UpdateBoatData = components["schemas"]["UpdateBoatRequest"];

export const getBoats = (customerAccountId: string) =>
  apiClient.get<BoatDto[]>(`/customers/${customerAccountId}/boats`).then((r) => r.data);

export const createBoat = (customerAccountId: string, data: CreateBoatData) =>
  apiClient.post<string>(`/customers/${customerAccountId}/boats`, data).then((r) => r.data);

export const updateBoat = (boatId: string, data: UpdateBoatData) =>
  apiClient.put(`/boats/${boatId}`, data);

export const deleteBoat = (boatId: string) =>
  apiClient.delete(`/boats/${boatId}`);

// ─── Slip Assignments ─────────────────────────────────────────────────────────

export type CreateAssignmentData = components["schemas"]["CreateSlipAssignmentCommand"];

export const getAssignments = (params?: {
  slipId?: string; customerAccountId?: string; activeOnly?: boolean;
}) => apiClient.get<SlipAssignmentDto[]>("/slip-assignments", { params }).then((r) => r.data);

export const createAssignment = (data: CreateAssignmentData) =>
  apiClient.post<string>("/slip-assignments", data).then((r) => r.data);

export const endAssignment = (assignmentId: string, endDate: string) =>
  apiClient.post(`/slip-assignments/${assignmentId}/end`, { endDate });

// ─── Staff ───────────────────────────────────────────────────────────────────

export const inviteStaff = (data: {
  marinaId: string; email: string; firstName: string; lastName: string; role: number;
}) => apiClient.post<InviteStaffResult>("/staff/invite", data).then((r) => r.data);
