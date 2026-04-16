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

// ─── Invoices ────────────────────────────────────────────────────────────────

export type InvoiceStatus = 0 | 1 | 2 | 3 | 4 | 5; // Draft|Sent|PartiallyPaid|Paid|Overdue|Voided
export type PaymentMethod = 0 | 1 | 2 | 3 | 4;     // Cash|Check|CreditCard|BankTransfer|Other

export interface InvoiceDto {
  id: string;
  invoiceNumber: string;
  customerAccountId: string;
  customerDisplayName: string;
  status: InvoiceStatus;
  issuedDate: string;
  dueDate: string;
  subTotal: number;
  taxAmount: number;
  totalAmount: number;
  amountPaid: number;
  balanceDue: number;
  notes: string | null;
  createdAt: string;
}

export interface InvoiceLineItemDto {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  slipAssignmentId: string | null;
}

export interface PaymentDto {
  id: string;
  amount: number;
  paidOn: string;
  method: PaymentMethod;
  referenceNumber: string | null;
  notes: string | null;
  recordedByUserId: string;
  createdAt: string;
}

export interface InvoiceDetailDto extends InvoiceDto {
  lineItems: InvoiceLineItemDto[];
  payments: PaymentDto[];
}

export const getInvoices = (params?: {
  customerAccountId?: string; status?: InvoiceStatus; issuedFrom?: string; issuedTo?: string;
}) => apiClient.get<InvoiceDto[]>("/invoices", { params }).then((r) => r.data);

export const getInvoice = (id: string) =>
  apiClient.get<InvoiceDetailDto>(`/invoices/${id}`).then((r) => r.data);

export const createInvoice = (data: {
  customerAccountId: string; issuedDate: string; dueDate: string; notes?: string | null;
}) => apiClient.post<string>("/invoices", data).then((r) => r.data);

export const updateInvoiceDraft = (id: string, data: {
  issuedDate: string; dueDate: string; notes?: string | null;
}) => apiClient.put(`/invoices/${id}`, data);

export const sendInvoice = (id: string) =>
  apiClient.post(`/invoices/${id}/send`);

export const voidInvoice = (id: string) =>
  apiClient.post(`/invoices/${id}/void`);

export const addLineItem = (invoiceId: string, data: {
  description: string; quantity: number; unitPrice: number; slipAssignmentId?: string | null;
}) => apiClient.post<string>(`/invoices/${invoiceId}/line-items`, data).then((r) => r.data);

export const updateLineItem = (invoiceId: string, lineItemId: string, data: {
  description: string; quantity: number; unitPrice: number;
}) => apiClient.put(`/invoices/${invoiceId}/line-items/${lineItemId}`, data);

export const removeLineItem = (invoiceId: string, lineItemId: string) =>
  apiClient.delete(`/invoices/${invoiceId}/line-items/${lineItemId}`);

export const recordPayment = (invoiceId: string, data: {
  amount: number; paidOn: string; method: PaymentMethod; referenceNumber?: string | null; notes?: string | null;
}) => apiClient.post<string>(`/invoices/${invoiceId}/payments`, data).then((r) => r.data);

// ─── Staff ───────────────────────────────────────────────────────────────────

export const inviteStaff = (data: {
  marinaId: string; email: string; firstName: string; lastName: string; role: number;
}) => apiClient.post<InviteStaffResult>("/staff/invite", data).then((r) => r.data);

export const inviteCustomer = (customerAccountId: string, data: {
  email: string; firstName: string; lastName: string;
}) => apiClient.post<{ userId: string; temporaryPassword: string }>(
  `/customers/${customerAccountId}/invite`, data
).then((r) => r.data);

// ─── Portal (Customer self-service) ──────────────────────────────────────────

export interface PortalMeDto {
  userId: string; email: string; firstName: string; lastName: string;
  customerAccountId: string; accountDisplayName: string; billingEmail: string; billingPhone: string | null;
}

export interface PortalSlipDto {
  id: string; slipId: string; slipName: string; dockName: string | null; marinaName: string;
  boatName: string; assignmentType: number; startDate: string; endDate: string | null;
  rateOverride: number | null; notes: string | null;
}

export interface PortalBoatDto {
  id: string; name: string; make: string | null; model: string | null; year: number | null;
  length: number; beam: number; draft: number; boatType: number;
  registrationNumber: string | null; insuranceExpiresOn: string | null;
}

export interface PortalInvoiceDto {
  id: string; invoiceNumber: string; status: InvoiceStatus;
  issuedDate: string; dueDate: string; totalAmount: number;
  amountPaid: number; balanceDue: number; notes: string | null; createdAt: string;
}

export interface PortalLineItemDto {
  description: string; quantity: number; unitPrice: number; lineTotal: number;
}

export interface PortalPaymentDto {
  amount: number; paidOn: string; method: PaymentMethod; referenceNumber: string | null;
}

export interface PortalInvoiceDetailDto extends PortalInvoiceDto {
  subTotal: number; taxAmount: number;
  lineItems: PortalLineItemDto[];
  payments: PortalPaymentDto[];
}

export interface PortalMaintenanceRequestDto {
  id: string; title: string; description: string; status: number; priority: number;
  slipId: string | null; slipName: string | null; boatId: string | null; boatName: string | null;
  submittedAt: string; resolvedAt: string | null;
}

export interface PortalAnnouncementDto {
  id: string; title: string; body: string; isPinned: boolean;
  publishedAt: string; expiresAt: string | null; marinaName: string;
}

export const getPortalMe = () =>
  apiClient.get<PortalMeDto>("/portal/me").then((r) => r.data);

export const getPortalSlip = () =>
  apiClient.get<PortalSlipDto>("/portal/slip").then((r) => r.data).catch(() => null);

