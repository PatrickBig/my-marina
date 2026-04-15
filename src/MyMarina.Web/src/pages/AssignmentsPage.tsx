import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState } from "react";
import { toast } from "sonner";
import { Plus, StopCircle } from "lucide-react";
import {
  getMarinas, getSlips, getCustomers, getBoats, getAssignments, createAssignment, endAssignment,
  type SlipAssignmentDto,
} from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

const AssignmentType = { 0: "Daily", 1: "Monthly", 2: "Annual", 3: "Seasonal" } as const;

const createSchema = z.object({
  slipId: z.string().min(1, "Select a slip"),
  customerAccountId: z.string().min(1, "Select a customer"),
  boatId: z.string().min(1, "Select a boat"),
  assignmentType: z.number(),
  startDate: z.string().min(1, "Required"),
  endDate: z.string().optional().nullable(),
  rateOverride: z.preprocess((v) => (v === "" || v === null || v === undefined ? null : Number(v)), z.number().nullable()),
  notes: z.string().optional().nullable(),
});
type CreateForm = z.infer<typeof createSchema>;

const endSchema = z.object({
  endDate: z.string().min(1, "End date is required"),
});
type EndForm = z.infer<typeof endSchema>;

export function AssignmentsPage() {
  const qc = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const [endingAssignment, setEndingAssignment] = useState<SlipAssignmentDto | null>(null);
  const [activeOnly, setActiveOnly] = useState(true);
  const [selectedCustomer, setSelectedCustomer] = useState<string>("");

  const { data: marinas } = useQuery({ queryKey: ["marinas"], queryFn: getMarinas });
  const marinaId = marinas?.[0]?.id;

  const { data: slips = [] } = useQuery({
    queryKey: ["slips", marinaId],
    queryFn: () => getSlips(marinaId!),
    enabled: !!marinaId,
  });

  const { data: customers = [] } = useQuery({
    queryKey: ["customers"],
    queryFn: getCustomers,
  });

  const { data: customerBoats = [] } = useQuery({
    queryKey: ["boats", selectedCustomer],
    queryFn: () => getBoats(selectedCustomer),
    enabled: !!selectedCustomer,
  });

  const { data: assignments = [], isLoading } = useQuery({
    queryKey: ["assignments", activeOnly],
    queryFn: () => getAssignments({ activeOnly }),
  });

  const createForm = useForm<CreateForm, any, CreateForm>({ resolver: zodResolver(createSchema) as any });
  const endForm = useForm<EndForm>({ resolver: zodResolver(endSchema) });

  const createMut = useMutation({
    mutationFn: (v: CreateForm) => createAssignment({
      slipId: v.slipId, customerAccountId: v.customerAccountId, boatId: v.boatId,
      assignmentType: v.assignmentType, startDate: v.startDate,
      endDate: v.endDate || null, rateOverride: v.rateOverride ?? null, notes: v.notes || null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["assignments"] });
      toast.success("Slip assigned successfully");
      setCreateOpen(false);
      createForm.reset();
    },
    onError: (err: any) => {
      const msg = err?.response?.status === 409 ? "This slip is already assigned for those dates." : "Failed to create assignment";
      toast.error(msg);
    },
  });

  const endMut = useMutation({
    mutationFn: ({ id, endDate }: { id: string; endDate: string }) => endAssignment(id, endDate),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["assignments"] });
      toast.success("Assignment ended");
      setEndingAssignment(null);
    },
    onError: () => toast.error("Failed to end assignment"),
  });

  const today = new Date().toISOString().split("T")[0];
  const isActive = (a: SlipAssignmentDto) => !a.endDate || a.endDate >= today;

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Slip Assignments</h1>
        <Button onClick={() => { createForm.reset(); setSelectedCustomer(""); setCreateOpen(true); }}>
          <Plus className="h-4 w-4" /> Assign Slip
        </Button>
      </div>

      <div className="flex items-center gap-4">
        <label className="flex items-center gap-2 text-sm cursor-pointer text-muted-foreground">
          <input type="checkbox" checked={activeOnly} onChange={(e) => setActiveOnly(e.target.checked)} className="rounded" />
          Active only
        </label>
      </div>

      {isLoading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : assignments.length === 0 ? (
        <p className="text-muted-foreground">No assignments found.</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Slip</TableHead>
              <TableHead>Customer</TableHead>
              <TableHead>Boat</TableHead>
              <TableHead>Type</TableHead>
              <TableHead>Start</TableHead>
              <TableHead>End</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="w-24"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {assignments.map((a) => {
              const active = isActive(a);
              return (
                <TableRow key={a.id}>
                  <TableCell className="font-medium">{a.slipName}</TableCell>
                  <TableCell>{a.customerDisplayName}</TableCell>
                  <TableCell className="text-muted-foreground">{a.boatName}</TableCell>
                  <TableCell>{AssignmentType[a.assignmentType as keyof typeof AssignmentType] ?? a.assignmentType}</TableCell>
                  <TableCell className="text-muted-foreground">{a.startDate}</TableCell>
                  <TableCell className="text-muted-foreground">{a.endDate ?? "Open-ended"}</TableCell>
                  <TableCell>
                    <Badge variant={active ? "success" : "secondary"}>{active ? "Active" : "Past"}</Badge>
                  </TableCell>
                  <TableCell>
                    {active && (
                      <Button size="icon" variant="ghost" onClick={() => { setEndingAssignment(a); endForm.reset({ endDate: today }); }} title="End assignment">
                        <StopCircle className="h-4 w-4 text-destructive" />
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      )}

      {/* Create Assignment Dialog */}
      <Dialog open={createOpen} onOpenChange={(v) => { if (!v) setCreateOpen(false); }}>
        <DialogContent className="max-w-lg">
          <DialogHeader><DialogTitle>Assign Slip</DialogTitle></DialogHeader>
          <form onSubmit={createForm.handleSubmit((v) => createMut.mutate(v))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Slip</Label>
              <Controller control={createForm.control} name="slipId" render={({ field }) => (
                <Select value={field.value ?? ""} onValueChange={field.onChange}>
                  <SelectTrigger><SelectValue placeholder="Select a slip…" /></SelectTrigger>
                  <SelectContent>
                    {slips.map(s => <SelectItem key={s.id} value={s.id}>{s.name}</SelectItem>)}
                  </SelectContent>
                </Select>
              )} />
              {createForm.formState.errors.slipId && <p className="text-xs text-destructive">{createForm.formState.errors.slipId.message}</p>}
            </div>

            <div className="space-y-1.5">
              <Label>Customer</Label>
              <Controller control={createForm.control} name="customerAccountId" render={({ field }) => (
                <Select value={field.value ?? ""} onValueChange={(v) => { field.onChange(v); setSelectedCustomer(v); createForm.setValue("boatId", ""); }}>
                  <SelectTrigger><SelectValue placeholder="Select a customer…" /></SelectTrigger>
                  <SelectContent>
                    {customers.filter(c => c.isActive).map(c => <SelectItem key={c.id} value={c.id}>{c.displayName}</SelectItem>)}
                  </SelectContent>
                </Select>
              )} />
              {createForm.formState.errors.customerAccountId && <p className="text-xs text-destructive">{createForm.formState.errors.customerAccountId.message}</p>}
            </div>

            <div className="space-y-1.5">
              <Label>Boat</Label>
              <Controller control={createForm.control} name="boatId" render={({ field }) => (
                <Select value={field.value ?? ""} onValueChange={field.onChange} disabled={!selectedCustomer}>
                  <SelectTrigger><SelectValue placeholder={selectedCustomer ? "Select a boat…" : "Select customer first"} /></SelectTrigger>
                  <SelectContent>
                    {customerBoats.map(b => <SelectItem key={b.id} value={b.id}>{b.name}</SelectItem>)}
                  </SelectContent>
                </Select>
              )} />
              {createForm.formState.errors.boatId && <p className="text-xs text-destructive">{createForm.formState.errors.boatId.message}</p>}
            </div>

            <div className="space-y-1.5">
              <Label>Assignment Type</Label>
              <Controller control={createForm.control} name="assignmentType" render={({ field }) => (
                <Select value={String(field.value ?? 1)} onValueChange={(v) => field.onChange(Number(v))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {Object.entries(AssignmentType).map(([k, v]) => <SelectItem key={k} value={k}>{v}</SelectItem>)}
                  </SelectContent>
                </Select>
              )} />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>Start Date</Label>
                <Input type="date" {...createForm.register("startDate")} />
                {createForm.formState.errors.startDate && <p className="text-xs text-destructive">{createForm.formState.errors.startDate.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>End Date (optional)</Label>
                <Input type="date" {...createForm.register("endDate")} />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>Rate Override ($/mo)</Label>
                <Input type="number" step="0.01" {...createForm.register("rateOverride")} placeholder="Leave blank for standard rate" />
              </div>
              <div className="space-y-1.5">
                <Label>Notes</Label>
                <Input {...createForm.register("notes")} />
              </div>
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setCreateOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={createForm.formState.isSubmitting}>
                {createForm.formState.isSubmitting ? "Assigning…" : "Assign"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* End Assignment Dialog */}
      <AlertDialog open={!!endingAssignment} onOpenChange={(v) => { if (!v) setEndingAssignment(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>End Assignment</AlertDialogTitle>
            <AlertDialogDescription>
              End the assignment for <strong>{endingAssignment?.slipName}</strong> ({endingAssignment?.customerDisplayName})?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <div className="px-6 pb-2 space-y-1.5">
            <Label>End Date</Label>
            <Input type="date" {...endForm.register("endDate")} />
            {endForm.formState.errors.endDate && <p className="text-xs text-destructive">{endForm.formState.errors.endDate.message}</p>}
          </div>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={endForm.handleSubmit((v) => endingAssignment && endMut.mutate({ id: endingAssignment.id, endDate: v.endDate }))}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              End Assignment
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
