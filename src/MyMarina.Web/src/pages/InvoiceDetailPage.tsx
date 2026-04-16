import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useParams } from "@tanstack/react-router";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState } from "react";
import { toast } from "sonner";
import { ArrowLeft, Plus, Pencil, Trash2, Send, Ban, DollarSign } from "lucide-react";
import { Link } from "@tanstack/react-router";
import {
  getInvoice, sendInvoice, voidInvoice,
  addLineItem, updateLineItem, removeLineItem, recordPayment,
  type InvoiceLineItemDto, type PaymentMethod,
} from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Controller } from "react-hook-form";
import { Separator } from "@/components/ui/separator";

// ── Constants ─────────────────────────────────────────────────────────────────

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

// ── Schemas ───────────────────────────────────────────────────────────────────

const lineItemSchema = z.object({
  description: z.string().min(1, "Required"),
  quantity: z.preprocess((v) => Number(v), z.number().positive("Must be > 0")),
  unitPrice: z.preprocess((v) => Number(v), z.number().positive("Must be > 0")),
});
type LineItemForm = z.infer<typeof lineItemSchema>;

const paymentSchema = z.object({
  amount: z.preprocess((v) => Number(v), z.number().positive("Must be > 0")),
  paidOn: z.string().min(1, "Required"),
  method: z.number(),
  referenceNumber: z.string().optional().nullable(),
  notes: z.string().optional().nullable(),
});
type PaymentForm = z.infer<typeof paymentSchema>;

// ── Main component ────────────────────────────────────────────────────────────

