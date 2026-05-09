import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { updateProfile, changePassword } from '@/api/api';
import { useAuthStore } from '@/store/authStore';
import { NavBar } from '@/components/NavBar';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

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

  return (
    <div className="min-h-screen bg-slate-50">
      <NavBar />
      <div className="max-w-4xl mx-auto py-10 px-4">
        <h1 className="text-xl font-semibold text-slate-800 mb-6">Profile</h1>

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
        </div>
      </div>
    </div>
  );
}
