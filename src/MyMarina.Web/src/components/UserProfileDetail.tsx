import { useEffect, useState } from 'react';
import { getUserProfile, forceSignOut, deactivatePlatformUser, activatePlatformUser, type UserProfileDto } from '@/api/api';
import { ChangeEmailDialog } from './ChangeEmailDialog';
import { ChangeNameDialog } from './ChangeNameDialog';
import { PasswordResetDialog } from './PasswordResetDialog';

interface UserProfileDetailProps {
  userId: string;
  onClose: () => void;
  onRefresh: () => void;
}

type DialogType = 'email' | 'name' | 'password' | null;

const btnAction = 'rounded px-2 py-1 text-xs font-medium bg-blue-50 text-blue-600 hover:bg-blue-100 transition-colors';
const btnDanger = 'rounded px-2 py-1 text-xs font-medium bg-red-50 text-red-600 hover:bg-red-100 transition-colors';

export function UserProfileDetail({ userId, onClose, onRefresh }: UserProfileDetailProps) {
  const [profile, setProfile] = useState<UserProfileDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialog, setDialog] = useState<DialogType>(null);

  useEffect(() => {
    async function fetch() {
      try {
        const data = await getUserProfile(userId);
        setProfile(data);
      } catch (err) {
        setError('Failed to load user profile');
      } finally {
        setLoading(false);
      }
    }
    fetch();
  }, [userId]);

  if (loading) {
    return (
      <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
        <div className="bg-card rounded-lg border border-border p-6 w-2xl max-h-96 overflow-auto shadow-lg">
          <p className="text-muted-foreground">Loading profile...</p>
        </div>
      </div>
    );
  }

  if (error || !profile) {
    return (
      <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
        <div className="bg-card rounded-lg border border-border p-6 w-96 shadow-lg">
          <p className="text-red-600">{error || 'User not found'}</p>
          <button onClick={onClose} className="mt-4 px-3 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted">
            Close
          </button>
        </div>
      </div>
    );
  }

  const { user, vessels, reservations, memberships, recentActivity } = profile;

  async function handleForceSignOut() {
    if (!window.confirm('Force sign out this user?')) return;
    await forceSignOut(userId);
    onRefresh();
  }

  async function handleDeactivate() {
    const reason = window.prompt('Reason (optional):') ?? undefined;
    await deactivatePlatformUser(userId, reason);
    setProfile((p) => p ? { ...p, user: { ...p.user, isActive: false } } : null);
    onRefresh();
  }

  async function handleActivate() {
    await activatePlatformUser(userId);
    setProfile((p) => p ? { ...p, user: { ...p.user, isActive: true } } : null);
    onRefresh();
  }

  return (
    <>
      <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
        <div className="bg-card rounded-lg border border-border p-6 w-3xl max-h-[90vh] overflow-auto shadow-lg">
          <div className="flex justify-between items-start mb-6">
            <div>
              <h2 className="text-2xl font-semibold">{user.firstName} {user.lastName}</h2>
              <p className="text-muted-foreground text-sm">{user.email}</p>
            </div>
            <button onClick={onClose} className="text-muted-foreground hover:text-foreground">✕</button>
          </div>

          {/* Account Details */}
          <div className="space-y-6">
            <section>
              <h3 className="text-sm font-semibold mb-3">Account Details</h3>
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-muted-foreground">Status:</span>
                  <span className={`ml-2 px-2 py-0.5 rounded-full text-xs ${
                    user.isActive ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'
                  }`}>
                    {user.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
                <div>
                  <span className="text-muted-foreground">Email Confirmed:</span>
                  <span className="ml-2">{user.emailConfirmed ? 'Yes' : 'No'}</span>
                </div>
                <div>
                  <span className="text-muted-foreground">Created:</span>
                  <span className="ml-2">{new Date(user.createdAt).toLocaleDateString()}</span>
                </div>
                <div>
                  <span className="text-muted-foreground">Last Login:</span>
                  <span className="ml-2">{user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : 'Never'}</span>
                </div>
              </div>

              <div className="flex gap-2 mt-4">
                <button onClick={() => setDialog('email')} className={btnAction}>Change Email</button>
                <button onClick={() => setDialog('name')} className={btnAction}>Change Name</button>
                <button onClick={() => setDialog('password')} className={btnAction}>Reset Password</button>
                <button onClick={handleForceSignOut} className={btnAction}>Force Sign Out</button>
                {user.isActive
                  ? <button onClick={handleDeactivate} className={btnDanger}>Deactivate</button>
                  : <button onClick={handleActivate} className={btnAction}>Activate</button>
                }
              </div>
            </section>

            {/* Vessels */}
            {vessels.length > 0 && (
              <section>
                <h3 className="text-sm font-semibold mb-2">Vessels ({vessels.length})</h3>
                <div className="space-y-2">
                  {vessels.map((v) => (
                    <div key={v.id} className="text-sm p-2 bg-muted/30 rounded">
                      <div className="font-medium">{v.name}</div>
                      <div className="text-muted-foreground text-xs">
                        {v.boatType} • {v.length}ft L × {v.beam}ft B × {v.draft}ft D
                      </div>
                    </div>
                  ))}
                </div>
              </section>
            )}

            {/* Reservations */}
            {reservations.length > 0 && (
              <section>
                <h3 className="text-sm font-semibold mb-2">Reservations ({reservations.length})</h3>
                <div className="space-y-2">
                  {reservations.slice(0, 5).map((r) => (
                    <div key={r.id} className="text-sm p-2 bg-muted/30 rounded">
                      <div className="font-medium">{r.slipName} @ {r.marinaName}</div>
                      <div className="text-muted-foreground text-xs">
                        {new Date(r.arrivesAt).toLocaleDateString()} – {new Date(r.departsAt).toLocaleDateString()}
                        {' '} • ${r.total.toFixed(2)} • {r.status}
                      </div>
                    </div>
                  ))}
                </div>
              </section>
            )}

            {/* Memberships */}
            {memberships.length > 0 && (
              <section>
                <h3 className="text-sm font-semibold mb-2">Memberships ({memberships.length})</h3>
                <div className="space-y-2">
                  {memberships.map((m) => (
                    <div key={m.id} className="text-sm p-2 bg-muted/30 rounded">
                      <div className="font-medium">{m.scope} • {m.role}</div>
                      <div className="text-muted-foreground text-xs">
                        {m.isPending ? 'Pending' : 'Active'} • {new Date(m.invitedAt).toLocaleDateString()}
                      </div>
                    </div>
                  ))}
                </div>
              </section>
            )}

            {/* Recent Activity */}
            {recentActivity.length > 0 && (
              <section>
                <h3 className="text-sm font-semibold mb-2">Recent Activity</h3>
                <div className="space-y-1 text-xs">
                  {recentActivity.slice(0, 10).map((a) => (
                    <div key={a.id} className="text-muted-foreground">
                      <span>{a.action}</span>
                      {a.actorName && <span className="ml-2">by {a.actorName}</span>}
                      {a.details && <span className="ml-2 text-muted-foreground/70">{a.details}</span>}
                      <span className="ml-2 text-muted-foreground/50">{new Date(a.occurredAt).toLocaleString()}</span>
                    </div>
                  ))}
                </div>
              </section>
            )}
          </div>

          <button onClick={onClose} className="mt-6 w-full px-3 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted">
            Close
          </button>
        </div>
      </div>

      {/* Dialogs */}
      {dialog === 'email' && (
        <ChangeEmailDialog
          userId={userId}
          currentEmail={user.email}
          userName={`${user.firstName} ${user.lastName}`}
          onSuccess={() => {
            setDialog(null);
            onRefresh();
          }}
          onClose={() => setDialog(null)}
        />
      )}
      {dialog === 'name' && (
        <ChangeNameDialog
          userId={userId}
          currentFirstName={user.firstName}
          currentLastName={user.lastName}
          onSuccess={() => {
            setDialog(null);
            onRefresh();
          }}
          onClose={() => setDialog(null)}
        />
      )}
      {dialog === 'password' && (
        <PasswordResetDialog
          userId={userId}
          userName={`${user.firstName} ${user.lastName}`}
          userEmail={user.email}
          onSuccess={() => {
            setDialog(null);
            onRefresh();
          }}
          onClose={() => setDialog(null)}
        />
      )}
    </>
  );
}
