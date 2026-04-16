import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState } from "react";
import { toast } from "sonner";
import { Search, Users } from "lucide-react";
import {
  getPlatformUsers, resetUserPassword, deactivatePlatformUser, reactivatePlatformUser,
  type PlatformUserDto, type UserRole,
} from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useSearch } from "@tanstack/react-router";

const ROLE_LABELS: Record<number, string> = {
  0: "Platform Operator", 1: "Marina Owner", 2: "Marina Staff", 3: "Customer",
};
const ROLE_VARIANTS: Record<number, "secondary" | "default" | "warning" | "success"> = {
  0: "warning", 1: "default", 2: "secondary", 3: "success",
};

const resetSchema = z.object({
  newPassword: z.string().min(10, "Minimum 10 characters"),
});
type ResetForm = z.infer<typeof resetSchema>;

function fmtDate(iso: string | null) {
  if (!iso) return "Never";
  return new Date(iso).toLocaleDateString();
}

export function UsersPage() {
  const qc = useQueryClient();
  const search = useSearch({ from: "/platform/users" });
  const [searchText, setSearchText] = useState("");
  const [roleFilter, setRoleFilter] = useState<string>("all");
  const [selected, setSelected] = useState<PlatformUserDto | null>(null);
  const [resetTarget, setResetTarget] = useState<PlatformUserDto | null>(null);

  // Pre-filter by tenantId if navigated from tenant detail
  const tenantId = (search as Record<string, string | undefined>).tenantId;

  const { data: users = [], isLoading } = useQuery({
    queryKey: ["platform-users", searchText, roleFilter, tenantId],
    queryFn: () => getPlatformUsers({
      search: searchText || undefined,
      tenantId: tenantId || undefined,
      role: roleFilter !== "all" ? Number(roleFilter) as UserRole : undefined,
    }),
  });

  const { register, handleSubmit, reset: resetForm, formState: { errors, isSubmitting } } = useForm<ResetForm>({
    resolver: zodResolver(resetSchema),
  });

  const resetPasswordMut = useMutation({
    mutationFn: ({ id, password }: { id: string; password: string }) =>
      resetUserPassword(id, password),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["platform-users"] });
      toast.success("Password reset");
      setResetTarget(null);
      resetForm();
    },
    onError: () => toast.error("Failed to reset password"),
  });

  const deactivateMut = useMutation({
    mutationFn: (u: PlatformUserDto) => deactivatePlatformUser(u.id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["platform-users"] });
      toast.success("User deactivated");
      setSelected(null);
    },
    onError: () => toast.error("Failed to deactivate user"),
  });

  const reactivateMut = useMutation({
    mutationFn: (u: PlatformUserDto) => reactivatePlatformUser(u.id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["platform-users"] });
      toast.success("User reactivated");
      setSelected(null);
    },
    onError: () => toast.error("Failed to reactivate user"),
  });

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Users</h1>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-4">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            className="pl-9"
            placeholder="Search by email or name…"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
          />
        </div>
        <div className="flex items-center gap-2">
          <Label className="text-sm text-muted-foreground">Role</Label>
          <Select value={roleFilter} onValueChange={setRoleFilter}>
            <SelectTrigger className="w-44">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Roles</SelectItem>
              {Object.entries(ROLE_LABELS).map(([k, v]) => (
                <SelectItem key={k} value={k}>{v}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {isLoading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : users.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-muted-foreground gap-3">
          <Users className="h-10 w-10 opacity-30" />
          <p>No users found.</p>
        </div>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Email</TableHead>
              <TableHead>Name</TableHead>
              <TableHead>Role</TableHead>
              <TableHead>Tenant</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Last Login</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {users.map((u) => (
              <TableRow key={u.id}>
                <TableCell className="font-medium text-sm">{u.email}</TableCell>
                <TableCell className="text-sm">{u.firstName} {u.lastName}</TableCell>
                <TableCell>
                  <Badge variant={ROLE_VARIANTS[u.role] ?? "secondary"}>
                    {ROLE_LABELS[u.role] ?? u.role}
                  </Badge>
                </TableCell>
                <TableCell className="text-muted-foreground text-sm">{u.tenantName ?? "—"}</TableCell>
                <TableCell>
                  {u.isActive
                    ? <Badge variant="success">Active</Badge>
                    : <Badge variant="destructive">Inactive</Badge>}
                </TableCell>
                <TableCell className="text-muted-foreground text-sm">{fmtDate(u.lastLoginAt)}</TableCell>
                <TableCell className="text-right space-x-2">
                  <Button size="sm" variant="outline" onClick={() => setSelected(u)}>Detail</Button>
                  <Button size="sm" variant="outline" onClick={() => { resetForm(); setResetTarget(u); }}>
                    Reset Password
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {/* User detail dialog */}
      <Dialog open={!!selected} onOpenChange={(open) => { if (!open) setSelected(null); }}>
        <DialogContent className="max-w-sm">
          <DialogHeader><DialogTitle>User Detail</DialogTitle></DialogHeader>
          {selected && (
            <div className="space-y-3 text-sm">
              <div>
                <p className="text-muted-foreground text-xs mb-0.5">Name</p>
                <p className="font-medium">{selected.firstName} {selected.lastName}</p>
              </div>
              <div>
                <p className="text-muted-foreground text-xs mb-0.5">Email</p>
                <p>{selected.email}</p>
              </div>
              <div>
                <p className="text-muted-foreground text-xs mb-0.5">Role</p>
                <Badge variant={ROLE_VARIANTS[selected.role] ?? "secondary"}>
                  {ROLE_LABELS[selected.role] ?? selected.role}
                </Badge>
              </div>
              {selected.tenantName && (
                <div>
                  <p className="text-muted-foreground text-xs mb-0.5">Tenant</p>
                  <p>{selected.tenantName}</p>
                </div>
              )}
              {selected.marinaName && (
                <div>
                  <p className="text-muted-foreground text-xs mb-0.5">Marina</p>
                  <p>{selected.marinaName}</p>
                </div>
              )}
              <div>
                <p className="text-muted-foreground text-xs mb-0.5">Status</p>
                {selected.isActive
                  ? <Badge variant="success">Active</Badge>
                  : <Badge variant="destructive">Inactive</Badge>}
              </div>
              <div>
                <p className="text-muted-foreground text-xs mb-0.5">Last Login</p>
                <p>{fmtDate(selected.lastLoginAt)}</p>
              </div>
            </div>
          )}
          <DialogFooter className="gap-2">
            {selected?.isActive ? (
              <Button
                variant="destructive"
                size="sm"
                onClick={() => selected && deactivateMut.mutate(selected)}
                disabled={deactivateMut.isPending}
              >
                Deactivate
              </Button>
            ) : (
              <Button
                size="sm"
                onClick={() => selected && reactivateMut.mutate(selected)}
                disabled={reactivateMut.isPending}
              >
                Reactivate
              </Button>
            )}
            <Button variant="outline" size="sm" onClick={() => setSelected(null)}>Close</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Reset Password Dialog */}
      <Dialog open={!!resetTarget} onOpenChange={(open) => { if (!open) { setResetTarget(null); resetForm(); } }}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Reset Password</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            Set a new password for <strong>{resetTarget?.email}</strong>.
          </p>
          <form onSubmit={handleSubmit((v) => resetPasswordMut.mutate({ id: resetTarget!.id, password: v.newPassword }))}
            className="space-y-4">
            <div className="space-y-1.5">
              <Label>New Password</Label>
              <Input type="password" {...register("newPassword")} />
              {errors.newPassword && <p className="text-xs text-destructive">{errors.newPassword.message}</p>}
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => { setResetTarget(null); resetForm(); }}>Cancel</Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? "Resetting…" : "Reset Password"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
