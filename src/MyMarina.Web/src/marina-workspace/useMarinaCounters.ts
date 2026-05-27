import { useQuery } from '@tanstack/react-query';
import { getMarinaReservations, getMarinaInvoices, getMarinaWorkOrders } from '@/api/api';

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
        getMarinaReservations(marinaId, 'PendingApproval'),
        getMarinaInvoices(marinaId, { status: 'Overdue' }),
        getMarinaWorkOrders(marinaId, 'InProgress'),
      ]);
      return {
        pendingReservations: reservations.length,
        overdueInvoices: invoices.length,
        openWorkOrders: workOrders.length,
      };
    },
    staleTime: 60_000,
  });
}
