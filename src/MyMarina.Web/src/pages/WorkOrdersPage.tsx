import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState } from "react";
import { toast } from "sonner";
import { Plus, ClipboardList } from "lucide-react";
import {
  getWorkOrders, createWorkOrder, updateWorkOrder, completeWorkOrder,
  type WorkOrderDto, type WorkOrderStatus, type Priority,
} from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Textarea } from "@/components/ui/textarea";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel,
  AlertDialogContent, AlertDialogDescription, AlertDialogFooter,
  AlertDialogHeader, AlertDialogTitle,
} from "@/components/ui/alert-dialog";

const STATUS_LABELS: Record<number, string> = {
  0: "Open", 1: "In Progress", 2: "On Hold", 3: "Completed", 4: "Cancelled",
};
const STATUS_VARIANTS: Record<number, "secondary" | "default" | "warning" | "success" | "destructive"> = {
  0: "default", 1: "warning", 2: "secondary", 3: "success", 4: "destructive",
};
const PRIORITY_LABELS: Record<number, string> = { 0: "Low", 1: "Medium", 2: "High", 3: "Urgent" };
const PRIORITY_VARIANTS: Record<number, "secondary" | "default" | "warning" | "destructive"> = {
  0: "secondary", 1: "default", 2: "warning", 3: "destructive",
};

function StatusBadge({ status }: { status: WorkOrderStatus }) {
  return <Badge variant={STATUS_VARIANTS[status] ?? "secondary"}>{STATUS_LABELS[status] ?? status}</Badge>;
}
function PriorityBadge({ priority }: { priority: Priority }) {
  return <Badge variant={PRIORITY_VARIANTS[priority] ?? "secondary"}>{PRIORITY_LABELS[priority] ?? priority}</Badge>;
}

function fmtDate(d: string | null) {
  if (!d) return "—";
  return new Date(d).toLocaleDateString();
}

const formSchema = z.object({
  title: z.string().min(1, "Title is required"),
  description: z.string().min(1, "Description is required"),
  priority: z.number().int().min(0).max(3),
  status: z.number().int().min(0).max(4).optional(),
  scheduledDate: z.string().optional().nullable(),
  notes: z.string().optional().nullable(),
});
type FormValues = z.infer<typeof formSchema>;

