import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState } from "react";
import { toast } from "sonner";
import { Plus, Pencil, Trash2, Search } from "lucide-react";
import { getMarinas, getDocks, getSlips, createSlip, updateSlip, deleteSlip, getAvailableSlips, type SlipDto, type DockDto } from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";

// Enum values matching the backend
const SlipType = { 0: "Floating", 1: "Fixed", 2: "Mooring" } as const;
const SlipStatus = { 0: "Available", 1: "Occupied", 2: "Reserved", 3: "OutOfService" } as const;

const slipSchema = z.object({
  dockId: z.string().optional().nullable(),
  name: z.string().min(1, "Name is required"),
  slipType: z.number(),
  maxLength: z.preprocess((v) => Number(v), z.number().positive()),
  maxBeam: z.preprocess((v) => Number(v), z.number().positive()),
  maxDraft: z.preprocess((v) => Number(v), z.number().positive()),
  hasElectric: z.boolean(),
  electric: z.number().optional().nullable(),
  hasWater: z.boolean(),
  rateType: z.number(),
  dailyRate: z.preprocess((v) => (v === "" || v === null || v === undefined ? null : Number(v)), z.number().nullable()),
  monthlyRate: z.preprocess((v) => (v === "" || v === null || v === undefined ? null : Number(v)), z.number().nullable()),
  annualRate: z.preprocess((v) => (v === "" || v === null || v === undefined ? null : Number(v)), z.number().nullable()),
  status: z.number(),
  notes: z.string().optional().nullable(),
});
type SlipFormValues = z.infer<typeof slipSchema>;

const availSchema = z.object({
  boatLength: z.preprocess((v) => Number(v), z.number().positive("Required")),
  boatBeam: z.preprocess((v) => Number(v), z.number().positive("Required")),
  boatDraft: z.preprocess((v) => Number(v), z.number().positive("Required")),
  startDate: z.string().min(1, "Required"),
});
type AvailValues = z.infer<typeof availSchema>;

function statusBadge(status: number) {
  const variants: Record<number, "success" | "destructive" | "warning" | "secondary"> = {
    0: "success", 1: "destructive", 2: "warning", 3: "secondary",
  };
  return <Badge variant={variants[status] ?? "outline"}>{SlipStatus[status as keyof typeof SlipStatus] ?? status}</Badge>;
}

