import { useQuery } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import { FileText } from "lucide-react";
import { getPortalInvoices } from "@/api/api";
import { Badge } from "@/components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

const STATUS_LABELS: Record<number, string> = {
  0: "Draft", 1: "Sent", 2: "Partially Paid", 3: "Paid", 4: "Overdue", 5: "Voided",
};
const STATUS_VARIANTS: Record<number, "secondary" | "default" | "warning" | "success" | "destructive"> = {
  0: "secondary", 1: "default", 2: "warning", 3: "success", 4: "destructive", 5: "secondary",
};

function fmt(n: number) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(n);
}

export function PortalInvoicesPage() {
  const { data: invoices, isLoading } = useQuery({ queryKey: ["portal-invoices"], queryFn: getPortalInvoices });

  if (isLoading) return <div className="p-8 text-muted-foreground">Loading…</div>;

  return (
    <div className="p-8 space-y-6">
      <h1 className="text-2xl font-bold">Invoices</h1>

      {!invoices || invoices.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-md border border-dashed p-16 text-center">
          <FileText className="h-10 w-10 text-muted-foreground mb-4" />
          <p className="text-muted-foreground">No invoices yet.</p>
        </div>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Invoice #</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Issued</TableHead>
              <TableHead>Due</TableHead>
              <TableHead className="text-right">Total</TableHead>
              <TableHead className="text-right">Balance Due</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {invoices.map((inv) => (
              <TableRow key={inv.id}>
                <TableCell>
                  <Link
                    to="/portal/invoices/$invoiceId"
                    params={{ invoiceId: inv.id }}
                    className="font-mono font-medium hover:underline"
                  >
                    {inv.invoiceNumber}
                  </Link>
                </TableCell>
                <TableCell>
                  <Badge variant={STATUS_VARIANTS[inv.status] ?? "secondary"}>
                    {STATUS_LABELS[inv.status] ?? inv.status}
                  </Badge>
                </TableCell>
                <TableCell>{inv.issuedDate}</TableCell>
                <TableCell>{inv.dueDate}</TableCell>
                <TableCell className="text-right">{fmt(inv.totalAmount)}</TableCell>
                <TableCell className={`text-right font-medium ${inv.balanceDue > 0 ? "text-destructive" : ""}`}>
                  {fmt(inv.balanceDue)}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}
