import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { toast } from "sonner";
import { Wrench, ExternalLink } from "lucide-react";
import { Link } from "@tanstack/react-router";
import {
  getMaintenanceRequests, updateMaintenanceStatus, createWorkOrder,
  type MaintenanceRequestDto, type MaintenanceStatus, type Priority,
} from "@/api/api";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";

const STATUS_LABELS: Record<number, string> = {
  0: "Submitted", 1: "Under Review", 2: "In Progress", 3: "Completed", 4: "Declined",
};
const STATUS_VARIANTS: Record<number, "secondary" | "default" | "warning" | "success" | "destructive"> = {
  0: "default", 1: "secondary", 2: "warning", 3: "success", 4: "destructive",
};
const PRIORITY_LABELS: Record<number, string> = { 0: "Low", 1: "Medium", 2: "High", 3: "Urgent" };
const PRIORITY_VARIANTS: Record<number, "secondary" | "default" | "warning" | "destructive"> = {
  0: "secondary", 1: "default", 2: "warning", 3: "destructive",
};

function StatusBadge({ status }: { status: MaintenanceStatus }) {
  return <Badge variant={STATUS_VARIANTS[status] ?? "secondary"}>{STATUS_LABELS[status] ?? status}</Badge>;
}
function PriorityBadge({ priority }: { priority: Priority }) {
  return <Badge variant={PRIORITY_VARIANTS[priority] ?? "secondary"}>{PRIORITY_LABELS[priority] ?? priority}</Badge>;
}

function fmtDate(iso: string | null) {
  if (!iso) return "—";
  return new Date(iso).toLocaleDateString();
}

export function MaintenanceRequestsPage() {
  const qc = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [priorityFilter, setPriorityFilter] = useState<string>("all");
  const [selected, setSelected] = useState<MaintenanceRequestDto | null>(null);
  const [workOrderDialog, setWorkOrderDialog] = useState(false);

  const { data: requests = [], isLoading } = useQuery({
    queryKey: ["maintenance-requests", statusFilter, priorityFilter],
    queryFn: () => getMaintenanceRequests({
      status: statusFilter !== "all" ? Number(statusFilter) as MaintenanceStatus : undefined,
      priority: priorityFilter !== "all" ? Number(priorityFilter) as Priority : undefined,
    }),
  });

  const updateStatusMut = useMutation({
    mutationFn: ({ id, status }: { id: string; status: MaintenanceStatus }) =>
      updateMaintenanceStatus(id, status),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["maintenance-requests"] });
      toast.success("Status updated");
    },
    onError: () => toast.error("Failed to update status"),
  });

  const createWorkOrderMut = useMutation({
    mutationFn: (req: MaintenanceRequestDto) =>
      createWorkOrder({
        title: req.title,
        description: req.description,
        priority: req.priority,
        maintenanceRequestId: req.id,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["maintenance-requests"] });
      qc.invalidateQueries({ queryKey: ["work-orders"] });
      toast.success("Work order created");
      setWorkOrderDialog(false);
      setSelected(null);
    },
    onError: () => toast.error("Failed to create work order"),
  });

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Maintenance Requests</h1>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-4">
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
        <div className="flex items-center gap-2">
          <Label className="text-sm text-muted-foreground">Priority</Label>
          <Select value={priorityFilter} onValueChange={setPriorityFilter}>
            <SelectTrigger className="w-36">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Priorities</SelectItem>
              {Object.entries(PRIORITY_LABELS).map(([k, v]) => (
                <SelectItem key={k} value={k}>{v}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {isLoading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : requests.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-muted-foreground gap-3">
          <Wrench className="h-10 w-10 opacity-30" />
          <p>No maintenance requests found.</p>
        </div>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Title</TableHead>
              <TableHead>Customer</TableHead>
              <TableHead>Priority</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Submitted</TableHead>
              <TableHead>Resolved</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {requests.map((r) => (
              <TableRow key={r.id}>
                <TableCell className="font-medium max-w-xs">
                  <button
                    className="text-left hover:underline text-primary"
                    onClick={() => setSelected(r)}
                  >
                    {r.title}
                  </button>
                </TableCell>
                <TableCell>
                  <Link
                    to="/customers/$customerId"
                    params={{ customerId: r.customerAccountId }}
                    className="hover:underline"
                  >
                    {r.customerDisplayName}
                  </Link>
                </TableCell>
                <TableCell><PriorityBadge priority={r.priority} /></TableCell>
                <TableCell><StatusBadge status={r.status} /></TableCell>
                <TableCell className="text-muted-foreground text-sm">{fmtDate(r.submittedAt)}</TableCell>
                <TableCell className="text-muted-foreground text-sm">{fmtDate(r.resolvedAt)}</TableCell>
                <TableCell className="text-right">
                  <Select
                    value={String(r.status)}
                    onValueChange={(v) => updateStatusMut.mutate({ id: r.id, status: Number(v) as MaintenanceStatus })}
                  >
                    <SelectTrigger className="w-36 h-8 text-xs">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {Object.entries(STATUS_LABELS).map(([k, v]) => (
                        <SelectItem key={k} value={k}>{v}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {/* Detail Dialog */}
      <Dialog open={!!selected && !workOrderDialog} onOpenChange={(open) => { if (!open) setSelected(null); }}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>{selected?.title}</DialogTitle>
          </DialogHeader>
          {selected && (
            <div className="space-y-4 text-sm">
              <div className="flex gap-3">
                <PriorityBadge priority={selected.priority} />
                <StatusBadge status={selected.status} />
              </div>
              <div>
                <p className="font-medium text-muted-foreground mb-1">Customer</p>
                <p>{selected.customerDisplayName}</p>
              </div>
              {selected.slipName && (
                <div>
                  <p className="font-medium text-muted-foreground mb-1">Slip</p>
                  <p>{selected.slipName}</p>
                </div>
              )}
              {selected.boatName && (
                <div>
                  <p className="font-medium text-muted-foreground mb-1">Boat</p>
                  <p>{selected.boatName}</p>
                </div>
              )}
              <div>
                <p className="font-medium text-muted-foreground mb-1">Description</p>
                <p className="whitespace-pre-wrap">{selected.description}</p>
              </div>
              <div className="text-muted-foreground text-xs">
                Submitted {new Date(selected.submittedAt).toLocaleString()}
                {selected.resolvedAt && ` · Resolved ${new Date(selected.resolvedAt).toLocaleString()}`}
              </div>
            </div>
          )}
          <DialogFooter className="gap-2">
            {selected && !selected.workOrderId && (
              <Button
                variant="outline"
                onClick={() => setWorkOrderDialog(true)}
              >
                <ExternalLink className="h-4 w-4 mr-1" /> Create Work Order
              </Button>
            )}
            {selected?.workOrderId && (
              <Link to="/work-orders" className="text-sm text-primary underline self-center">
                View Work Order
              </Link>
            )}
            <Button variant="outline" onClick={() => setSelected(null)}>Close</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Create Work Order Confirm */}
      <Dialog open={workOrderDialog} onOpenChange={setWorkOrderDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create Work Order</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            Create an internal work order for "{selected?.title}"? You can assign staff and schedule it from the Work Orders page.
          </p>
          <DialogFooter>
            <Button variant="outline" onClick={() => setWorkOrderDialog(false)}>Cancel</Button>
            <Button
              onClick={() => selected && createWorkOrderMut.mutate(selected)}
              disabled={createWorkOrderMut.isPending}
            >
              {createWorkOrderMut.isPending ? "Creating…" : "Create Work Order"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
