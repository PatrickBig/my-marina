import { useState } from 'react';
import { changeUserEmail } from '@/api/api';

interface ChangeEmailDialogProps {
  userId: string;
  currentEmail: string;
  userName: string;
  onSuccess: () => void;
  onClose: () => void;
}

export function ChangeEmailDialog({ userId, currentEmail, userName, onSuccess, onClose }: ChangeEmailDialogProps) {
  const [newEmail, setNewEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [step, setStep] = useState<'form' | 'confirm'>('form');

  const isValidEmail = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(newEmail);

  async function handleConfirm() {
    setLoading(true);
    setError(null);
    try {
      await changeUserEmail(userId, newEmail);
      onSuccess();
      onClose();
    } catch (err: any) {
      setError(err?.response?.data?.message || 'Failed to change email');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-card rounded-lg border border-border p-6 w-96 shadow-lg">
        {step === 'form' ? (
          <>
            <h3 className="text-lg font-semibold mb-4">Change Email for {userName}</h3>
            <div className="space-y-4">
              <div>
                <label className="text-sm font-medium text-muted-foreground">Current Email</label>
                <div className="text-foreground text-sm mt-1 p-2 bg-muted rounded">{currentEmail}</div>
              </div>
              <div>
                <label className="text-sm font-medium text-muted-foreground">New Email</label>
                <input
                  type="email"
                  value={newEmail}
                  onChange={(e) => setNewEmail(e.target.value)}
                  className="w-full rounded-lg border border-border px-3 py-2 text-sm mt-1 focus:outline-none focus:ring-2 focus:ring-ring"
                  placeholder="Enter new email address"
                />
              </div>
              {error && <div className="text-sm text-red-600 bg-red-50 p-2 rounded">{error}</div>}
            </div>
            <div className="flex gap-3 mt-6">
              <button
                onClick={onClose}
                className="flex-1 px-3 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={() => setStep('confirm')}
                disabled={!isValidEmail || loading}
                className="flex-1 px-3 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 disabled:opacity-50 transition-colors"
              >
                Continue
              </button>
            </div>
          </>
        ) : (
          <>
            <h3 className="text-lg font-semibold mb-4">Confirm Email Change</h3>
            <p className="text-sm text-muted-foreground mb-4">
              Change email for <strong>{userName}</strong> from <strong>{currentEmail}</strong> to <strong>{newEmail}</strong>?
            </p>
            <div className="flex gap-3">
              <button
                onClick={() => setStep('form')}
                className="flex-1 px-3 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors"
              >
                Back
              </button>
              <button
                onClick={handleConfirm}
                disabled={loading}
                className="flex-1 px-3 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 disabled:opacity-50 transition-colors"
              >
                {loading ? 'Changing...' : 'Change Email'}
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
