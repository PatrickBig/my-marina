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

const inviteSchema = z.object({
  email: z.string().email("Invalid email"),
  firstName: z.string().min(1, "First name is required"),
  lastName: z.string().min(1, "Last name is required"),
});
type InviteFormValues = z.infer<typeof inviteSchema>;

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

  const { register: registerInvite, handleSubmit: handleInviteSubmit, reset: resetInvite, formState: { errors: inviteErrors, isSubmitting: inviteSubmitting } } = useForm<InviteFormValues>({
    resolver: zodResolver(inviteSchema),
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
    mutationFn: (values: InviteFormValues) => {
      if (!selectedCustomerId) throw new Error("No customer selected");
      return inviteCustomer(selectedCustomerId, {
        email: values.email,
        firstName: values.firstName,
        lastName: values.lastName,
      });
    },
    onSuccess: (data) => {
      toast.success(`Invitation sent. Temporary password: ${data.temporaryPassword}`);
      setInviteOpen(false);
      setSelectedCustomerId(null);
      resetInvite();
    },
    onError: (error: any) => {
      const message = error.response?.status === 409
        ? "This email is already registered"
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
                        <Button size="icon" variant="ghost" onClick={() => { setSelectedCustomerId(c.id); setInviteOpen(true); resetInvite(); }} title="Invite member" className="text-muted-foreground">
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
          <DialogHeader><DialogTitle>Invite Customer Member</DialogTitle></DialogHeader>
          <form onSubmit={handleInviteSubmit((v) => inviteMut.mutate(v))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Email</Label>
              <Input type="email" {...registerInvite("email")} />
              {inviteErrors.email && <p className="text-xs text-destructive">{inviteErrors.email.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>First Name</Label>
                <Input {...registerInvite("firstName")} />
                {inviteErrors.firstName && <p className="text-xs text-destructive">{inviteErrors.firstName.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Last Name</Label>
                <Input {...registerInvite("lastName")} />
                {inviteErrors.lastName && <p className="text-xs text-destructive">{inviteErrors.lastName.message}</p>}
              </div>
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setInviteOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={inviteSubmitting}>{inviteSubmitting ? "Sending…" : "Send Invitation"}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