export const getPortalBoats = () =>
  apiClient.get<PortalBoatDto[]>("/portal/boats").then((r) => r.data);

export const getPortalInvoices = () =>
  apiClient.get<PortalInvoiceDto[]>("/portal/invoices").then((r) => r.data);

export const getPortalInvoice = (id: string) =>
  apiClient.get<PortalInvoiceDetailDto>(`/portal/invoices/${id}`).then((r) => r.data);

export const getPortalMaintenanceRequests = () =>
  apiClient.get<PortalMaintenanceRequestDto[]>("/portal/maintenance-requests").then((r) => r.data);

export const submitMaintenanceRequest = (data: {
  title: string; description: string; priority: number; slipId?: string | null; boatId?: string | null;
}) => apiClient.post<string>("/portal/maintenance-requests", data).then((r) => r.data);

export const getPortalAnnouncements = () =>
  apiClient.get<PortalAnnouncementDto[]>("/portal/announcements").then((r) => r.data);

// ─── Announcements (Operator) ─────────────────────────────────────────────────

export interface AnnouncementDto {
  id: string;
  marinaId: string;
  title: string;
  body: string;
  isPinned: boolean;
  isPublished: boolean;
  publishedAt: string | null;
  expiresAt: string | null;
  createdByUserId: string;
  createdAt: string;
}

export const getAnnouncements = (marinaId: string, params?: {
  includeDrafts?: boolean; includeExpired?: boolean;
}) => apiClient.get<AnnouncementDto[]>(`/marinas/${marinaId}/announcements`, { params }).then((r) => r.data);

export const getAnnouncement = (marinaId: string, id: string) =>
  apiClient.get<AnnouncementDto>(`/marinas/${marinaId}/announcements/${id}`).then((r) => r.data);

export const createAnnouncement = (marinaId: string, data: {
  title: string; body: string; publish: boolean; isPinned: boolean; expiresAt?: string | null;
}) => apiClient.post<string>(`/marinas/${marinaId}/announcements`, data).then((r) => r.data);

export const updateAnnouncement = (marinaId: string, id: string, data: {
  title: string; body: string; isPinned: boolean; expiresAt?: string | null;
}) => apiClient.put(`/marinas/${marinaId}/announcements/${id}`, data);

export const publishAnnouncement = (marinaId: string, id: string) =>
  apiClient.post(`/marinas/${marinaId}/announcements/${id}/publish`);

export const unpublishAnnouncement = (marinaId: string, id: string) =>
  apiClient.post(`/marinas/${marinaId}/announcements/${id}/unpublish`);

export const deleteAnnouncement = (marinaId: string, id: string) =>
  apiClient.delete(`/marinas/${marinaId}/announcements/${id}`);

// ─── Maintenance Requests (Operator) ─────────────────────────────────────────

export type MaintenanceStatus = 0 | 1 | 2 | 3 | 4; // Submitted|UnderReview|InProgress|Completed|Declined
export type Priority = 0 | 1 | 2 | 3;              // Low|Medium|High|Urgent

export interface MaintenanceRequestDto {
  id: string;
  customerAccountId: string;
  customerDisplayName: string;
  slipId: string | null;
  slipName: string | null;
  boatId: string | null;
  boatName: string | null;
  title: string;
  description: string;
  status: MaintenanceStatus;
  priority: Priority;
  submittedAt: string;
  resolvedAt: string | null;
  workOrderId: string | null;
}

export const getMaintenanceRequests = (params?: {
  status?: MaintenanceStatus; priority?: Priority;
}) => apiClient.get<MaintenanceRequestDto[]>("/maintenance-requests", { params }).then((r) => r.data);

export const getMaintenanceRequest = (id: string) =>
  apiClient.get<MaintenanceRequestDto>(`/maintenance-requests/${id}`).then((r) => r.data);

export const updateMaintenanceStatus = (id: string, status: MaintenanceStatus) =>
  apiClient.post(`/maintenance-requests/${id}/status`, { status });

// ─── Work Orders ──────────────────────────────────────────────────────────────

export type WorkOrderStatus = 0 | 1 | 2 | 3 | 4; // Open|InProgress|OnHold|Completed|Cancelled

export interface WorkOrderDto {
  id: string;
  maintenanceRequestId: string | null;
  maintenanceRequestTitle: string | null;
  title: string;
  description: string;
  assignedToUserId: string | null;
  assignedToName: string | null;
  status: WorkOrderStatus;
  priority: Priority;
  scheduledDate: string | null;
  completedAt: string | null;
  notes: string | null;
  createdAt: string;
}

export const getWorkOrders = (params?: {
  status?: WorkOrderStatus; assignedToUserId?: string;
}) => apiClient.get<WorkOrderDto[]>("/work-orders", { params }).then((r) => r.data);

export const getWorkOrder = (id: string) =>
  apiClient.get<WorkOrderDto>(`/work-orders/${id}`).then((r) => r.data);

export const createWorkOrder = (data: {
  title: string; description: string; priority: Priority;
  maintenanceRequestId?: string | null; assignedToUserId?: string | null;
  scheduledDate?: string | null; notes?: string | null;
}) => apiClient.post<string>("/work-orders", data).then((r) => r.data);

export const updateWorkOrder = (id: string, data: {
  title: string; description: string; priority: Priority; status: WorkOrderStatus;
  assignedToUserId?: string | null; scheduledDate?: string | null; notes?: string | null;
}) => apiClient.put(`/work-orders/${id}`, data);

export const completeWorkOrder = (id: string, notes?: string | null) =>
  apiClient.post(`/work-orders/${id}/complete`, { notes });