export function SlipsPage() {
  const qc = useQueryClient();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<SlipDto | null>(null);
  const [deleting, setDeleting] = useState<SlipDto | null>(null);
  const [availOpen, setAvailOpen] = useState(false);
  const [availResults, setAvailResults] = useState<SlipDto[] | null>(null);

  const { data: marinas } = useQuery({ queryKey: ["marinas"], queryFn: getMarinas });
  const marinaId = marinas?.[0]?.id;

  const { data: docks = [] } = useQuery<DockDto[]>({
    queryKey: ["docks", marinaId],
    queryFn: () => getDocks(marinaId!),
    enabled: !!marinaId,
  });

  const { data: slips = [], isLoading } = useQuery({
    queryKey: ["slips", marinaId],
    queryFn: () => getSlips(marinaId!),
    enabled: !!marinaId,
  });

  const slipForm = useForm<SlipFormValues, any, SlipFormValues>({ resolver: zodResolver(slipSchema) as any });
  const availForm = useForm<AvailValues, any, AvailValues>({ resolver: zodResolver(availSchema) as any });

  const saveMut = useMutation({
    mutationFn: async (values: SlipFormValues) => {
      const payload = { ...values, dockId: values.dockId || null, notes: values.notes || null };
      if (editing) { await updateSlip(editing.id, payload as any); } else { await createSlip(marinaId!, payload as any); }
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["slips", marinaId] });
      toast.success(editing ? "Slip updated" : "Slip created");
      closeDialog();
    },
    onError: () => toast.error("Failed to save slip"),
  });

  const deleteMut = useMutation({
    mutationFn: (id: string) => deleteSlip(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["slips", marinaId] });
      toast.success("Slip deleted");
      setDeleting(null);
    },
    onError: () => toast.error("Failed to delete slip"),
  });

  const openCreate = () => {
    setEditing(null);
    slipForm.reset({ name: "", slipType: 0, maxLength: undefined, maxBeam: undefined, maxDraft: undefined, hasElectric: false, electric: null, hasWater: true, rateType: 0, status: 0, notes: "" });
    setOpen(true);
  };
  const openEdit = (s: SlipDto) => {
    setEditing(s);
    slipForm.reset({ ...s, dockId: s.dockId ?? null, maxLength: Number(s.maxLength), maxBeam: Number(s.maxBeam), maxDraft: Number(s.maxDraft), dailyRate: s.dailyRate ? Number(s.dailyRate) : null, monthlyRate: s.monthlyRate ? Number(s.monthlyRate) : null, annualRate: s.annualRate ? Number(s.annualRate) : null, electric: s.electric ?? null, notes: s.notes ?? "" });
    setOpen(true);
  };
  const closeDialog = () => { setOpen(false); setEditing(null); };

  const onAvailSubmit = async (values: AvailValues) => {
    const results = await getAvailableSlips(marinaId!, values);
    setAvailResults(results);
  };

  if (!marinaId) return <div className="p-8 text-muted-foreground">No marina configured yet.</div>;

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Slips</h1>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => setAvailOpen(true)}><Search className="h-4 w-4" /> Check Availability</Button>
          <Button onClick={openCreate}><Plus className="h-4 w-4" /> Add Slip</Button>
        </div>
      </div>

      {isLoading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : slips.length === 0 ? (
        <p className="text-muted-foreground">No slips yet.</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Dock</TableHead>
              <TableHead>Type</TableHead>
              <TableHead>Max L / B / D</TableHead>
              <TableHead>Monthly Rate</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="w-24"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {slips.map((slip) => (
              <TableRow key={slip.id}>
                <TableCell className="font-medium">{slip.name}</TableCell>
                <TableCell className="text-muted-foreground">{docks.find(d => d.id === slip.dockId)?.name ?? "—"}</TableCell>
                <TableCell>{SlipType[slip.slipType as keyof typeof SlipType] ?? slip.slipType}</TableCell>
                <TableCell>{Number(slip.maxLength)}′ / {Number(slip.maxBeam)}′ / {Number(slip.maxDraft)}′</TableCell>
                <TableCell>{slip.monthlyRate ? `$${Number(slip.monthlyRate).toLocaleString()}` : "—"}</TableCell>
                <TableCell>{statusBadge(slip.status as number)}</TableCell>
                <TableCell>
                  <div className="flex gap-1">
                    <Button size="icon" variant="ghost" onClick={() => openEdit(slip)}><Pencil className="h-4 w-4" /></Button>
                    <Button size="icon" variant="ghost" onClick={() => setDeleting(slip)} className="text-destructive hover:text-destructive"><Trash2 className="h-4 w-4" /></Button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {/* Availability Checker */}
      <Dialog open={availOpen} onOpenChange={setAvailOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader><DialogTitle>Check Slip Availability</DialogTitle></DialogHeader>
          <form onSubmit={availForm.handleSubmit(onAvailSubmit)} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5"><Label>Boat Length (ft)</Label><Input type="number" step="0.1" {...availForm.register("boatLength")} /></div>
              <div className="space-y-1.5"><Label>Boat Beam (ft)</Label><Input type="number" step="0.1" {...availForm.register("boatBeam")} /></div>
              <div className="space-y-1.5"><Label>Boat Draft (ft)</Label><Input type="number" step="0.1" {...availForm.register("boatDraft")} /></div>
              <div className="space-y-1.5"><Label>Start Date</Label><Input type="date" {...availForm.register("startDate")} /></div>
            </div>
            <Button type="submit"><Search className="h-4 w-4" /> Search</Button>
          </form>
          {availResults !== null && (
            <>
              <Separator />
              {availResults.length === 0 ? (
                <p className="text-muted-foreground text-sm">No available slips match those dimensions.</p>
              ) : (
                <Table>
                  <TableHeader><TableRow><TableHead>Slip</TableHead><TableHead>Max L/B/D</TableHead><TableHead>Monthly</TableHead></TableRow></TableHeader>
                  <TableBody>
                    {availResults.map(s => (
                      <TableRow key={s.id}>
                        <TableCell>{s.name}</TableCell>
                        <TableCell>{Number(s.maxLength)}′ / {Number(s.maxBeam)}′ / {Number(s.maxDraft)}′</TableCell>
                        <TableCell>{s.monthlyRate ? `$${Number(s.monthlyRate).toLocaleString()}` : "—"}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </>
          )}
        </DialogContent>
      </Dialog>

      {/* Create/Edit Slip Dialog */}
      <Dialog open={open} onOpenChange={(v) => { if (!v) closeDialog(); }}>
        <DialogContent className="max-w-xl max-h-[90vh] overflow-y-auto">
          <DialogHeader><DialogTitle>{editing ? "Edit Slip" : "Add Slip"}</DialogTitle></DialogHeader>
          <form onSubmit={slipForm.handleSubmit((v) => saveMut.mutate(v))} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>Name</Label>
                <Input {...slipForm.register("name")} placeholder="A-01" />
                {slipForm.formState.errors.name && <p className="text-xs text-destructive">{slipForm.formState.errors.name.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Dock (optional)</Label>
                <Controller control={slipForm.control} name="dockId" render={({ field }) => (
                  <Select value={field.value ?? ""} onValueChange={(v) => field.onChange(v || null)}>
                    <SelectTrigger><SelectValue placeholder="No dock" /></SelectTrigger>
                    <SelectContent>
                      <SelectItem value="">No dock</SelectItem>
                      {docks.map(d => <SelectItem key={d.id} value={d.id}>{d.name}</SelectItem>)}
                    </SelectContent>
                  </Select>
                )} />
              </div>
              <div className="space-y-1.5">
                <Label>Slip Type</Label>
                <Controller control={slipForm.control} name="slipType" render={({ field }) => (
                  <Select value={String(field.value)} onValueChange={(v) => field.onChange(Number(v))}>
                    <SelectTrigger><SelectValue /></SelectTrigger>
                    <SelectContent>
                      {Object.entries(SlipType).map(([k, v]) => <SelectItem key={k} value={k}>{v}</SelectItem>)}
                    </SelectContent>
                  </Select>
                )} />
              </div>
              <div className="space-y-1.5">
                <Label>Status</Label>
                <Controller control={slipForm.control} name="status" render={({ field }) => (
                  <Select value={String(field.value)} onValueChange={(v) => field.onChange(Number(v))}>
                    <SelectTrigger><SelectValue /></SelectTrigger>
                    <SelectContent>
                      {Object.entries(SlipStatus).map(([k, v]) => <SelectItem key={k} value={k}>{v}</SelectItem>)}
                    </SelectContent>
                  </Select>
                )} />
              </div>
            </div>
            <div className="grid grid-cols-3 gap-4">
              <div className="space-y-1.5"><Label>Max Length (ft)</Label><Input type="number" step="0.1" {...slipForm.register("maxLength")} /></div>
              <div className="space-y-1.5"><Label>Max Beam (ft)</Label><Input type="number" step="0.1" {...slipForm.register("maxBeam")} /></div>
              <div className="space-y-1.5"><Label>Max Draft (ft)</Label><Input type="number" step="0.1" {...slipForm.register("maxDraft")} /></div>
            </div>
            <div className="grid grid-cols-3 gap-4">
              <div className="space-y-1.5"><Label>Daily Rate ($)</Label><Input type="number" step="0.01" {...slipForm.register("dailyRate")} /></div>
              <div className="space-y-1.5"><Label>Monthly Rate ($)</Label><Input type="number" step="0.01" {...slipForm.register("monthlyRate")} /></div>
              <div className="space-y-1.5"><Label>Annual Rate ($)</Label><Input type="number" step="0.01" {...slipForm.register("annualRate")} /></div>
            </div>
            <div className="flex gap-6">
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <input type="checkbox" {...slipForm.register("hasWater")} className="rounded" /> Has Water
              </label>
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <input type="checkbox" {...slipForm.register("hasElectric")} className="rounded" /> Has Electric
              </label>
            </div>
            <div className="space-y-1.5">
              <Label>Notes</Label>
              <Input {...slipForm.register("notes")} placeholder="Optional notes" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={closeDialog}>Cancel</Button>
              <Button type="submit" disabled={slipForm.formState.isSubmitting}>{slipForm.formState.isSubmitting ? "Saving…" : "Save"}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Delete Confirm */}
      <AlertDialog open={!!deleting} onOpenChange={(v) => { if (!v) setDeleting(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Slip</AlertDialogTitle>
            <AlertDialogDescription>Delete <strong>{deleting?.name}</strong>? This cannot be undone.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={() => deleting && deleteMut.mutate(deleting.id)} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">Delete</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
