import { useQuery } from "@tanstack/react-query";
import { useParams } from "@tanstack/react-router";
import { ArrowLeft } from "lucide-react";
import { Link } from "@tanstack/react-router";
import { getPortalInvoice } from "@/api/api";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Separator } from "@/components/ui/separator";

const STATUS_LABELS: Record<number, string> = {
  0: "Draft", 1: "Sent", 2: "Partially Paid", 3: "Paid", 4: "Overdue", 5: "Voided",
};
const STATUS_VARIANTS: Record<number, "secondary" | "default" | "warning" | "success" | "destructive"> = {
  0: "secondary", 1: "default", 2: "warning", 3: "success", 4: "destructive", 5: "secondary",
};
const PAYMENT_METHOD_LABELS: Record<number, string> = {
  0: "Cash", 1: "Check", 2: "Credit Card", 3: "Bank Transfer", 4: "Other",
};

function fmt(n: number) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(n);
}

export function PortalInvoiceDetailPage() {
  const { invoiceId } = useParams({ from: "/portal/invoices/$invoiceId" });

  const { data: invoice, isLoading } = useQuery({
    queryKey: ["portal-invoice", invoiceId],
    queryFn: () => getPortalInvoice(invoiceId),
  });

  if (isLoading) return <div className="p-8 text-muted-foreground">Loading…</div>;
  if (!invoice) return <div className="p-8 text-muted-foreground">Invoice not found.</div>;

  return (
    <div className="p-8 space-y-6 max-w-3xl">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Link to="/portal/invoices">
          <Button variant="ghost" size="icon"><ArrowLeft className="h-4 w-4" /></Button>
        </Link>
        <div className="flex-1">
          <h1 className="text-2xl font-bold font-mono">{invoice.invoiceNumber}</h1>
          <p className="text-sm text-muted-foreground">Issued {invoice.issuedDate} · Due {invoice.dueDate}</p>
        </div>
        <Badge variant={STATUS_VARIANTS[invoice.status] ?? "secondary"}>
          {STATUS_LABELS[invoice.status] ?? invoice.status}
        </Badge>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-4 gap-4">
        <Card>
          <CardContent className="pt-4">
            <p className="text-xs text-muted-foreground">Subtotal</p>
            <p className="text-lg font-semibold">{fmt(invoice.subTotal)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4">
            <p className="text-xs text-muted-foreground">Total</p>
            <p className="text-lg font-semibold">{fmt(invoice.totalAmount)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4">
            <p className="text-xs text-muted-foreground">Amount Paid</p>
            <p className="text-lg font-semibold text-green-600">{fmt(invoice.amountPaid)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4">
            <p className="text-xs text-muted-foreground">Balance Due</p>
            <p className={`text-lg font-semibold ${invoice.balanceDue > 0 ? "text-destructive" : ""}`}>
              {fmt(invoice.balanceDue)}
            </p>
          </CardContent>
        </Card>
      </div>

      {invoice.notes && (
        <Card>
          <CardContent className="pt-4">
            <span className="text-muted-foreground text-sm block">Notes</span>
            <p className="text-sm">{invoice.notes}</p>
          </CardContent>
        </Card>
      )}

      {/* Line Items */}
      <div className="space-y-3">
        <h2 className="text-lg font-semibold">Line Items</h2>
        {invoice.lineItems.length === 0 ? (
          <p className="text-sm text-muted-foreground">No line items.</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Description</TableHead>
                <TableHead className="text-right">Qty</TableHead>
                <TableHead className="text-right">Unit Price</TableHead>
                <TableHead className="text-right">Total</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {invoice.lineItems.map((li, i) => (
                <TableRow key={i}>
                  <TableCell>{li.description}</TableCell>
                  <TableCell className="text-right">{Number(li.quantity)}</TableCell>
                  <TableCell className="text-right">{fmt(li.unitPrice)}</TableCell>
                  <TableCell className="text-right font-medium">{fmt(li.lineTotal)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </div>

      <Separator />

      {/* Payments */}
      <div className="space-y-3">
        <h2 className="text-lg font-semibold">Payment History</h2>
        {invoice.payments.length === 0 ? (
          <p className="text-sm text-muted-foreground">No payments recorded.</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Date</TableHead>
                <TableHead>Method</TableHead>
                <TableHead>Reference</TableHead>
                <TableHead className="text-right">Amount</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {invoice.payments.map((p, i) => (
                <TableRow key={i}>
                  <TableCell>{p.paidOn}</TableCell>
                  <TableCell>{PAYMENT_METHOD_LABELS[p.method] ?? p.method}</TableCell>
                  <TableCell className="text-muted-foreground">{p.referenceNumber ?? "—"}</TableCell>
                  <TableCell className="text-right font-medium text-green-600">{fmt(p.amount)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </div>
    </div>
  );
}
