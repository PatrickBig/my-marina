import { useQuery } from '@tanstack/react-query';
import { getMarinaReservations, getMarinaInvoices, getMarinaWorkOrders, getMarinaLeaseInquiries } from '@/api/api';

export interface MarinaCounters {
  pendingReservations: number;
  overdueInvoices: number;
  openWorkOrders: number;
  pendingInquiries: number;
}

export function useMarinaCounters(marinaId: string) {
  return useQuery<MarinaCounters>({
    queryKey: ['marina-counters', marinaId],
    queryFn: async () => {
      const [reservations, invoices, workOrders, inquiries] = await Promise.all([
        getMarinaReservations(marinaId, 'PendingApproval'),
        getMarinaInvoices(marinaId, { status: 'Overdue' }),
        getMarinaWorkOrders(marinaId, 'InProgress'),
        getMarinaLeaseInquiries(marinaId, { status: 'Pending' }),
      ]);
      return {
        pendingReservations: reservations.length,
        overdueInvoices: invoices.length,
        openWorkOrders: workOrders.length,
        pendingInquiries: inquiries.length,
      };
    },
    staleTime: 60_000,
  });
}
