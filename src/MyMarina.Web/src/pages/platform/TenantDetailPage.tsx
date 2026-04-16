import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useEffect } from "react";
import { toast } from "sonner";
import { ArrowLeft } from "lucide-react";
import { Link, useParams } from "@tanstack/react-router";
import { getTenant, updateTenant, type SubscriptionTier } from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

const TIER_LABELS: Record<number, string> = { 0: "Free", 1: "Starter", 2: "Pro", 3: "Enterprise" };

const editSchema = z.object({
  name: z.string().min(1, "Name is required"),
  subscriptionTier: z.number().int().min(0).max(3),
});
type EditForm = z.infer<typeof editSchema>;

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString();
}

export function TenantDetailPage() {
  const qc = useQueryClient();
  const { tenantId } = useParams({ from: "/platform/tenants/$tenantId" });

  const { data: tenant, isLoading } = useQuery({
    queryKey: ["platform-tenant", tenantId],
    queryFn: () => getTenant(tenantId),
  });

  const { register, handleSubmit, reset, control, formState: { errors, isSubmitting, isDirty } } = useForm<EditForm>({
    resolver: zodResolver(editSchema),
  });

  useEffect(() => {
    if (tenant) reset({ name: tenant.name, subscriptionTier: tenant.subscriptionTier });
  }, [tenant, reset]);

  const updateMut = useMutation({
    mutationFn: (v: EditForm) => updateTenant(tenantId, {
      name: v.name,
      isActive: tenant!.isActive,
      subscriptionTier: v.subscriptionTier as SubscriptionTier,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["platform-tenant", tenantId] });
      qc.invalidateQueries({ queryKey: ["platform-tenants"] });
      toast.success("Tenant updated");
    },
    onError: () => toast.error("Failed to update tenant"),
  });

  if (isLoading) return <div className="p-8 text-muted-foreground">Loading…</div>;
  if (!tenant) return <div className="p-8 text-muted-foreground">Tenant not found.</div>;

  return (
    <div className="p-8 space-y-6 max-w-4xl">
      <div className="flex items-center gap-3">
        <Link to="/platform/tenants">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="h-4 w-4" /> Tenants
          </Button>
        </Link>
        <h1 className="text-2xl font-bold">{tenant.name}</h1>
        {tenant.isActive
          ? <Badge variant="success">Active</Badge>
          : <Badge variant="destructive">Suspended</Badge>}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Edit form */}
        <Card>
          <CardHeader><CardTitle>Settings</CardTitle></CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit((v) => updateMut.mutate(v))} className="space-y-4">
              <div className="space-y-1.5">
                <Label>Name</Label>
                <Input {...register("name")} />
                {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
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
              <div className="space-y-0.5 text-sm text-muted-foreground">
                <p>Slug: <span className="font-mono">{tenant.slug}</span></p>
                <p>Created: {fmtDate(tenant.createdAt)}</p>
              </div>
              <Button type="submit" disabled={isSubmitting || !isDirty}>
                {isSubmitting ? "Saving…" : "Save Changes"}
              </Button>
            </form>
          </CardContent>
        </Card>

        {/* Owner */}
        <Card>
          <CardHeader><CardTitle>Marina Owner</CardTitle></CardHeader>
          <CardContent>
            {tenant.owner ? (
              <div className="space-y-2 text-sm">
                <p className="font-medium">{tenant.owner.firstName} {tenant.owner.lastName}</p>
                <p className="text-muted-foreground">{tenant.owner.email}</p>
                <div>
                  {tenant.owner.isActive
                    ? <Badge variant="success">Active</Badge>
                    : <Badge variant="destructive">Inactive</Badge>}
                </div>
                <div className="pt-2">
                  <Link to="/platform/users" search={{ tenantId: tenant.id }}>
                    <Button size="sm" variant="outline">View All Users</Button>
                  </Link>
                </div>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">No owner account found.</p>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Marinas */}
      <Card>
        <CardHeader><CardTitle>Marinas ({tenant.marinas.length})</CardTitle></CardHeader>
        <CardContent>
          {tenant.marinas.length === 0 ? (
            <p className="text-sm text-muted-foreground">No marinas yet.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Created</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {tenant.marinas.map((m) => (
                  <TableRow key={m.id}>
                    <TableCell className="font-medium">{m.name}</TableCell>
                    <TableCell className="text-muted-foreground text-sm">{fmtDate(m.createdAt)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
