import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState } from "react";
import { toast } from "sonner";
import { Plus, Megaphone, Pin, Globe, FileX } from "lucide-react";
import {
  getMarinas, getAnnouncements, createAnnouncement, updateAnnouncement,
  publishAnnouncement, unpublishAnnouncement, deleteAnnouncement,
  type AnnouncementDto,
} from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Textarea } from "@/components/ui/textarea";
import { Checkbox } from "@/components/ui/checkbox";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel,
  AlertDialogContent, AlertDialogDescription, AlertDialogFooter,
  AlertDialogHeader, AlertDialogTitle,
} from "@/components/ui/alert-dialog";

const formSchema = z.object({
  title: z.string().min(1, "Title is required"),
  body: z.string().min(1, "Body is required"),
  isPinned: z.boolean(),
  expiresAt: z.string().optional().nullable(),
});
type FormValues = z.infer<typeof formSchema>;

function fmtDate(iso: string | null) {
  if (!iso) return "—";
  return new Date(iso).toLocaleDateString();
}

export function AnnouncementsPage() {
  const qc = useQueryClient();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<AnnouncementDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<AnnouncementDto | null>(null);

  const { data: marinas } = useQuery({ queryKey: ["marinas"], queryFn: getMarinas });
  const marinaId = marinas?.[0]?.id;

  const { data: announcements = [], isLoading } = useQuery({
    queryKey: ["announcements", marinaId],
    queryFn: () => getAnnouncements(marinaId!, { includeDrafts: true, includeExpired: true }),
    enabled: !!marinaId,
  });

  const { register, handleSubmit, reset, setValue, watch, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: { title: "", body: "", isPinned: false, expiresAt: "" },
  });

  const isPinned = watch("isPinned");

  const openCreate = () => {
    setEditing(null);
    reset({ title: "", body: "", isPinned: false, expiresAt: "" });
    setDialogOpen(true);
  };

  const openEdit = (a: AnnouncementDto) => {
    setEditing(a);
    reset({
      title: a.title,
      body: a.body,
      isPinned: a.isPinned,
      expiresAt: a.expiresAt ? new Date(a.expiresAt).toISOString().slice(0, 16) : "",
    });
    setDialogOpen(true);
  };

  const saveMut = useMutation({
    mutationFn: async (v: FormValues) => {
      const payload = {
        title: v.title,
        body: v.body,
        isPinned: v.isPinned,
        expiresAt: v.expiresAt || null,
      };
      if (editing) {
        await updateAnnouncement(marinaId!, editing.id, payload);
      } else {
        await createAnnouncement(marinaId!, { ...payload, publish: false });
      }
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["announcements"] });
      toast.success(editing ? "Announcement updated" : "Announcement created as draft");
      setDialogOpen(false);
      reset();
    },
    onError: () => toast.error("Failed to save announcement"),
  });

  const publishMut = useMutation({
    mutationFn: (a: AnnouncementDto) => publishAnnouncement(marinaId!, a.id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["announcements"] });
      toast.success("Announcement published");
    },
    onError: () => toast.error("Failed to publish"),
  });

  const unpublishMut = useMutation({
    mutationFn: (a: AnnouncementDto) => unpublishAnnouncement(marinaId!, a.id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["announcements"] });
      toast.success("Announcement unpublished");
    },
    onError: () => toast.error("Failed to unpublish"),
  });

  const deleteMut = useMutation({
    mutationFn: (a: AnnouncementDto) => deleteAnnouncement(marinaId!, a.id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["announcements"] });
      toast.success("Announcement deleted");
      setDeleteTarget(null);
    },
    onError: () => toast.error("Failed to delete"),
  });

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Announcements</h1>
        <Button onClick={openCreate} disabled={!marinaId}>
          <Plus className="h-4 w-4" /> New Announcement
        </Button>
      </div>

      {isLoading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : announcements.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-muted-foreground gap-3">
          <Megaphone className="h-10 w-10 opacity-30" />
          <p>No announcements yet.</p>
        </div>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Title</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Pinned</TableHead>
              <TableHead>Published</TableHead>
              <TableHead>Expires</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {announcements.map((a) => (
              <TableRow key={a.id}>
                <TableCell className="font-medium max-w-xs truncate">{a.title}</TableCell>
                <TableCell>
                  {a.isPublished ? (
                    <Badge variant="success">Published</Badge>
                  ) : (
                    <Badge variant="secondary">Draft</Badge>
                  )}
                </TableCell>
                <TableCell>
                  {a.isPinned && <Pin className="h-4 w-4 text-primary" />}
                </TableCell>
                <TableCell className="text-muted-foreground text-sm">{fmtDate(a.publishedAt)}</TableCell>
                <TableCell className="text-muted-foreground text-sm">{fmtDate(a.expiresAt)}</TableCell>
                <TableCell className="text-right space-x-2">
                  <Button size="sm" variant="outline" onClick={() => openEdit(a)}>
                    Edit
                  </Button>
                  {a.isPublished ? (
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => unpublishMut.mutate(a)}
                      disabled={unpublishMut.isPending}
                    >
                      <FileX className="h-3 w-3 mr-1" /> Unpublish
                    </Button>
                  ) : (
                    <Button
                      size="sm"
                      onClick={() => publishMut.mutate(a)}
                      disabled={publishMut.isPending}
                    >
                      <Globe className="h-3 w-3 mr-1" /> Publish
                    </Button>
                  )}
                  <Button
                    size="sm"
                    variant="destructive"
                    onClick={() => setDeleteTarget(a)}
                  >
                    Delete
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {/* Create / Edit Dialog */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{editing ? "Edit Announcement" : "New Announcement"}</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit((v) => saveMut.mutate(v))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Title</Label>
              <Input {...register("title")} placeholder="Announcement title…" />
              {errors.title && <p className="text-xs text-destructive">{errors.title.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Body</Label>
              <Textarea
                {...register("body")}
                placeholder="Announcement body…"
                rows={6}
                className="resize-none"
              />
              {errors.body && <p className="text-xs text-destructive">{errors.body.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>Expires At (optional)</Label>
                <Input type="datetime-local" {...register("expiresAt")} />
              </div>
              <div className="flex items-center gap-2 pt-6">
                <Checkbox
                  id="isPinned"
                  checked={isPinned}
                  onCheckedChange={(v: boolean | "indeterminate") => setValue("isPinned", v === true)}
                />
                <Label htmlFor="isPinned">Pin this announcement</Label>
              </div>
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? "Saving…" : editing ? "Save Changes" : "Create Draft"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Delete Confirm */}
      <AlertDialog open={!!deleteTarget} onOpenChange={(open) => { if (!open) setDeleteTarget(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Announcement?</AlertDialogTitle>
            <AlertDialogDescription>
              "{deleteTarget?.title}" will be permanently deleted.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => deleteTarget && deleteMut.mutate(deleteTarget)}
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
