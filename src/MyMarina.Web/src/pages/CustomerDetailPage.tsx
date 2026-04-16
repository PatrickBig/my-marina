import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useParams } from "@tanstack/react-router";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState, useEffect } from "react";
import { toast } from "sonner";
import { Plus, Pencil, Trash2, ArrowLeft, FileText } from "lucide-react";
import { Link } from "@tanstack/react-router";
import {
  getCustomer, updateCustomer, getBoats, createBoat, updateBoat, deleteBoat,
  getInvoices,
  type BoatDto, type InvoiceDto,
} from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Controller } from "react-hook-form";

const BoatType = { 0: "Sailboat", 1: "Powerboat", 2: "Catamaran", 3: "Kayak", 4: "Other" } as const;

const INV_STATUS_LABELS: Record<number, string> = {
  0: "Draft", 1: "Sent", 2: "Part. Paid", 3: "Paid", 4: "Overdue", 5: "Voided",
};
const INV_STATUS_VARIANTS: Record<number, "secondary" | "default" | "warning" | "success" | "destructive"> = {
  0: "secondary", 1: "default", 2: "warning", 3: "success", 4: "destructive", 5: "secondary",
};

function fmtCurrency(n: number) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(n);
}

function CustomerInvoiceRow({ inv }: { inv: InvoiceDto }) {
  return (
    <TableRow>
      <TableCell>
        <Link to="/invoices/$invoiceId" params={{ invoiceId: inv.id }} className="font-mono font-medium hover:underline text-primary">
          {inv.invoiceNumber}
        </Link>
      </TableCell>
      <TableCell className="text-muted-foreground">{inv.issuedDate}</TableCell>
      <TableCell className="text-muted-foreground">{inv.dueDate}</TableCell>
      <TableCell>{fmtCurrency(inv.totalAmount)}</TableCell>
      <TableCell className={inv.balanceDue > 0 && inv.status !== 5 ? "font-medium" : "text-muted-foreground"}>
        {fmtCurrency(inv.balanceDue)}
      </TableCell>
      <TableCell>
        <Badge variant={INV_STATUS_VARIANTS[inv.status] ?? "secondary"}>
          {INV_STATUS_LABELS[inv.status] ?? inv.status}
        </Badge>
      </TableCell>
    </TableRow>
  );
}

const customerSchema = z.object({
  displayName: z.string().min(1, "Required"),
  billingEmail: z.string().email("Invalid email"),
  billingPhone: z.string().optional().nullable(),
  emergencyContactName: z.string().optional().nullable(),
  emergencyContactPhone: z.string().optional().nullable(),
  notes: z.string().optional().nullable(),
});
type CustomerForm = z.infer<typeof customerSchema>;

const boatSchema = z.object({
  name: z.string().min(1, "Required"),
  make: z.string().optional().nullable(),
  model: z.string().optional().nullable(),
  year: z.preprocess((v) => (v === "" || v === null || v === undefined ? null : Number(v)), z.number().int().nullable()),
  length: z.preprocess((v) => Number(v), z.number().positive("Required")),
  beam: z.preprocess((v) => Number(v), z.number().positive("Required")),
  draft: z.preprocess((v) => Number(v), z.number().positive("Required")),
  boatType: z.number(),
  hullColor: z.string().optional().nullable(),
  registrationNumber: z.string().optional().nullable(),
  registrationState: z.string().optional().nullable(),
  insuranceProvider: z.string().optional().nullable(),
  insurancePolicyNumber: z.string().optional().nullable(),
  insuranceExpiresOn: z.string().optional().nullable(),
});
type BoatForm = z.infer<typeof boatSchema>;

