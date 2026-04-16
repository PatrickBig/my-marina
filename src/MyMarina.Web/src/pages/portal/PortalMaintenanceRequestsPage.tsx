import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Plus, Wrench } from "lucide-react";
import { toast } from "sonner";
import { getPortalMaintenanceRequests, submitMaintenanceRequest } from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Controller } from "react-hook-form";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

const STATUS_LABELS: Record<number, string> = {
  0: "Submitted", 1: "Acknowledged", 2: "In Progress", 3: "Resolved", 4: "Closed",
};
const STATUS_VARIANTS: Record<number, "secondary" | "default" | "warning" | "success" | "destructive"> = {
  0: "secondary", 1: "default", 2: "warning", 3: "success", 4: "secondary",
};
const PRIORITY_LABELS: Record<number, string> = {
  0: "Low", 1: "Normal", 2: "High", 3: "Urgent",
};
const PRIORITY_VARIANTS: Record<number, "secondary" | "default" | "warning" | "destructive"> = {
  0: "secondary", 1: "default", 2: "warning", 3: "destructive",
};

const schema = z.object({
  title: z.string().min(1, "Title is required").max(300),
  description: z.string().min(1, "Description is required").max(4000),
  priority: z.number(),
});
type FormValues = z.infer<typeof schema>;

export function PortalMaintenanceRequestsPage() {
  const [dialogOpen, setDialogOpen] = useState(false);
  const qc = useQueryClient();

  const { data: requests, isLoading } = useQuery({
    queryKey: ["portal-maintenance"],
    queryFn: getPortalMaintenanceRequests,
  });

  const form = useForm<FormValues>({
    resolver: zodResolver(schema) as any,
    defaultValues: { priority: 1 },
  });

  const submitMut = useMutation({
    mutationFn: (v: FormValues) => submitMaintenanceRequest({
      title: v.title,
      description: v.description,
      priority: v.priority,
      slipId: null,
      boatId: null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["portal-maintenance"] });
      toast.success("Maintenance request submitted");
      setDialogOpen(false);
      form.reset({ priority: 1 });
    },
    onError: () => toast.error("Failed to submit request"),
  });

  const openDialog = () => {
    form.reset({ title: "", description: "", priority: 1 });
    setDialogOpen(true);
  };

  if (isLoading) return <div className="p-8 text-muted-foreground">Loading…</div>;

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Maintenance Requests</h1>
        <Button onClick={openDialog}><Plus className="h-4 w-4" /> New Request</Button>
      </div>

      {!requests || requests.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-md border border-dashed p-16 text-center">
          <Wrench className="h-10 w-10 text-muted-foreground mb-4" />
          <p className="text-muted-foreground">No maintenance requests yet.</p>
          <Button variant="outline" className="mt-4" onClick={openDialog}>Submit a request</Button>
        </div>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Title</TableHead>
              <TableHead>Priority</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Submitted</TableHead>
              <TableHead>Resolved</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {requests.map((r) => (
              <TableRow key={r.id}>
                <TableCell>
                  <div>
                    <p className="font-medium">{r.title}</p>
                    {(r.slipName || r.boatName) && (
                      <p className="text-xs text-muted-foreground">
                        {[r.slipName, r.boatName].filter(Boolean).join(" · ")}
                      </p>
                    )}
                  </div>
                </TableCell>
                <TableCell>
                  <Badge variant={PRIORITY_VARIANTS[r.priority] ?? "secondary"}>
                    {PRIORITY_LABELS[r.priority] ?? r.priority}
                  </Badge>
                </TableCell>
                <TableCell>
                  <Badge variant={STATUS_VARIANTS[r.status] ?? "secondary"}>
                    {STATUS_LABELS[r.status] ?? r.status}
                  </Badge>
                </TableCell>
                <TableCell className="text-sm">{new Date(r.submittedAt).toLocaleDateString()}</TableCell>
                <TableCell className="text-sm text-muted-foreground">
                  {r.resolvedAt ? new Date(r.resolvedAt).toLocaleDateString() : "—"}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {/* Submit dialog */}
      <Dialog open={dialogOpen} onOpenChange={(v) => { if (!v) setDialogOpen(false); }}>
        <DialogContent>
          <DialogHeader><DialogTitle>Submit Maintenance Request</DialogTitle></DialogHeader>
          <form onSubmit={form.handleSubmit((v) => submitMut.mutate(v))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Title</Label>
              <Input {...form.register("title")} placeholder="e.g. Electrical outlet not working" />
              {form.formState.errors.title && (
                <p className="text-xs text-destructive">{form.formState.errors.title.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label>Description</Label>
              <textarea
                {...form.register("description")}
                className="w-full min-h-[100px] rounded-md border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring resize-y"
                placeholder="Describe the issue in detail…"
              />
              {form.formState.errors.description && (
                <p className="text-xs text-destructive">{form.formState.errors.description.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label>Priority</Label>
              <Controller control={form.control} name="priority" render={({ field }) => (
                <Select value={String(field.value)} onValueChange={(v) => field.onChange(Number(v))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {Object.entries(PRIORITY_LABELS).map(([k, v]) => (
                      <SelectItem key={k} value={k}>{v}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )} />
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={form.formState.isSubmitting || submitMut.isPending}>
                {submitMut.isPending ? "Submitting…" : "Submit Request"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
