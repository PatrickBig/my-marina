/**
 * useMarinaCounters — single TanStack Query that backs the sidebar's pending /
 * overdue / open badge counts.
 *
 * Invalidate from any mutation that changes one of the underlying counts:
 *   queryClient.invalidateQueries({ queryKey: ['marina-counters', marinaId] });
 *
 * Examples that should invalidate:
 *   - approveReservation / declineReservation / markNoShow
 *   - recordPayment / voidInvoice
 *   - createWorkOrder / updateWorkOrder (status transitions)
 */

import { useQuery } from '@tanstack/react-query';
import {
  getMarinaReservations,
  getMarinaInvoices,
  getMarinaWorkOrders,
} from '@/api/api';

export interface MarinaCounters {
  pendingReservations: number;
  overdueInvoices: number;
  openWorkOrders: number;
}

export function useMarinaCounters(marinaId: string) {
  return useQuery<MarinaCounters>({
    queryKey: ['marina-counters', marinaId],
    queryFn: async () => {
      const [reservations, invoices, workOrders] = await Promise.all([
        getMarinaReservations(marinaId, { status: 'PendingApproval' }),
        getMarinaInvoices(marinaId, { status: 'Overdue' }),
        getMarinaWorkOrders(marinaId, { status: 'InProgress' }),
      ]);
      return {
        pendingReservations: reservations.length,
        overdueInvoices: invoices.length,
        openWorkOrders: workOrders.length,
      };
    },
    staleTime: 60_000, // 1 minute — these don't need to be live-tailed.
  });
}
