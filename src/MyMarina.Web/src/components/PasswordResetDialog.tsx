import { useState } from 'react';
import { initiatePasswordReset } from '@/api/api';

interface PasswordResetDialogProps {
  userId: string;
  userName: string;
  userEmail: string;
  onSuccess: () => void;
  onClose: () => void;
}

export function PasswordResetDialog({
  userId,
  userName,
  userEmail,
  onSuccess,
  onClose,
}: PasswordResetDialogProps) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleConfirm() {
    setLoading(true);
    setError(null);
    try {
      await initiatePasswordReset(userId);
      onSuccess();
    } catch (err: any) {
      setError(err?.response?.data?.message || 'Failed to send password reset email');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-card rounded-lg border border-border p-6 w-96 shadow-lg">
        <h3 className="text-lg font-semibold mb-4">Send Password Reset</h3>
        <div className="space-y-4">
          <div className="bg-yellow-50 border border-yellow-200 rounded p-3">
            <p className="text-sm text-yellow-800">
              <strong>Warning:</strong> This will invalidate all current sessions for {userName}.
            </p>
          </div>
          <p className="text-sm text-muted-foreground">
            Send a password reset email to <strong>{userEmail}</strong>?
          </p>
          {error && <div className="text-sm text-red-600 bg-red-50 p-2 rounded">{error}</div>}
        </div>
        <div className="flex gap-3 mt-6">
          <button
            onClick={onClose}
            disabled={loading}
            className="flex-1 px-3 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted disabled:opacity-50 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleConfirm}
            disabled={loading}
            className="flex-1 px-3 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 disabled:opacity-50 transition-colors"
          >
            {loading ? 'Sending...' : 'Send Reset Email'}
          </button>
        </div>
      </div>
    </div>
  );
}
