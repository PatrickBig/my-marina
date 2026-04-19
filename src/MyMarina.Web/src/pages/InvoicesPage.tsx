import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState } from "react";
import { toast } from "sonner";
import { Plus, FileText, ArrowUp, ArrowDown, ArrowUpDown } from "lucide-react";
import { Link, useSearch, useNavigate } from "@tanstack/react-router";
import {
  getInvoices, createInvoice, getCustomers, getMarinas,
  type InvoiceDto, type InvoiceStatus,
} from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

const STATUS_LABELS: Record<number, string> = {
  0: "Draft", 1: "Sent", 2: "Partially Paid", 3: "Paid", 4: "Overdue", 5: "Voided",
};

const STATUS_VARIANTS: Record<number, "secondary" | "default" | "warning" | "success" | "destructive"> = {
  0: "secondary", 1: "default", 2: "warning", 3: "success", 4: "destructive", 5: "secondary",
};

function StatusBadge({ status }: { status: InvoiceStatus }) {
  return (
    <Badge variant={STATUS_VARIANTS[status] ?? "secondary"}>
      {STATUS_LABELS[status] ?? status}
    </Badge>
  );
}

function fmt(amount: number | string) {
  const num = typeof amount === "string" ? parseFloat(amount) : amount;
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(num);
}

const createSchema = z.object({
  customerAccountId: z.string().min(1, "Customer is required"),
  issuedDate: z.string().min(1, "Required"),
  dueDate: z.string().min(1, "Required"),
  notes: z.string().optional().nullable(),
});
type CreateForm = z.infer<typeof createSchema>;

type SortField = "issuedDate" | "dueDate";

function SortHeader({
  label, field, current, descending, onSort,
}: {
  label: string;
  field: SortField;
  current: string;
  descending: boolean;
  onSort: (field: SortField, desc: boolean) => void;
}) {
  const active = current === field;
  return (
    <button
      className="flex items-center gap-1 hover:text-foreground transition-colors"
      onClick={() => onSort(field, active ? !descending : field === "dueDate" ? false : true)}
    >
      {label}
      {active
        ? descending
          ? <ArrowDown className="h-3 w-3" />
          : <ArrowUp className="h-3 w-3" />
        : <ArrowUpDown className="h-3 w-3 opacity-40" />}
    </button>
  );
}

