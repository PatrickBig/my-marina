import { useState } from 'react';
import { changeUserName } from '@/api/api';

interface ChangeNameDialogProps {
  userId: string;
  currentFirstName: string;
  currentLastName: string;
  onSuccess: () => void;
  onClose: () => void;
}

export function ChangeNameDialog({
  userId,
  currentFirstName,
  currentLastName,
  onSuccess,
  onClose,
}: ChangeNameDialogProps) {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [step, setStep] = useState<'form' | 'confirm'>('form');

  const hasChanges = firstName.trim() !== '' || lastName.trim() !== '';
  const newFullName = `${firstName || currentFirstName} ${lastName || currentLastName}`;

  async function handleConfirm() {
    setLoading(true);
    setError(null);
    try {
      await changeUserName(
        userId,
        firstName.trim() || undefined,
        lastName.trim() || undefined
      );
      onSuccess();
      onClose();
    } catch (err: any) {
      setError(err?.response?.data?.message || 'Failed to change name');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-card rounded-lg border border-border p-6 w-96 shadow-lg">
        {step === 'form' ? (
          <>
            <h3 className="text-lg font-semibold mb-4">Change Name</h3>
            <div className="space-y-4">
              <div>
                <label className="text-sm font-medium text-muted-foreground">Current Name</label>
                <div className="text-foreground text-sm mt-1 p-2 bg-muted rounded">
                  {currentFirstName} {currentLastName}
                </div>
              </div>
              <div>
                <label className="text-sm font-medium text-muted-foreground">First Name</label>
                <input
                  type="text"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  placeholder={currentFirstName}
                  className="w-full rounded-lg border border-border px-3 py-2 text-sm mt-1 focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
              <div>
                <label className="text-sm font-medium text-muted-foreground">Last Name</label>
                <input
                  type="text"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  placeholder={currentLastName}
                  className="w-full rounded-lg border border-border px-3 py-2 text-sm mt-1 focus:outline-none focus:ring-2 focus:ring-ring"
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
                disabled={!hasChanges || loading}
                className="flex-1 px-3 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 disabled:opacity-50 transition-colors"
              >
                Continue
              </button>
            </div>
          </>
        ) : (
          <>
            <h3 className="text-lg font-semibold mb-4">Confirm Name Change</h3>
            <p className="text-sm text-muted-foreground mb-4">
              Change name from <strong>{currentFirstName} {currentLastName}</strong> to{' '}
              <strong>{newFullName}</strong>?
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
                {loading ? 'Changing...' : 'Change Name'}
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
