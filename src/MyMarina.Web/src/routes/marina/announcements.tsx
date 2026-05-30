import { useState } from 'react';
import { useParams } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus } from 'lucide-react';
import {
  getMarinaAnnouncements, createAnnouncement, publishAnnouncement, deleteAnnouncement,
  type AnnouncementDto,
} from '@/api/api';
import { PageHeader, PageBody } from '@/marina-workspace/PageHeader';
import { FilterChip } from '@/components/ui/filter-chip';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';

// ─── Types ────────────────────────────────────────────────────────────────────

type AudienceFilter = 'all' | 'Both' | 'Customers' | 'IncomingBoaters';
type StatusFilter = 'all' | 'draft' | 'published';

const AUDIENCE_CHIPS: { key: AudienceFilter; label: string }[] = [
  { key: 'all', label: 'All' },
  { key: 'Both', label: 'Everyone' },
  { key: 'Customers', label: 'Customers' },
  { key: 'IncomingBoaters', label: 'Boaters' },
];

const STATUS_CHIPS: { key: StatusFilter; label: string }[] = [
  { key: 'all', label: 'All' },
  { key: 'draft', label: 'Draft' },
  { key: 'published', label: 'Published' },
];

// ─── Create dialog ────────────────────────────────────────────────────────────

function CreateAnnouncementDialog({
  marinaId,
  open,
  onOpenChange,
  onCreated,
}: {
  marinaId: string;
  open: boolean;
  onOpenChange: (o: boolean) => void;
  onCreated: () => void;
}) {
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [audience, setAudience] = useState('Both');
  const [pinned, setPinned] = useState(false);
  const [saving, setSaving] = useState(false);

  const inp = 'w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm';

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      await createAnnouncement(marinaId, { title, body, audience, isPinned: pinned });
      setTitle(''); setBody(''); setAudience('Both'); setPinned(false);
      onOpenChange(false);
      onCreated();
    } finally {
      setSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader><DialogTitle>New announcement</DialogTitle></DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="space-y-1">
            <label className="text-sm font-medium">Title</label>
            <input value={title} onChange={(e) => setTitle(e.target.value)}
              className={inp} placeholder="Dock closure notice…" required autoFocus />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium">Body</label>
            <textarea value={body} onChange={(e) => setBody(e.target.value)}
              className={`${inp} h-28 resize-none`} placeholder="Markdown supported…" required />
          </div>
          <div className="flex gap-4 items-center flex-wrap">
            <div className="space-y-1">
              <label className="text-sm font-medium">Audience</label>
              <select value={audience} onChange={(e) => setAudience(e.target.value)}
                className={`${inp} w-44`}>
                <option value="Both">Everyone</option>
                <option value="Customers">Customers only</option>
                <option value="IncomingBoaters">Incoming boaters</option>
              </select>
            </div>
            <label className="flex items-center gap-2 text-sm mt-4">
              <input type="checkbox" checked={pinned} onChange={(e) => setPinned(e.target.checked)} />
              Pin announcement
            </label>
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
            <Button type="submit" disabled={saving}>{saving ? '…' : 'Save as draft'}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─── Announcement card ────────────────────────────────────────────────────────

function AnnouncementCard({
  announcement,
  onPublish,
  onDelete,
}: {
  announcement: AnnouncementDto;
  onPublish: () => void;
  onDelete: () => void;
}) {
  const a = announcement;
  const isPublished = !!a.publishedAt;

  return (
    <div className="rounded-xl border border-border bg-card p-4 space-y-2">
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-start gap-2 min-w-0">
          {a.isPinned && <span className="text-base leading-none" title="Pinned">📌</span>}
          <div className="min-w-0">
            <p className="text-sm font-medium text-foreground truncate">{a.title}</p>
            <div className="flex items-center gap-2 mt-1 flex-wrap">
              <Badge variant={isPublished ? 'success' : 'neutral'} dot>
                {isPublished ? 'Published' : 'Draft'}
              </Badge>
              <span className="text-xs text-muted-foreground">{a.audience}</span>
              {a.publishedAt && (
                <span className="text-xs text-muted-foreground">
                  {new Date(a.publishedAt).toLocaleDateString()}
                </span>
              )}
            </div>
          </div>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          {!isPublished && (
            <Button size="sm" variant="outline" onClick={onPublish}>Publish</Button>
          )}
          <Button size="sm" variant="outline" className="text-destructive hover:bg-destructive/10" onClick={onDelete}>
            Delete
          </Button>
        </div>
      </div>
      <p className="text-xs text-muted-foreground line-clamp-2">{a.body}</p>
    </div>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export function AnnouncementsPage() {
  const { marinaId } = useParams({ strict: false }) as { marinaId: string };
  const qc = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const [audienceFilter, setAudienceFilter] = useState<AudienceFilter>('all');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');

  const { data: announcements = [], isLoading } = useQuery<AnnouncementDto[]>({
    queryKey: ['marina-announcements', marinaId],
    queryFn: () => getMarinaAnnouncements(marinaId),
    staleTime: 30_000,
  });

  const { mutate: doPublish } = useMutation({
    mutationFn: ({ id }: { id: string }) => publishAnnouncement(marinaId, id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['marina-announcements', marinaId] }),
  });

  const { mutate: doDelete } = useMutation({
    mutationFn: ({ id }: { id: string }) => deleteAnnouncement(marinaId, id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['marina-announcements', marinaId] }),
  });

  function invalidate() {
    qc.invalidateQueries({ queryKey: ['marina-announcements', marinaId] });
  }

  const filtered = announcements.filter((a) => {
    const audienceOk = audienceFilter === 'all' || a.audience === audienceFilter;
    const statusOk = statusFilter === 'all'
      || (statusFilter === 'published' && !!a.publishedAt)
      || (statusFilter === 'draft' && !a.publishedAt);
    return audienceOk && statusOk;
  });

  const draftCount = announcements.filter((a) => !a.publishedAt).length;
  const publishedCount = announcements.filter((a) => !!a.publishedAt).length;

  return (
    <>
      <PageHeader
        title="Announcements"
        actions={<Button onClick={() => setCreateOpen(true)}><Plus className="h-4 w-4 mr-1" /> New announcement</Button>}
      />
      <PageBody className="space-y-4">
        {/* Filters */}
        <div className="flex gap-6 flex-wrap">
          <div className="flex gap-2 flex-wrap">
            {STATUS_CHIPS.map(({ key, label }) => {
              const n = key === 'draft' ? draftCount : key === 'published' ? publishedCount : announcements.length;
              return (
                <FilterChip key={key} active={statusFilter === key} count={n > 0 ? n : undefined}
                  onClick={() => setStatusFilter(key)}>
                  {label}
                </FilterChip>
              );
            })}
          </div>
          <div className="flex gap-2 flex-wrap">
            {AUDIENCE_CHIPS.map(({ key, label }) => (
              <FilterChip key={key} active={audienceFilter === key}
                onClick={() => setAudienceFilter(key)}>
                {label}
              </FilterChip>
            ))}
          </div>
        </div>

        {isLoading && (
          <div className="space-y-3">
            {[1, 2, 3].map((i) => <div key={i} className="h-24 bg-muted animate-pulse rounded-xl" />)}
          </div>
        )}

        {!isLoading && filtered.length === 0 && (
          <div className="py-16 text-center">
            <p className="text-sm text-muted-foreground">No announcements found.</p>
            <Button variant="outline" className="mt-4" onClick={() => setCreateOpen(true)}>
              Create your first announcement
            </Button>
          </div>
        )}

        <div className="space-y-3">
          {filtered.map((a) => (
            <AnnouncementCard
              key={a.id}
              announcement={a}
              onPublish={() => doPublish({ id: a.id })}
              onDelete={() => {
                if (window.confirm(`Delete "${a.title}"?`)) doDelete({ id: a.id });
              }}
            />
          ))}
        </div>
      </PageBody>

      <CreateAnnouncementDialog
        marinaId={marinaId}
        open={createOpen}
        onOpenChange={setCreateOpen}
        onCreated={invalidate}
      />
    </>
  );
}