export function CustomerDetailPage() {
  const { customerId } = useParams({ from: "/operator/customers/$customerId" });
  const qc = useQueryClient();
  const [boatDialogOpen, setBoatDialogOpen] = useState(false);
  const [editingBoat, setEditingBoat] = useState<BoatDto | null>(null);
  const [deletingBoat, setDeletingBoat] = useState<BoatDto | null>(null);
  const [editCustomerOpen, setEditCustomerOpen] = useState(false);

  const { data: customer, isLoading: custLoading } = useQuery({
    queryKey: ["customer", customerId],
    queryFn: () => getCustomer(customerId),
  });

  const { data: boats = [], isLoading: boatsLoading } = useQuery({
    queryKey: ["boats", customerId],
    queryFn: () => getBoats(customerId),
    enabled: !!customerId,
  });

  const { data: invoices = [], isLoading: invoicesLoading } = useQuery({
    queryKey: ["invoices", "customer", customerId],
    queryFn: () => getInvoices({ customerAccountId: customerId }),
    enabled: !!customerId,
  });

  // Customer edit form
  const custForm = useForm<CustomerForm>({ resolver: zodResolver(customerSchema) });
  useEffect(() => {
    if (customer) {
      custForm.reset({
        displayName: customer.displayName, billingEmail: customer.billingEmail,
        billingPhone: customer.billingPhone ?? "", emergencyContactName: customer.emergencyContactName ?? "",
        emergencyContactPhone: customer.emergencyContactPhone ?? "", notes: customer.notes ?? "",
      });
    }
  }, [customer]);

  const updateCustMut = useMutation({
    mutationFn: (v: CustomerForm) => updateCustomer(customerId, {
      displayName: v.displayName, billingEmail: v.billingEmail,
      billingPhone: v.billingPhone || null, billingAddress: null,
      emergencyContactName: v.emergencyContactName || null,
      emergencyContactPhone: v.emergencyContactPhone || null,
      notes: v.notes || null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["customer", customerId] });
      qc.invalidateQueries({ queryKey: ["customers"] });
      toast.success("Customer updated");
      setEditCustomerOpen(false);
    },
    onError: () => toast.error("Failed to update customer"),
  });

  // Boat form
  const boatForm = useForm<BoatForm, any, BoatForm>({ resolver: zodResolver(boatSchema) as any });
  const openCreateBoat = () => {
    setEditingBoat(null);
    boatForm.reset({ name: "", make: "", model: "", year: null, length: undefined, beam: undefined, draft: undefined, boatType: 0, hullColor: "", registrationNumber: "", registrationState: "", insuranceProvider: "", insurancePolicyNumber: "", insuranceExpiresOn: null });
    setBoatDialogOpen(true);
  };
  const openEditBoat = (b: BoatDto) => {
    setEditingBoat(b);
    boatForm.reset({ ...b, make: b.make ?? "", model: b.model ?? "", year: b.year ? Number(b.year) : null, length: Number(b.length), beam: Number(b.beam), draft: Number(b.draft), hullColor: b.hullColor ?? "", registrationNumber: b.registrationNumber ?? "", registrationState: b.registrationState ?? "", insuranceProvider: b.insuranceProvider ?? "", insurancePolicyNumber: b.insurancePolicyNumber ?? "", insuranceExpiresOn: b.insuranceExpiresOn ?? null });
    setBoatDialogOpen(true);
  };

  const saveBoatMut = useMutation({
    mutationFn: async (v: BoatForm) => {
      const payload = { ...v, make: v.make || null, model: v.model || null, year: v.year ?? null, hullColor: v.hullColor || null, registrationNumber: v.registrationNumber || null, registrationState: v.registrationState || null, insuranceProvider: v.insuranceProvider || null, insurancePolicyNumber: v.insurancePolicyNumber || null, insuranceExpiresOn: v.insuranceExpiresOn || null };
      if (editingBoat) { await updateBoat(editingBoat.id, payload as any); } else { await createBoat(customerId, payload as any); }
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["boats", customerId] });
      toast.success(editingBoat ? "Boat updated" : "Boat added");
      setBoatDialogOpen(false);
    },
    onError: () => toast.error("Failed to save boat"),
  });

  const deleteBoatMut = useMutation({
    mutationFn: (id: string) => deleteBoat(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["boats", customerId] });
      toast.success("Boat deleted");
      setDeletingBoat(null);
    },
    onError: () => toast.error("Failed to delete boat"),
  });

  if (custLoading) return <div className="p-8 text-muted-foreground">Loading…</div>;
  if (!customer) return <div className="p-8 text-muted-foreground">Customer not found.</div>;

  return (
    <div className="p-8 space-y-6 max-w-3xl">
      <div className="flex items-center gap-3">
        <Link to="/customers">
          <Button variant="ghost" size="icon"><ArrowLeft className="h-4 w-4" /></Button>
        </Link>
        <div className="flex-1">
          <h1 className="text-2xl font-bold">{customer.displayName}</h1>
          <p className="text-muted-foreground text-sm">{customer.billingEmail}</p>
        </div>
        <Badge variant={customer.isActive ? "success" : "secondary"}>{customer.isActive ? "Active" : "Inactive"}</Badge>
        <Button variant="outline" onClick={() => setEditCustomerOpen(true)}><Pencil className="h-4 w-4" /> Edit</Button>
      </div>

      {/* Info card */}
      <Card>
        <CardHeader><CardTitle>Contact Info</CardTitle></CardHeader>
        <CardContent className="grid grid-cols-2 gap-4 text-sm">
          <div><span className="text-muted-foreground">Phone</span><p>{customer.billingPhone ?? "—"}</p></div>
          <div><span className="text-muted-foreground">Emergency Contact</span><p>{customer.emergencyContactName ?? "—"}</p></div>
          <div><span className="text-muted-foreground">Emergency Phone</span><p>{customer.emergencyContactPhone ?? "—"}</p></div>
          {customer.notes && <div className="col-span-2"><span className="text-muted-foreground">Notes</span><p>{customer.notes}</p></div>}
        </CardContent>
      </Card>

      {/* Boats */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Boats</h2>
          <Button size="sm" onClick={openCreateBoat}><Plus className="h-4 w-4" /> Add Boat</Button>
        </div>
        {boatsLoading ? <p className="text-muted-foreground">Loading…</p> : boats.length === 0 ? (
          <p className="text-muted-foreground text-sm">No boats registered.</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Make / Model</TableHead>
                <TableHead>Type</TableHead>
                <TableHead>L / B / D</TableHead>
                <TableHead className="w-24"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {boats.map((b) => (
                <TableRow key={b.id}>
                  <TableCell className="font-medium">{b.name}</TableCell>
                  <TableCell className="text-muted-foreground">{[b.make, b.model, b.year ? String(b.year) : null].filter(Boolean).join(" ") || "—"}</TableCell>
                  <TableCell>{BoatType[b.boatType as keyof typeof BoatType] ?? b.boatType}</TableCell>
                  <TableCell>{Number(b.length)}′ / {Number(b.beam)}′ / {Number(b.draft)}′</TableCell>
                  <TableCell>
                    <div className="flex gap-1">
                      <Button size="icon" variant="ghost" onClick={() => openEditBoat(b)}><Pencil className="h-4 w-4" /></Button>
                      <Button size="icon" variant="ghost" onClick={() => setDeletingBoat(b)} className="text-destructive hover:text-destructive"><Trash2 className="h-4 w-4" /></Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </div>

      {/* Billing History */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Billing History</h2>
          <Link to="/invoices" search={{ customerAccountId: customerId } as any}>
            <Button variant="outline" size="sm"><FileText className="h-4 w-4" /> All Invoices</Button>
          </Link>
        </div>
        {invoicesLoading ? <p className="text-muted-foreground">Loading…</p> : invoices.length === 0 ? (
          <p className="text-muted-foreground text-sm">No invoices on file.</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Invoice #</TableHead>
                <TableHead>Issued</TableHead>
                <TableHead>Due</TableHead>
                <TableHead>Total</TableHead>
                <TableHead>Balance</TableHead>
                <TableHead>Status</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {invoices.slice(0, 10).map((inv) => (
                <CustomerInvoiceRow key={inv.id} inv={inv} />
              ))}
            </TableBody>
          </Table>
        )}
      </div>

      {/* Edit Customer Dialog */}
      <Dialog open={editCustomerOpen} onOpenChange={setEditCustomerOpen}>
        <DialogContent>
          <DialogHeader><DialogTitle>Edit Customer</DialogTitle></DialogHeader>
          <form onSubmit={custForm.handleSubmit((v) => updateCustMut.mutate(v))} className="space-y-4">
            <div className="space-y-1.5"><Label>Display Name</Label><Input {...custForm.register("displayName")} /></div>
            <div className="space-y-1.5"><Label>Billing Email</Label><Input type="email" {...custForm.register("billingEmail")} /></div>
            <div className="space-y-1.5"><Label>Phone</Label><Input {...custForm.register("billingPhone")} /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5"><Label>Emergency Contact</Label><Input {...custForm.register("emergencyContactName")} /></div>
              <div className="space-y-1.5"><Label>Emergency Phone</Label><Input {...custForm.register("emergencyContactPhone")} /></div>
            </div>
            <div className="space-y-1.5"><Label>Notes</Label><Input {...custForm.register("notes")} /></div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setEditCustomerOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={custForm.formState.isSubmitting}>Save</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Boat Dialog */}
      <Dialog open={boatDialogOpen} onOpenChange={(v) => { if (!v) setBoatDialogOpen(false); }}>
        <DialogContent className="max-w-xl max-h-[90vh] overflow-y-auto">
          <DialogHeader><DialogTitle>{editingBoat ? "Edit Boat" : "Add Boat"}</DialogTitle></DialogHeader>
          <form onSubmit={boatForm.handleSubmit((v) => saveBoatMut.mutate(v))} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5"><Label>Name</Label><Input {...boatForm.register("name")} /></div>
              <div className="space-y-1.5">
                <Label>Type</Label>
                <Controller control={boatForm.control} name="boatType" render={({ field }) => (
                  <Select value={String(field.value)} onValueChange={(v) => field.onChange(Number(v))}>
                    <SelectTrigger><SelectValue /></SelectTrigger>
                    <SelectContent>
                      {Object.entries(BoatType).map(([k, v]) => <SelectItem key={k} value={k}>{v}</SelectItem>)}
                    </SelectContent>
                  </Select>
                )} />
              </div>
              <div className="space-y-1.5"><Label>Make</Label><Input {...boatForm.register("make")} /></div>
              <div className="space-y-1.5"><Label>Model</Label><Input {...boatForm.register("model")} /></div>
              <div className="space-y-1.5"><Label>Year</Label><Input type="number" {...boatForm.register("year")} /></div>
              <div className="space-y-1.5"><Label>Hull Color</Label><Input {...boatForm.register("hullColor")} /></div>
            </div>
            <div className="grid grid-cols-3 gap-4">
              <div className="space-y-1.5"><Label>Length (ft)</Label><Input type="number" step="0.1" {...boatForm.register("length")} /></div>
              <div className="space-y-1.5"><Label>Beam (ft)</Label><Input type="number" step="0.1" {...boatForm.register("beam")} /></div>
              <div className="space-y-1.5"><Label>Draft (ft)</Label><Input type="number" step="0.1" {...boatForm.register("draft")} /></div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5"><Label>Reg. Number</Label><Input {...boatForm.register("registrationNumber")} /></div>
              <div className="space-y-1.5"><Label>Reg. State</Label><Input {...boatForm.register("registrationState")} /></div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5"><Label>Insurance Provider</Label><Input {...boatForm.register("insuranceProvider")} /></div>
              <div className="space-y-1.5"><Label>Policy Number</Label><Input {...boatForm.register("insurancePolicyNumber")} /></div>
            </div>
            <div className="space-y-1.5"><Label>Insurance Expires</Label><Input type="date" {...boatForm.register("insuranceExpiresOn")} /></div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setBoatDialogOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={boatForm.formState.isSubmitting}>{boatForm.formState.isSubmitting ? "Saving…" : "Save"}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Delete Boat Confirm */}
      <AlertDialog open={!!deletingBoat} onOpenChange={(v) => { if (!v) setDeletingBoat(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Boat</AlertDialogTitle>
            <AlertDialogDescription>Delete <strong>{deletingBoat?.name}</strong>? This cannot be undone.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={() => deletingBoat && deleteBoatMut.mutate(deletingBoat.id)} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">Delete</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
