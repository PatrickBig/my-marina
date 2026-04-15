import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState } from "react";
import { toast } from "sonner";
import { Plus, Pencil, Trash2 } from "lucide-react";
import { getMarinas, getDocks, createDock, updateDock, deleteDock, type DockDto } from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog";

const schema = z.object({
  name: z.string().min(1, "Name is required"),
  description: z.string().optional().nullable(),
  sortOrder: z.preprocess((v) => Number(v), z.number().int().min(0)).default(0),
});
type FormValues = z.infer<typeof schema>;

export function DocksPage() {
  const qc = useQueryClient();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<DockDto | null>(null);
  const [deleting, setDeleting] = useState<DockDto | null>(null);

  const { data: marinas } = useQuery({ queryKey: ["marinas"], queryFn: getMarinas });
  const marinaId = marinas?.[0]?.id;

  const { data: docks = [], isLoading } = useQuery({
    queryKey: ["docks", marinaId],
    queryFn: () => getDocks(marinaId!),
    enabled: !!marinaId,
  });

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormValues, any, FormValues>({
    resolver: zodResolver(schema) as any,
  });

  const saveMut = useMutation({
    mutationFn: async (values: FormValues) => {
      if (editing) { await updateDock(editing.id, values); } else { await createDock(marinaId!, values); }
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["docks", marinaId] });
      toast.success(editing ? "Dock updated" : "Dock created");
      closeDialog();
    },
    onError: () => toast.error("Failed to save dock"),
  });

  const deleteMut = useMutation({
    mutationFn: (id: string) => deleteDock(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["docks", marinaId] });
      toast.success("Dock deleted");
      setDeleting(null);
    },
    onError: () => toast.error("Failed to delete dock"),
  });

  const openCreate = () => { setEditing(null); reset({ name: "", description: "", sortOrder: 0 }); setOpen(true); };
  const openEdit = (d: DockDto) => { setEditing(d); reset({ name: d.name, description: d.description ?? "", sortOrder: Number(d.sortOrder) }); setOpen(true); };
  const closeDialog = () => { setOpen(false); setEditing(null); };

  if (!marinaId) return <div className="p-8 text-muted-foreground">No marina configured yet. Set up your marina profile first.</div>;

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Docks</h1>
        <Button onClick={openCreate}><Plus className="h-4 w-4" /> Add Dock</Button>
      </div>

      {isLoading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : docks.length === 0 ? (
        <p className="text-muted-foreground">No docks yet. Add your first dock to get started.</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Description</TableHead>
              <TableHead>Sort Order</TableHead>
              <TableHead className="w-24"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {docks.map((dock) => (
              <TableRow key={dock.id}>
                <TableCell className="font-medium">{dock.name}</TableCell>
                <TableCell className="text-muted-foreground">{dock.description ?? "—"}</TableCell>
                <TableCell>{String(dock.sortOrder)}</TableCell>
                <TableCell>
                  <div className="flex gap-1">
                    <Button size="icon" variant="ghost" onClick={() => openEdit(dock)}><Pencil className="h-4 w-4" /></Button>
                    <Button size="icon" variant="ghost" onClick={() => setDeleting(dock)} className="text-destructive hover:text-destructive"><Trash2 className="h-4 w-4" /></Button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {/* Create/Edit Dialog */}
      <Dialog open={open} onOpenChange={(v) => { if (!v) closeDialog(); }}>
        <DialogContent>
          <DialogHeader><DialogTitle>{editing ? "Edit Dock" : "Add Dock"}</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit((v) => saveMut.mutate(v))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Name</Label>
              <Input {...register("name")} placeholder="Dock A" />
              {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Description</Label>
              <Input {...register("description")} placeholder="Optional description" />
            </div>
            <div className="space-y-1.5">
              <Label>Sort Order</Label>
              <Input type="number" {...register("sortOrder")} />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={closeDialog}>Cancel</Button>
              <Button type="submit" disabled={isSubmitting}>{isSubmitting ? "Saving…" : "Save"}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Delete Confirm */}
      <AlertDialog open={!!deleting} onOpenChange={(v) => { if (!v) setDeleting(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Dock</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete <strong>{deleting?.name}</strong>? This cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={() => deleting && deleteMut.mutate(deleting.id)} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
