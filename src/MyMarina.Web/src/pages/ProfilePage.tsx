import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { updateProfile } from '@/api/api';
import { useAuthStore } from '@/store/authStore';
import { NavBar } from '@/components/NavBar';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

const schema = z.object({
  firstName: z.string().min(1, 'Required'),
  lastName:  z.string().min(1, 'Required'),
  phoneNumber: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

export function ProfilePage() {
  const { user, setAuth, accessToken, refreshToken, expiresAt } = useAuthStore();
  const [serverError, setServerError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<FormValues>({
      resolver: zodResolver(schema),
      defaultValues: {
        firstName:   user?.firstName ?? '',
        lastName:    user?.lastName  ?? '',
        phoneNumber: user?.phoneNumber ?? '',
      },
    });

  const onSubmit = async (values: FormValues) => {
    setServerError(null);
    setSaved(false);
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
      setSaved(true);
    } catch {
      setServerError('Something went wrong. Please try again.');
    }
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <NavBar />
      <div className="max-w-4xl mx-auto py-10 px-4">
        <h1 className="text-xl font-semibold text-slate-800 mb-6">Profile</h1>

        <Card className="max-w-md">
          <CardHeader>
            <CardTitle className="text-base">Personal information</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1">
                  <Label htmlFor="firstName">First name</Label>
                  <Input id="firstName" {...register('firstName')} />
                  {errors.firstName && (
                    <p className="text-xs text-red-600">{errors.firstName.message}</p>
                  )}
                </div>
                <div className="space-y-1">
                  <Label htmlFor="lastName">Last name</Label>
                  <Input id="lastName" {...register('lastName')} />
                  {errors.lastName && (
                    <p className="text-xs text-red-600">{errors.lastName.message}</p>
                  )}
                </div>
              </div>

              <div className="space-y-1">
                <Label htmlFor="email">Email</Label>
                <Input id="email" type="email" value={user?.email ?? ''} disabled />
              </div>

              <div className="space-y-1">
                <Label htmlFor="phoneNumber">Phone number</Label>
                <Input id="phoneNumber" type="tel" {...register('phoneNumber')} />
              </div>

              {serverError && <p className="text-sm text-red-600">{serverError}</p>}
              {saved && <p className="text-sm text-green-600">Saved.</p>}

              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? 'Saving…' : 'Save changes'}
              </Button>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
