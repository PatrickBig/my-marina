import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState } from "react";
import { toast } from "sonner";
import { Plus, Building2 } from "lucide-react";
import { Link } from "@tanstack/react-router";
import {
  getTenants, suspendTenant, reactivateTenant, createTenant,
  type TenantDto, type SubscriptionTier,
} from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Controller } from "react-hook-form";

const TIER_LABELS: Record<number, string> = { 0: "Free", 1: "Starter", 2: "Pro", 3: "Enterprise" };

const createSchema = z.object({
  name: z.string().min(1, "Name is required"),
  slug: z.string().min(1, "Slug is required").regex(/^[a-z0-9-]+$/, "Lowercase letters, numbers, hyphens only"),
  ownerEmail: z.string().email("Valid email required"),
  ownerFirstName: z.string().min(1, "Required"),
  ownerLastName: z.string().min(1, "Required"),
  ownerPassword: z.string().min(10, "Minimum 10 characters"),
  subscriptionTier: z.number().int().min(0).max(3),
});
type CreateForm = z.infer<typeof createSchema>;

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString();
}

export function TenantsPage() {
  const qc = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);

  const { data: tenants = [], isLoading } = useQuery({
    queryKey: ["platform-tenants"],
    queryFn: getTenants,
  });

  const { register, handleSubmit, reset, control, formState: { errors, isSubmitting } } = useForm<CreateForm>({
    resolver: zodResolver(createSchema),
    defaultValues: { subscriptionTier: 0 },
  });

  const createMut = useMutation({
    mutationFn: (v: CreateForm) => createTenant({
      name: v.name,
      slug: v.slug,
      ownerEmail: v.ownerEmail,
      ownerFirstName: v.ownerFirstName,
      ownerLastName: v.ownerLastName,
      ownerPassword: v.ownerPassword,
      subscriptionTier: v.subscriptionTier as SubscriptionTier,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["platform-tenants"] });
      toast.success("Tenant created");
      setCreateOpen(false);
      reset();
    },
    onError: () => toast.error("Failed to create tenant"),
  });

  const suspendMut = useMutation({
    mutationFn: (t: TenantDto) => suspendTenant(t.id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["platform-tenants"] });
      toast.success("Tenant suspended");
    },
    onError: () => toast.error("Failed to suspend tenant"),
  });

  const reactivateMut = useMutation({
    mutationFn: (t: TenantDto) => reactivateTenant(t.id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["platform-tenants"] });
      toast.success("Tenant reactivated");
    },
    onError: () => toast.error("Failed to reactivate tenant"),
  });

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Tenants</h1>
        <Button onClick={() => { reset(); setCreateOpen(true); }}>
          <Plus className="h-4 w-4" /> New Tenant
        </Button>
      </div>

      {isLoading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : tenants.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-muted-foreground gap-3">
          <Building2 className="h-10 w-10 opacity-30" />
          <p>No tenants found.</p>
        </div>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Slug</TableHead>
              <TableHead>Tier</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Created</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {tenants.map((t) => (
              <TableRow key={t.id}>
                <TableCell className="font-medium">
                  <Link
                    to="/platform/tenants/$tenantId"
                    params={{ tenantId: t.id }}
                    className="hover:underline text-primary"
                  >
                    {t.name}
                  </Link>
                </TableCell>
                <TableCell className="font-mono text-sm text-muted-foreground">{t.slug}</TableCell>
                <TableCell>
                  <Badge variant="secondary">{TIER_LABELS[t.subscriptionTier] ?? t.subscriptionTier}</Badge>
                </TableCell>
                <TableCell>
                  {t.isActive
                    ? <Badge variant="success">Active</Badge>
                    : <Badge variant="destructive">Suspended</Badge>
                  }
                </TableCell>
                <TableCell className="text-muted-foreground text-sm">{fmtDate(t.createdAt)}</TableCell>
                <TableCell className="text-right space-x-2">
                  <Link to="/platform/tenants/$tenantId" params={{ tenantId: t.id }}>
                    <Button size="sm" variant="outline">Detail</Button>
                  </Link>
                  {t.isActive ? (
                    <Button
                      size="sm"
                      variant="destructive"
                      onClick={() => suspendMut.mutate(t)}
                      disabled={suspendMut.isPending}
                    >
                      Suspend
                    </Button>
                  ) : (
                    <Button
                      size="sm"
                      onClick={() => reactivateMut.mutate(t)}
                      disabled={reactivateMut.isPending}
                    >
                      Reactivate
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {/* Create Tenant Dialog */}
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="max-w-lg">
          <DialogHeader><DialogTitle>New Tenant</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit((v) => createMut.mutate(v))} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>Marina Name</Label>
                <Input {...register("name")} placeholder="Sunseeker Marina" />
                {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Slug</Label>
                <Input {...register("slug")} placeholder="sunseeker-marina" />
                {errors.slug && <p className="text-xs text-destructive">{errors.slug.message}</p>}
              </div>
            </div>

            <div className="space-y-1.5">
              <Label>Subscription Tier</Label>
              <Controller
                control={control}
                name="subscriptionTier"
                render={({ field }) => (
                  <Select value={String(field.value)} onValueChange={(v) => field.onChange(Number(v))}>
                    <SelectTrigger><SelectValue /></SelectTrigger>
                    <SelectContent>
                      {Object.entries(TIER_LABELS).map(([k, v]) => (
                        <SelectItem key={k} value={k}>{v}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </div>

            <p className="text-sm font-medium text-muted-foreground pt-1">Owner Account</p>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>First Name</Label>
                <Input {...register("ownerFirstName")} />
                {errors.ownerFirstName && <p className="text-xs text-destructive">{errors.ownerFirstName.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Last Name</Label>
                <Input {...register("ownerLastName")} />
                {errors.ownerLastName && <p className="text-xs text-destructive">{errors.ownerLastName.message}</p>}
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Email</Label>
              <Input type="email" {...register("ownerEmail")} />
              {errors.ownerEmail && <p className="text-xs text-destructive">{errors.ownerEmail.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Temporary Password</Label>
              <Input type="password" {...register("ownerPassword")} />
              {errors.ownerPassword && <p className="text-xs text-destructive">{errors.ownerPassword.message}</p>}
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setCreateOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? "Creating…" : "Create Tenant"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