export function InvoicesPage() {
  const qc = useQueryClient();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);

  const rawSearch = useSearch({ strict: false }) as {
    status?: number; marinaId?: string; sortBy?: string; sortDescending?: boolean;
  };
  const status = rawSearch.status;
  const marinaId = rawSearch.marinaId;
  const sortBy = rawSearch.sortBy ?? "issuedDate";
  const sortDescending = rawSearch.sortDescending ?? true;

  const { data: invoices = [], isLoading } = useQuery({
    queryKey: ["invoices", status, marinaId, sortBy, sortDescending],
    queryFn: () => getInvoices({
      status: status as InvoiceStatus | undefined,
      marinaId,
      sortBy,
      sortDescending,
    }),
  });

  const { data: customers = [] } = useQuery({
    queryKey: ["customers"],
    queryFn: getCustomers,
  });

  const { data: marinas = [] } = useQuery({
    queryKey: ["marinas"],
    queryFn: getMarinas,
  });

  const marinaMap = Object.fromEntries(marinas.map((m) => [m.id, m.name]));
  const showMarinaColumn = !marinaId && marinas.length > 1;

  function setSearch(patch: { status?: number | undefined; marinaId?: string | undefined; sortBy?: string; sortDescending?: boolean }) {
    navigate({
      to: "/invoices",
      search: {
        status: "status" in patch ? patch.status : status,
        marinaId: "marinaId" in patch ? patch.marinaId : marinaId,
        sortBy: patch.sortBy ?? sortBy,
        sortDescending: patch.sortDescending ?? sortDescending,
      },
    });
  }

  function handleSort(field: SortField, desc: boolean) {
    setSearch({ sortBy: field, sortDescending: desc });
  }

  const { register, handleSubmit, reset, control, formState: { errors, isSubmitting } } = useForm<CreateForm>({
    resolver: zodResolver(createSchema),
    defaultValues: { issuedDate: new Date().toISOString().slice(0, 10), dueDate: "" },
  });

  const createMut = useMutation({
    mutationFn: (v: CreateForm) => createInvoice({
      customerAccountId: v.customerAccountId,
      issuedDate: v.issuedDate,
      dueDate: v.dueDate,
      notes: v.notes || null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["invoices"] });
      toast.success("Invoice created");
      setOpen(false);
      reset();
    },
    onError: () => toast.error("Failed to create invoice"),
  });

  const activeCustomers = customers.filter((c) => c.isActive);

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Invoices</h1>
        <Button onClick={() => { reset({ issuedDate: new Date().toISOString().slice(0, 10), dueDate: "" }); setOpen(true); }}>
          <Plus className="h-4 w-4" /> New Invoice
        </Button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="flex items-center gap-2">
          <Label className="text-sm text-muted-foreground">Status</Label>
          <Select
            value={status !== undefined ? String(status) : "all"}
            onValueChange={(v) => setSearch({ status: v === "all" ? undefined : Number(v) })}
          >
            <SelectTrigger className="w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Statuses</SelectItem>
              {Object.entries(STATUS_LABELS).map(([k, v]) => (
                <SelectItem key={k} value={k}>{v}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {marinas.length > 1 && (
          <div className="flex items-center gap-2">
            <Label className="text-sm text-muted-foreground">Marina</Label>
            <Select
              value={marinaId ?? "all"}
              onValueChange={(v) => setSearch({ marinaId: v === "all" ? undefined : v })}
            >
              <SelectTrigger className="w-52">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Marinas</SelectItem>
                {marinas.map((m) => (
                  <SelectItem key={m.id} value={m.id}>{m.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        )}

        {(status !== undefined || marinaId) && (
          <Button
            variant="ghost"
            size="sm"
            className="text-muted-foreground"
            onClick={() => navigate({ to: "/invoices", search: { status: undefined, marinaId: undefined, sortBy: "issuedDate", sortDescending: true } })}
          >
            Clear filters
          </Button>
        )}
      </div>

      {isLoading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : invoices.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-muted-foreground gap-3">
          <FileText className="h-10 w-10 opacity-30" />
          <p>No invoices found.</p>
        </div>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Invoice #</TableHead>
              <TableHead>Customer</TableHead>
              {showMarinaColumn && <TableHead>Marina</TableHead>}
              <TableHead>
                <SortHeader label="Issued" field="issuedDate" current={sortBy} descending={sortDescending} onSort={handleSort} />
              </TableHead>
              <TableHead>
                <SortHeader label="Due" field="dueDate" current={sortBy} descending={sortDescending} onSort={handleSort} />
              </TableHead>
              <TableHead>Total</TableHead>
              <TableHead>Balance Due</TableHead>
              <TableHead>Status</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {invoices.map((inv) => (
              <InvoiceRow key={inv.id} inv={inv} marinaName={showMarinaColumn ? marinaMap[inv.marinaId] : undefined} />
            ))}
          </TableBody>
        </Table>
      )}

      {/* Create Invoice Dialog */}
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader><DialogTitle>New Invoice</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit((v) => createMut.mutate(v))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Customer</Label>
              <Controller control={control} name="customerAccountId" render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select customer…" />
                  </SelectTrigger>
                  <SelectContent>
                    {activeCustomers.map((c) => (
                      <SelectItem key={c.id} value={c.id}>{c.displayName}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )} />
              {errors.customerAccountId && <p className="text-xs text-destructive">{errors.customerAccountId.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>Issue Date</Label>
                <Input type="date" {...register("issuedDate")} />
                {errors.issuedDate && <p className="text-xs text-destructive">{errors.issuedDate.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Due Date</Label>
                <Input type="date" {...register("dueDate")} />
                {errors.dueDate && <p className="text-xs text-destructive">{errors.dueDate.message}</p>}
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Notes (optional)</Label>
              <Input {...register("notes")} placeholder="Internal notes…" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={isSubmitting}>{isSubmitting ? "Creating…" : "Create Invoice"}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function InvoiceRow({ inv, marinaName }: { inv: InvoiceDto; marinaName?: string }) {
  const balanceDue = typeof inv.balanceDue === "string" ? parseFloat(inv.balanceDue) : inv.balanceDue;
  return (
    <TableRow>
      <TableCell>
        <Link
          to="/invoices/$invoiceId"
          params={{ invoiceId: inv.id }}
          className="font-mono font-medium hover:underline text-primary"
        >
          {inv.invoiceNumber}
        </Link>
      </TableCell>
      <TableCell>
        <Link to="/customers/$customerId" params={{ customerId: inv.customerAccountId }} className="hover:underline">
          {inv.customerDisplayName}
        </Link>
      </TableCell>
      {marinaName !== undefined && (
        <TableCell className="text-muted-foreground text-sm">{marinaName}</TableCell>
      )}
      <TableCell className="text-muted-foreground">{inv.issuedDate}</TableCell>
      <TableCell className="text-muted-foreground">{inv.dueDate}</TableCell>
      <TableCell>{fmt(inv.totalAmount)}</TableCell>
      <TableCell className={balanceDue > 0 && inv.status !== 5 ? "font-medium" : "text-muted-foreground"}>
        {fmt(balanceDue)}
      </TableCell>
      <TableCell><StatusBadge status={inv.status} /></TableCell>
    </TableRow>
  );
}