export function InvoiceDetailPage() {
  const { invoiceId } = useParams({ from: "/operator/invoices/$invoiceId" });
  const qc = useQueryClient();

  const [lineItemDialogOpen, setLineItemDialogOpen] = useState(false);
  const [editingLineItem, setEditingLineItem] = useState<InvoiceLineItemDto | null>(null);
  const [deletingLineItem, setDeletingLineItem] = useState<InvoiceLineItemDto | null>(null);
  const [paymentDialogOpen, setPaymentDialogOpen] = useState(false);
  const [voidConfirmOpen, setVoidConfirmOpen] = useState(false);
  const [sendConfirmOpen, setSendConfirmOpen] = useState(false);

  const { data: invoice, isLoading } = useQuery({
    queryKey: ["invoice", invoiceId],
    queryFn: () => getInvoice(invoiceId),
  });

  // Line item form
  const liForm = useForm<LineItemForm, any, LineItemForm>({ resolver: zodResolver(lineItemSchema) as any });

  const openAddLineItem = () => {
    setEditingLineItem(null);
    liForm.reset({ description: "", quantity: 1, unitPrice: undefined });
    setLineItemDialogOpen(true);
  };
  const openEditLineItem = (li: InvoiceLineItemDto) => {
    setEditingLineItem(li);
    liForm.reset({ description: li.description, quantity: Number(li.quantity), unitPrice: Number(li.unitPrice) });
    setLineItemDialogOpen(true);
  };

  const saveLineItemMut = useMutation({
    mutationFn: async (v: LineItemForm) => {
      if (editingLineItem) {
        await updateLineItem(invoiceId, editingLineItem.id, v);
      } else {
        await addLineItem(invoiceId, { ...v, slipAssignmentId: null });
      }
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["invoice", invoiceId] });
      qc.invalidateQueries({ queryKey: ["invoices"] });
      toast.success(editingLineItem ? "Line item updated" : "Line item added");
      setLineItemDialogOpen(false);
    },
    onError: () => toast.error("Failed to save line item"),
  });

  const removeLineItemMut = useMutation({
    mutationFn: (lineItemId: string) => removeLineItem(invoiceId, lineItemId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["invoice", invoiceId] });
      qc.invalidateQueries({ queryKey: ["invoices"] });
      toast.success("Line item removed");
      setDeletingLineItem(null);
    },
    onError: () => toast.error("Failed to remove line item"),
  });

  // Payment form
  const payForm = useForm<PaymentForm>({
    resolver: zodResolver(paymentSchema) as any,
    defaultValues: { paidOn: new Date().toISOString().slice(0, 10), method: 0 },
  });

  const recordPaymentMut = useMutation({
    mutationFn: (v: PaymentForm) => recordPayment(invoiceId, {
      amount: v.amount,
      paidOn: v.paidOn,
      method: v.method as PaymentMethod,
      referenceNumber: v.referenceNumber || null,
      notes: v.notes || null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["invoice", invoiceId] });
      qc.invalidateQueries({ queryKey: ["invoices"] });
      toast.success("Payment recorded");
      setPaymentDialogOpen(false);
    },
    onError: (err: any) => {
      const msg = err?.response?.data?.message ?? "Failed to record payment";
      toast.error(msg);
    },
  });

  // Status transitions
  const sendMut = useMutation({
    mutationFn: () => sendInvoice(invoiceId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["invoice", invoiceId] });
      qc.invalidateQueries({ queryKey: ["invoices"] });
      toast.success("Invoice sent");
      setSendConfirmOpen(false);
    },
    onError: () => toast.error("Failed to send invoice"),
  });

  const voidMut = useMutation({
    mutationFn: () => voidInvoice(invoiceId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["invoice", invoiceId] });
      qc.invalidateQueries({ queryKey: ["invoices"] });
      toast.success("Invoice voided");
      setVoidConfirmOpen(false);
    },
    onError: () => toast.error("Failed to void invoice"),
  });

  if (isLoading) return <div className="p-8 text-muted-foreground">Loading…</div>;
  if (!invoice) return <div className="p-8 text-muted-foreground">Invoice not found.</div>;

  const isDraft = invoice.status === 0;
  const canVoid = invoice.status !== 5 && invoice.status !== 3;  // can't void paid or already voided
  const canRecordPayment = invoice.status === 1 || invoice.status === 2 || invoice.status === 4; // Sent|PartiallyPaid|Overdue

  return (
    <div className="p-8 space-y-6 max-w-4xl">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Link to="/invoices">
          <Button variant="ghost" size="icon"><ArrowLeft className="h-4 w-4" /></Button>
        </Link>
        <div className="flex-1">
          <h1 className="text-2xl font-bold font-mono">{invoice.invoiceNumber}</h1>
          <Link to="/customers/$customerId" params={{ customerId: invoice.customerAccountId }} className="text-sm text-muted-foreground hover:underline">
            {invoice.customerDisplayName}
          </Link>
        </div>
        <Badge variant={STATUS_VARIANTS[invoice.status] ?? "secondary"}>
          {STATUS_LABELS[invoice.status] ?? invoice.status}
        </Badge>

        {/* Action buttons */}
        <div className="flex gap-2">
          {isDraft && (
            <Button onClick={() => setSendConfirmOpen(true)}>
              <Send className="h-4 w-4" /> Send
            </Button>
          )}
          {canRecordPayment && (
            <Button variant="outline" onClick={() => { payForm.reset({ paidOn: new Date().toISOString().slice(0, 10), method: 0 }); setPaymentDialogOpen(true); }}>
              <DollarSign className="h-4 w-4" /> Record Payment
            </Button>
          )}
          {canVoid && (
            <Button variant="outline" className="text-destructive hover:text-destructive" onClick={() => setVoidConfirmOpen(true)}>
              <Ban className="h-4 w-4" /> Void
            </Button>
          )}
        </div>
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

      {/* Dates + notes */}
      <Card>
        <CardContent className="pt-4 grid grid-cols-3 gap-4 text-sm">
          <div><span className="text-muted-foreground block">Issue Date</span>{invoice.issuedDate}</div>
          <div><span className="text-muted-foreground block">Due Date</span>{invoice.dueDate}</div>
          {invoice.notes && <div className="col-span-3"><span className="text-muted-foreground block">Notes</span>{invoice.notes}</div>}
        </CardContent>
      </Card>

      {/* Line Items */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Line Items</h2>
          {isDraft && (
            <Button size="sm" onClick={openAddLineItem}><Plus className="h-4 w-4" /> Add Item</Button>
          )}
        </div>
        {invoice.lineItems.length === 0 ? (
          <p className="text-sm text-muted-foreground">No line items yet.{isDraft ? " Add one above." : ""}</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Description</TableHead>
                <TableHead className="text-right">Qty</TableHead>
                <TableHead className="text-right">Unit Price</TableHead>
                <TableHead className="text-right">Total</TableHead>
                {isDraft && <TableHead className="w-20"></TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {invoice.lineItems.map((li) => (
                <TableRow key={li.id}>
                  <TableCell>{li.description}</TableCell>
                  <TableCell className="text-right">{Number(li.quantity)}</TableCell>
                  <TableCell className="text-right">{fmt(li.unitPrice)}</TableCell>
                  <TableCell className="text-right font-medium">{fmt(li.lineTotal)}</TableCell>
                  {isDraft && (
                    <TableCell>
                      <div className="flex gap-1 justify-end">
                        <Button size="icon" variant="ghost" onClick={() => openEditLineItem(li)}><Pencil className="h-3 w-3" /></Button>
                        <Button size="icon" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setDeletingLineItem(li)}><Trash2 className="h-3 w-3" /></Button>
                      </div>
                    </TableCell>
                  )}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </div>

      <Separator />

      {/* Payment History */}
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
                <TableHead>Notes</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {invoice.payments.map((p) => (
                <TableRow key={p.id}>
                  <TableCell>{p.paidOn}</TableCell>
                  <TableCell>{PAYMENT_METHOD_LABELS[p.method] ?? p.method}</TableCell>
                  <TableCell className="text-muted-foreground">{p.referenceNumber ?? "—"}</TableCell>
                  <TableCell className="text-right font-medium text-green-600">{fmt(p.amount)}</TableCell>
                  <TableCell className="text-muted-foreground">{p.notes ?? "—"}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </div>

      {/* Line Item Dialog */}
      <Dialog open={lineItemDialogOpen} onOpenChange={(v) => { if (!v) setLineItemDialogOpen(false); }}>
        <DialogContent>
          <DialogHeader><DialogTitle>{editingLineItem ? "Edit Line Item" : "Add Line Item"}</DialogTitle></DialogHeader>
          <form onSubmit={liForm.handleSubmit((v) => saveLineItemMut.mutate(v))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Description</Label>
              <Input {...liForm.register("description")} placeholder="e.g. Monthly slip fee – Slip B-01" />
              {liForm.formState.errors.description && <p className="text-xs text-destructive">{liForm.formState.errors.description.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>Quantity</Label>
                <Input type="number" step="0.01" min="0.01" {...liForm.register("quantity")} />
                {liForm.formState.errors.quantity && <p className="text-xs text-destructive">{liForm.formState.errors.quantity.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Unit Price</Label>
                <Input type="number" step="0.01" min="0.01" {...liForm.register("unitPrice")} />
                {liForm.formState.errors.unitPrice && <p className="text-xs text-destructive">{liForm.formState.errors.unitPrice.message}</p>}
              </div>
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setLineItemDialogOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={liForm.formState.isSubmitting}>
                {liForm.formState.isSubmitting ? "Saving…" : "Save"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Remove Line Item Confirm */}
      <AlertDialog open={!!deletingLineItem} onOpenChange={(v) => { if (!v) setDeletingLineItem(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove Line Item</AlertDialogTitle>
            <AlertDialogDescription>
              Remove <strong>{deletingLineItem?.description}</strong>? Totals will be recalculated.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => deletingLineItem && removeLineItemMut.mutate(deletingLineItem.id)}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Remove
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Record Payment Dialog */}
      <Dialog open={paymentDialogOpen} onOpenChange={(v) => { if (!v) setPaymentDialogOpen(false); }}>
        <DialogContent>
          <DialogHeader><DialogTitle>Record Payment</DialogTitle></DialogHeader>
          <p className="text-sm text-muted-foreground">Balance due: <span className="font-medium text-foreground">{fmt(invoice.balanceDue)}</span></p>
          <form onSubmit={payForm.handleSubmit((v) => recordPaymentMut.mutate(v))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Amount</Label>
              <Input type="number" step="0.01" min="0.01" {...payForm.register("amount")} />
              {payForm.formState.errors.amount && <p className="text-xs text-destructive">{payForm.formState.errors.amount.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>Date</Label>
                <Input type="date" {...payForm.register("paidOn")} />
              </div>
              <div className="space-y-1.5">
                <Label>Method</Label>
                <Controller control={payForm.control} name="method" render={({ field }) => (
                  <Select value={String(field.value)} onValueChange={(v) => field.onChange(Number(v))}>
                    <SelectTrigger><SelectValue /></SelectTrigger>
                    <SelectContent>
                      {Object.entries(PAYMENT_METHOD_LABELS).map(([k, v]) => (
                        <SelectItem key={k} value={k}>{v}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )} />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Reference # (check, transaction ID, etc.)</Label>
              <Input {...payForm.register("referenceNumber")} placeholder="Optional" />
            </div>
            <div className="space-y-1.5">
              <Label>Notes</Label>
              <Input {...payForm.register("notes")} placeholder="Optional" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setPaymentDialogOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={payForm.formState.isSubmitting}>
                {payForm.formState.isSubmitting ? "Saving…" : "Record Payment"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Send Confirm */}
      <AlertDialog open={sendConfirmOpen} onOpenChange={setSendConfirmOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Send Invoice?</AlertDialogTitle>
            <AlertDialogDescription>
              This will mark {invoice.invoiceNumber} as Sent. You won't be able to edit line items after sending.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={() => sendMut.mutate()}>Send</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Void Confirm */}
      <AlertDialog open={voidConfirmOpen} onOpenChange={setVoidConfirmOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Void Invoice?</AlertDialogTitle>
            <AlertDialogDescription>
              Void {invoice.invoiceNumber}? This cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => voidMut.mutate()}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Void Invoice
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
