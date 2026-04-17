import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState } from "react";
import { toast } from "sonner";
import { Plus, Search, UserX, Mail } from "lucide-react";
import { Link } from "@tanstack/react-router";
import { getCustomers, createCustomer, deactivateCustomer, inviteCustomer } from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";

const schema = z.object({
  displayName: z.string().min(1, "Name is required"),
  billingEmail: z.string().email("Invalid email"),
  billingPhone: z.string().optional().nullable(),
  emergencyContactName: z.string().optional().nullable(),
  emergencyContactPhone: z.string().optional().nullable(),
  notes: z.string().optional().nullable(),
});
type FormValues = z.infer<typeof schema>;


export function CustomersPage() {
  const qc = useQueryClient();
  const [open, setOpen] = useState(false);
  const [inviteOpen, setInviteOpen] = useState(false);
  const [selectedCustomerId, setSelectedCustomerId] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [showInactive, setShowInactive] = useState(false);

  const { data: customers = [], isLoading } = useQuery({
    queryKey: ["customers"],
    queryFn: getCustomers,
  });

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  });


  const createMut = useMutation({
    mutationFn: (values: FormValues) => createCustomer({
      displayName: values.displayName,
      billingEmail: values.billingEmail,
      billingPhone: values.billingPhone || null,
      billingAddress: null,
      emergencyContactName: values.emergencyContactName || null,
      emergencyContactPhone: values.emergencyContactPhone || null,
      notes: values.notes || null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["customers"] });
      toast.success("Customer created");
      setOpen(false);
      reset();
    },
    onError: () => toast.error("Failed to create customer"),
  });

  const deactivateMut = useMutation({
    mutationFn: (id: string) => deactivateCustomer(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["customers"] });
      toast.success("Customer deactivated");
    },
    onError: () => toast.error("Failed to deactivate customer"),
  });

  const inviteMut = useMutation({
    mutationFn: () => {
      if (!selectedCustomerId) throw new Error("No customer selected");
      return inviteCustomer(selectedCustomerId);
    },
    onSuccess: (data) => {
      toast.success(`Invitation sent. Temporary password: ${data.temporaryPassword}`);
      qc.invalidateQueries({ queryKey: ["customers"] });
      setInviteOpen(false);
      setSelectedCustomerId(null);
    },
    onError: (error: any) => {
      const message = error.response?.status === 409
        ? "This customer already has a login"
        : error.response?.status === 404
        ? "Customer not found"
        : "Failed to send invitation";
      toast.error(message);
    },
  });

  const filtered = customers.filter((c) => {
    const matchesSearch =
      c.displayName.toLowerCase().includes(search.toLowerCase()) ||
      c.billingEmail.toLowerCase().includes(search.toLowerCase());
    const matchesActive = showInactive ? true : c.isActive;
    return matchesSearch && matchesActive;
  });

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Customers</h1>
        <Button onClick={() => { reset(); setOpen(true); }}><Plus className="h-4 w-4" /> Add Customer</Button>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-4">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input className="pl-8" placeholder="Search by name or email…" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
        <label className="flex items-center gap-2 text-sm cursor-pointer text-muted-foreground">
          <input type="checkbox" checked={showInactive} onChange={(e) => setShowInactive(e.target.checked)} className="rounded" />
          Show inactive
        </label>
      </div>

      {isLoading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : filtered.length === 0 ? (
        <p className="text-muted-foreground">No customers found.</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>Phone</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="w-24"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filtered.map((c) => (
              <TableRow key={c.id}>
                <TableCell>
                  <Link to="/customers/$customerId" params={{ customerId: c.id }} className="font-medium hover:underline">
                    {c.displayName}
                  </Link>
                </TableCell>
                <TableCell className="text-muted-foreground">{c.billingEmail}</TableCell>
                <TableCell className="text-muted-foreground">{c.billingPhone ?? "—"}</TableCell>
                <TableCell>
                  <Badge variant={c.isActive ? "success" : "secondary"}>{c.isActive ? "Active" : "Inactive"}</Badge>
                </TableCell>
                <TableCell>
                  <div className="flex gap-2">
                    {c.isActive && (
                      <>
                        <Button size="icon" variant="ghost" onClick={() => { setSelectedCustomerId(c.id); setInviteOpen(true); }} title="Invite member" className="text-muted-foreground">
                          <Mail className="h-4 w-4" />
                        </Button>
                        <Button size="icon" variant="ghost" onClick={() => deactivateMut.mutate(c.id)} title="Deactivate" className="text-muted-foreground">
                          <UserX className="h-4 w-4" />
                        </Button>
                      </>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {/* Create Dialog */}
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader><DialogTitle>Add Customer</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit((v) => createMut.mutate(v))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Display Name</Label>
              <Input {...register("displayName")} placeholder="e.g. Smith Family" />
              {errors.displayName && <p className="text-xs text-destructive">{errors.displayName.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Billing Email</Label>
              <Input type="email" {...register("billingEmail")} />
              {errors.billingEmail && <p className="text-xs text-destructive">{errors.billingEmail.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Phone (optional)</Label>
              <Input {...register("billingPhone")} />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>Emergency Contact</Label>
                <Input {...register("emergencyContactName")} placeholder="Name" />
              </div>
              <div className="space-y-1.5">
                <Label>Emergency Phone</Label>
                <Input {...register("emergencyContactPhone")} />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Notes</Label>
              <Input {...register("notes")} />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={isSubmitting}>{isSubmitting ? "Saving…" : "Add Customer"}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Invite Dialog */}
      <Dialog open={inviteOpen} onOpenChange={setInviteOpen}>
        <DialogContent>
          <DialogHeader><DialogTitle>Invite Customer to Create Login</DialogTitle></DialogHeader>
          {selectedCustomerId && customers.find((c) => c.id === selectedCustomerId) && (
            <div className="space-y-4">
              <div className="bg-muted p-4 rounded-lg space-y-2">
                <p className="text-sm text-muted-foreground">You are about to send an invitation to:</p>
                <p className="font-semibold text-base">{customers.find((c) => c.id === selectedCustomerId)?.displayName}</p>
                <p className="text-sm text-muted-foreground">{customers.find((c) => c.id === selectedCustomerId)?.billingEmail}</p>
              </div>
              <p className="text-sm text-muted-foreground">They will receive a temporary password to access the customer portal.</p>
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => setInviteOpen(false)}>Cancel</Button>
                <Button type="button" onClick={() => inviteMut.mutate()} disabled={inviteMut.isPending}>{inviteMut.isPending ? "Sending…" : "Send Invitation"}</Button>
              </DialogFooter>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
