import { useEffect, useState } from 'react';
import { NavBar } from '@/components/NavBar';
import {
  getMyMaintenanceRequests, submitMaintenanceRequest, getMyAnnouncements,
  type MaintenanceRequestDto, type AnnouncementDto,
} from '@/api/api';
import { useAuthStore } from '@/store/authStore';

const STATUS_COLORS: Record<string, string> = {
  Submitted:   'bg-blue-50 text-blue-700',
  UnderReview: 'bg-yellow-50 text-yellow-700',
  InProgress:  'bg-orange-50 text-orange-700',
  Completed:   'bg-green-50 text-green-700',
  Declined:    'bg-red-50 text-red-700',
};

const PRIORITY_COLORS: Record<string, string> = {
  Low:    'text-muted-foreground/70',
  Medium: 'text-blue-500',
  High:   'text-orange-500',
  Urgent: 'text-red-600 font-bold',
};

const input = 'w-full rounded-lg border border-border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring';
const btn   = 'rounded-lg bg-foreground text-white px-4 py-2 text-sm font-medium hover:bg-foreground/90 disabled:opacity-50 transition-colors';

export function MaintenancePage() {
  const { marinaMemberships } = useAuthStore();
  const [requests, setRequests]     = useState<MaintenanceRequestDto[]>([]);
  const [announcements, setAnnouncements] = useState<AnnouncementDto[]>([]);
  const [showForm, setShowForm]     = useState(false);
  const [selectedMarinaId, setSelectedMarinaId] = useState('');
  const [title, setTitle]           = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority]     = useState('Medium');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError]           = useState<string | null>(null);

  const marinas = marinaMemberships();

  useEffect(() => {
    getMyMaintenanceRequests().then(setRequests).catch(() => {});
    getMyAnnouncements().then(setAnnouncements).catch(() => {});
  }, []);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedMarinaId) return;
    setSubmitting(true);
    setError(null);
    try {
      const r = await submitMaintenanceRequest(selectedMarinaId, { title, description, priority });
      setRequests((prev) => [r, ...prev]);
      setShowForm(false);
      setTitle(''); setDescription(''); setPriority('Medium'); setSelectedMarinaId('');
    } catch {
      setError('Failed to submit request.');
    } finally {
      setSubmitting(false);
    }
  }

  const pinnedAnnouncements  = announcements.filter((a) => a.isPinned);
  const regularAnnouncements = announcements.filter((a) => !a.isPinned);

  return (
    <div className="min-h-screen bg-muted/30">
      <NavBar />
      <div className="max-w-4xl mx-auto px-4 py-8 space-y-8">

        {/* Announcements */}
        {announcements.length > 0 && (
          <section>
            <h2 className="text-lg font-semibold text-foreground mb-3">Announcements</h2>
            <div className="space-y-3">
              {[...pinnedAnnouncements, ...regularAnnouncements].map((a) => (
                <div key={a.id} className="bg-card rounded-xl border border-border p-4">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      {a.isPinned && (
                        <span className="text-xs text-muted-foreground/70 mr-2">📌</span>
                      )}
                      <span className="text-sm font-semibold text-foreground">{a.title}</span>
                      <span className="ml-2 text-xs text-muted-foreground/70">
                        {a.audience !== 'Both' ? `· ${a.audience}` : ''}
                      </span>
                    </div>
                    <span className="text-xs text-muted-foreground/70 shrink-0">
                      {a.publishedAt ? new Date(a.publishedAt).toLocaleDateString() : ''}
                    </span>
                  </div>
                  <p className="text-sm text-foreground/80 mt-1 whitespace-pre-wrap">{a.body}</p>
                </div>
              ))}
            </div>
          </section>
        )}

        {/* Maintenance requests */}
        <section>
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-lg font-semibold text-foreground">My Maintenance Requests</h2>
            <button onClick={() => setShowForm((v) => !v)} className={btn}>
              {showForm ? 'Cancel' : 'Submit request'}
            </button>
          </div>

          {showForm && (
            <form onSubmit={handleSubmit} className="bg-card rounded-xl border border-border p-4 mb-4 space-y-3">
              <div>
                <label className="block text-xs font-medium text-foreground/80 mb-1">Marina</label>
                <select value={selectedMarinaId} onChange={(e) => setSelectedMarinaId(e.target.value)}
                  className={input} required>
                  <option value="">Select a marina…</option>
                  {marinas.filter((m) => m.marinaId).map((m) => (
                    <option key={m.marinaId!} value={m.marinaId!}>{m.marinaId}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-foreground/80 mb-1">Title</label>
                <input value={title} onChange={(e) => setTitle(e.target.value)}
                  className={input} placeholder="Brief description of the issue" required />
              </div>
              <div>
                <label className="block text-xs font-medium text-foreground/80 mb-1">Description</label>
                <textarea value={description} onChange={(e) => setDescription(e.target.value)}
                  className={`${input} h-24 resize-none`} placeholder="Details…" required />
              </div>
              <div>
                <label className="block text-xs font-medium text-foreground/80 mb-1">Priority</label>
                <select value={priority} onChange={(e) => setPriority(e.target.value)} className={`${input} w-32`}>
                  {['Low','Medium','High','Urgent'].map((p) => <option key={p}>{p}</option>)}
                </select>
              </div>
              {error && <p className="text-sm text-red-600">{error}</p>}
              <button type="submit" disabled={submitting} className={btn}>
                {submitting ? 'Submitting…' : 'Submit'}
              </button>
            </form>
          )}

          {requests.length === 0 ? (
            <p className="text-sm text-muted-foreground/70">No maintenance requests yet.</p>
          ) : (
            <div className="space-y-3">
              {requests.map((r) => (
                <div key={r.id} className="bg-card rounded-xl border border-border p-4">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <p className="text-sm font-semibold text-foreground">{r.title}</p>
                      <p className="text-xs text-muted-foreground mt-0.5">{r.description}</p>
                    </div>
                    <div className="flex flex-col items-end gap-1 shrink-0">
                      <span className={`text-xs px-2 py-0.5 rounded-full ${STATUS_COLORS[r.status] ?? 'bg-muted text-foreground/80'}`}>
                        {r.status}
                      </span>
                      <span className={`text-xs ${PRIORITY_COLORS[r.priority] ?? ''}`}>{r.priority}</span>
                    </div>
                  </div>
                  <p className="text-xs text-muted-foreground/70 mt-2">
                    Submitted {new Date(r.submittedAt).toLocaleDateString()}
                    {r.workOrder && ` · Work order: ${r.workOrder.status}`}
                  </p>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
