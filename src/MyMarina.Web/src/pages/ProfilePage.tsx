import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { updateProfile, changePassword, getClientConfig, getLinkedProviders, unlinkProvider } from '@/api/api';
import { apiBaseUrl } from '@/api/client';
import { useAuthStore } from '@/store/authStore';
import { NavBar } from '@/components/NavBar';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

const PROVIDER_LABELS: Record<string, string> = {
  google:   'Google',
  apple:    'Apple',
  facebook: 'Facebook',
};

function linkProviderUrl(provider: string) {
  const returnUrl = encodeURIComponent('/profile');
  return `${apiBaseUrl}/auth/external/${provider}/link?returnUrl=${returnUrl}`;
}

const profileSchema = z.object({
  firstName: z.string().min(1, 'Required'),
  lastName:  z.string().min(1, 'Required'),
  phoneNumber: z.string().optional(),
});

const passwordSchema = z.object({
  currentPassword: z.string().min(1, 'Required'),
  newPassword: z.string().min(8, 'Must be at least 8 characters'),
  confirmPassword: z.string().min(1, 'Required'),
}).refine((d) => d.newPassword === d.confirmPassword, {
  message: 'Passwords do not match',
  path: ['confirmPassword'],
});

type ProfileValues = z.infer<typeof profileSchema>;
type PasswordValues = z.infer<typeof passwordSchema>;