export function WorkOrdersPage() {
  const qc = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<WorkOrderDto | null>(null);
  const [completeTarget, setCompleteTarget] = useState<WorkOrderDto | null>(null);

  const { data: workOrders = [], isLoading } = useQuery({
    queryKey: ["work-orders", statusFilter],
    queryFn: () => getWorkOrders({
      status: statusFilter !== "all" ? Number(statusFilter) as WorkOrderStatus : undefined,
    }),
  });

  const { register, handleSubmit, reset, control, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: { title: "", description: "", priority: 1, scheduledDate: "", notes: "" },
  });

  const openCreate = () => {
    setEditing(null);
    reset({ title: "", description: "", priority: 1, status: 0, scheduledDate: "", notes: "" });
    setDialogOpen(true);
  };

  const openEdit = (wo: WorkOrderDto) => {
    setEditing(wo);
    reset({
      title: wo.title,
      description: wo.description,
      priority: wo.priority,
      status: wo.status,
      scheduledDate: wo.scheduledDate ?? "",
      notes: wo.notes ?? "",
    });
    setDialogOpen(true);
  };

  const saveMut = useMutation({
    mutationFn: async (v: FormValues) => {
      const payload = {
        title: v.title,
        description: v.description,
        priority: v.priority as Priority,
        scheduledDate: v.scheduledDate || null,
        notes: v.notes || null,
      };
      if (editing) {
        await updateWorkOrder(editing.id, {
          ...payload,
          status: (v.status ?? editing.status) as WorkOrderStatus,
        });
      } else {
        await createWorkOrder(payload);
      }
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["work-orders"] });
      toast.success(editing ? "Work order updated" : "Work order created");
      setDialogOpen(false);
      reset();
    },
    onError: () => toast.error("Failed to save work order"),
  });

  const completeMut = useMutation({
    mutationFn: (wo: WorkOrderDto) => completeWorkOrder(wo.id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["work-orders"] });
      toast.success("Work order completed");
      setCompleteTarget(null);
    },
    onError: () => toast.error("Failed to complete work order"),
  });

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Work Orders</h1>
        <Button onClick={openCreate}>
          <Plus className="h-4 w-4" /> New Work Order
        </Button>
      </div>

      {/* Status filter */}
      <div className="flex items-center gap-2">
        <Label className="text-sm text-muted-foreground">Status</Label>
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-44">
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

      {isLoading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : workOrders.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-muted-foreground gap-3">
          <ClipboardList className="h-10 w-10 opacity-30" />
          <p>No work orders found.</p>
        </div>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Title</TableHead>
              <TableHead>Linked Request</TableHead>
              <TableHead>Priority</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Assigned To</TableHead>
              <TableHead>Scheduled</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {workOrders.map((wo) => (
              <TableRow key={wo.id}>
                <TableCell className="font-medium">{wo.title}</TableCell>
                <TableCell className="text-muted-foreground text-sm">
                  {wo.maintenanceRequestTitle ?? "—"}
                </TableCell>
                <TableCell><PriorityBadge priority={wo.priority} /></TableCell>
                <TableCell><StatusBadge status={wo.status} /></TableCell>
                <TableCell className="text-sm">{wo.assignedToName ?? "—"}</TableCell>
                <TableCell className="text-muted-foreground text-sm">{fmtDate(wo.scheduledDate)}</TableCell>
                <TableCell className="text-right space-x-2">
                  <Button size="sm" variant="outline" onClick={() => openEdit(wo)}>
                    Edit
                  </Button>
                  {wo.status !== 3 && wo.status !== 4 && (
                    <Button
                      size="sm"
                      onClick={() => setCompleteTarget(wo)}
                    >
                      Complete
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {/* Create / Edit Dialog */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>{editing ? "Edit Work Order" : "New Work Order"}</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit((v) => saveMut.mutate(v))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Title</Label>
              <Input {...register("title")} placeholder="Work order title…" />
              {errors.title && <p className="text-xs text-destructive">{errors.title.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Description</Label>
              <Textarea
                {...register("description")}
                placeholder="Describe the work to be done…"
                rows={4}
                className="resize-none"
              />
              {errors.description && <p className="text-xs text-destructive">{errors.description.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>Priority</Label>
                <Controller
                  control={control}
                  name="priority"
                  render={({ field }) => (
                    <Select value={String(field.value)} onValueChange={(v) => field.onChange(Number(v))}>
                      <SelectTrigger><SelectValue /></SelectTrigger>
                      <SelectContent>
                        {Object.entries(PRIORITY_LABELS).map(([k, v]) => (
                          <SelectItem key={k} value={k}>{v}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </div>
              {editing && (
                <div className="space-y-1.5">
                  <Label>Status</Label>
                  <Controller
                    control={control}
                    name="status"
                    render={({ field }) => (
                      <Select value={String(field.value ?? editing.status)} onValueChange={(v) => field.onChange(Number(v))}>
                        <SelectTrigger><SelectValue /></SelectTrigger>
                        <SelectContent>
                          {Object.entries(STATUS_LABELS).map(([k, v]) => (
                            <SelectItem key={k} value={k}>{v}</SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  />
                </div>
              )}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>Scheduled Date (optional)</Label>
                <Input type="date" {...register("scheduledDate")} />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Notes (optional)</Label>
              <Textarea
                {...register("notes")}
                placeholder="Internal notes…"
                rows={2}
                className="resize-none"
              />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? "Saving…" : editing ? "Save Changes" : "Create"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Complete Confirm */}
      <AlertDialog open={!!completeTarget} onOpenChange={(open) => { if (!open) setCompleteTarget(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Complete Work Order?</AlertDialogTitle>
            <AlertDialogDescription>
              Mark "{completeTarget?.title}" as completed?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => completeTarget && completeMut.mutate(completeTarget)}
            >
              Complete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