export function ProfilePage() {
  const { user, setAuth, accessToken, refreshToken, expiresAt } = useAuthStore();
  const [profileError, setProfileError] = useState<string | null>(null);
  const [profileSaved, setProfileSaved] = useState(false);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [passwordSaved, setPasswordSaved] = useState(false);

  const [availableProviders, setAvailableProviders] = useState<string[]>([]);
  const [linkedProviders, setLinkedProviders] = useState<string[]>([]);
  const [loadingProviders, setLoadingProviders] = useState(true);
  const [unlinkingProvider, setUnlinkingProvider] = useState<string | null>(null);
  const [providerMessage, setProviderMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const linked = params.get('linked');
    const error  = params.get('error');
    if (linked) {
      const label = PROVIDER_LABELS[linked.toLowerCase()] ?? linked;
      setProviderMessage({ type: 'success', text: `${label} connected successfully.` });
      window.history.replaceState({}, '', '/profile');
    } else if (error) {
      setProviderMessage({ type: 'error', text: error });
      window.history.replaceState({}, '', '/profile');
    }

    Promise.all([getClientConfig(), getLinkedProviders()])
      .then(([config, linked]) => {
        setAvailableProviders(config.socialProviders);
        setLinkedProviders(linked.map((p) => p.provider.toLowerCase()));
      })
      .catch(() => {})
      .finally(() => setLoadingProviders(false));
  }, []);

  const { register: regProfile, handleSubmit: handleProfile, formState: { errors: profileErrors, isSubmitting: profileSubmitting } } =
    useForm<ProfileValues>({
      resolver: zodResolver(profileSchema),
      defaultValues: {
        firstName:   user?.firstName ?? '',
        lastName:    user?.lastName  ?? '',
        phoneNumber: user?.phoneNumber ?? '',
      },
    });

  const { register: regPwd, handleSubmit: handlePwd, reset: resetPwd, formState: { errors: pwdErrors, isSubmitting: pwdSubmitting } } =
    useForm<PasswordValues>({ resolver: zodResolver(passwordSchema) });

  const onProfileSubmit = async (values: ProfileValues) => {
    setProfileError(null);
    setProfileSaved(false);
    try {
      await updateProfile({
        firstName:   values.firstName,
        lastName:    values.lastName,
        phoneNumber: values.phoneNumber || null,
      });
      if (user && accessToken && refreshToken && expiresAt) {
        setAuth(accessToken, refreshToken, expiresAt, {
          ...user,
          firstName:   values.firstName,
          lastName:    values.lastName,
          phoneNumber: values.phoneNumber || null,
        });
      }
      setProfileSaved(true);
    } catch {
      setProfileError('Something went wrong. Please try again.');
    }
  };

  const onPasswordSubmit = async (values: PasswordValues) => {
    setPasswordError(null);
    setPasswordSaved(false);
    try {
      await changePassword(values.currentPassword, values.newPassword);
      setPasswordSaved(true);
      resetPwd();
    } catch (err: unknown) {
      const data = (err as { response?: { data?: { errors?: Record<string, string[]> } } })?.response?.data;
      if (data?.errors) {
        const messages = Object.values(data.errors).flat().join(' ');
        setPasswordError(messages || 'Failed to change password.');
      } else {
        setPasswordError('Current password is incorrect or the new password does not meet requirements.');
      }
    }
  };

  const handleUnlink = async (provider: string) => {
    setUnlinkingProvider(provider);
    setProviderMessage(null);
    try {
      await unlinkProvider(provider);
      setLinkedProviders((prev) => prev.filter((p) => p !== provider));
      const label = PROVIDER_LABELS[provider] ?? provider;
      setProviderMessage({ type: 'success', text: `${label} disconnected.` });
    } catch (err: unknown) {
      const detail = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
      setProviderMessage({
        type: 'error',
        text: detail ?? 'Could not disconnect. This may be your only sign-in method.',
      });
    } finally {
      setUnlinkingProvider(null);
    }
  };

  return (
    <div className="min-h-screen bg-muted/30">
      <NavBar />
      <div className="max-w-4xl mx-auto py-10 px-4">
        <h1 className="text-xl font-semibold text-foreground mb-6">Profile</h1>

        <div className="flex flex-col gap-6 max-w-md">
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Personal information</CardTitle>
            </CardHeader>
            <CardContent>
              <form onSubmit={handleProfile(onProfileSubmit)} className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-1">
                    <Label htmlFor="firstName">First name</Label>
                    <Input id="firstName" {...regProfile('firstName')} />
                    {profileErrors.firstName && (
                      <p className="text-xs text-red-600">{profileErrors.firstName.message}</p>
                    )}
                  </div>
                  <div className="space-y-1">
                    <Label htmlFor="lastName">Last name</Label>
                    <Input id="lastName" {...regProfile('lastName')} />
                    {profileErrors.lastName && (
                      <p className="text-xs text-red-600">{profileErrors.lastName.message}</p>
                    )}
                  </div>
                </div>

                <div className="space-y-1">
                  <Label htmlFor="email">Email</Label>
                  <Input id="email" type="email" value={user?.email ?? ''} disabled />
                </div>

                <div className="space-y-1">
                  <Label htmlFor="phoneNumber">Phone number</Label>
                  <Input id="phoneNumber" type="tel" {...regProfile('phoneNumber')} />
                </div>

                {profileError && <p className="text-sm text-red-600">{profileError}</p>}
                {profileSaved && <p className="text-sm text-green-600">Saved.</p>}

                <Button type="submit" disabled={profileSubmitting}>
                  {profileSubmitting ? 'Saving…' : 'Save changes'}
                </Button>
              </form>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Change password</CardTitle>
            </CardHeader>
            <CardContent>
              <form onSubmit={handlePwd(onPasswordSubmit)} className="space-y-4">
                <div className="space-y-1">
                  <Label htmlFor="currentPassword">Current password</Label>
                  <Input id="currentPassword" type="password" {...regPwd('currentPassword')} />
                  {pwdErrors.currentPassword && (
                    <p className="text-xs text-red-600">{pwdErrors.currentPassword.message}</p>
                  )}
                </div>

                <div className="space-y-1">
                  <Label htmlFor="newPassword">New password</Label>
                  <Input id="newPassword" type="password" {...regPwd('newPassword')} />
                  {pwdErrors.newPassword && (
                    <p className="text-xs text-red-600">{pwdErrors.newPassword.message}</p>
                  )}
                </div>

                <div className="space-y-1">
                  <Label htmlFor="confirmPassword">Confirm new password</Label>
                  <Input id="confirmPassword" type="password" {...regPwd('confirmPassword')} />
                  {pwdErrors.confirmPassword && (
                    <p className="text-xs text-red-600">{pwdErrors.confirmPassword.message}</p>
                  )}
                </div>

                {passwordError && <p className="text-sm text-red-600">{passwordError}</p>}
                {passwordSaved && <p className="text-sm text-green-600">Password updated.</p>}

                <Button type="submit" disabled={pwdSubmitting}>
                  {pwdSubmitting ? 'Updating…' : 'Update password'}
                </Button>
              </form>
            </CardContent>
          </Card>

          {!loadingProviders && availableProviders.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Linked accounts</CardTitle>
              </CardHeader>
              <CardContent>
                {providerMessage && (
                  <p className={`text-sm mb-4 ${providerMessage.type === 'error' ? 'text-red-600' : 'text-green-600'}`}>
                    {providerMessage.text}
                  </p>
                )}
                <div className="space-y-3">
                  {availableProviders.map((provider) => {
                    const isLinked    = linkedProviders.includes(provider);
                    const isUnlinking = unlinkingProvider === provider;
                    return (
                      <div key={provider} className="flex items-center justify-between">
                        <span className="text-sm font-medium">
                          {PROVIDER_LABELS[provider] ?? provider}
                        </span>
                        {isLinked ? (
                          <Button
                            variant="outline"
                            size="sm"
                            disabled={isUnlinking}
                            onClick={() => handleUnlink(provider)}
                          >
                            {isUnlinking ? 'Disconnecting…' : 'Disconnect'}
                          </Button>
                        ) : (
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => { window.location.href = linkProviderUrl(provider); }}
                          >
                            Connect
                          </Button>
                        )}
                      </div>
                    );
                  })}
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
