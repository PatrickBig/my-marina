import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { login, getMe } from '@/api/api';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

const SOCIAL_PROVIDERS = [
  { id: 'google',   label: 'Continue with Google' },
  { id: 'apple',    label: 'Continue with Apple' },
  { id: 'facebook', label: 'Continue with Facebook' },
] as const;

function socialLoginUrl(provider: string) {
  const returnUrl = encodeURIComponent(`${window.location.origin}/auth/callback`);
  return `/api/auth/external/${provider}?returnUrl=${returnUrl}`;
}

const schema = z.object({
  email: z.string().email('Enter a valid email address'),
  password: z.string().min(1, 'Password is required'),
});

type FormValues = z.infer<typeof schema>;

export function LoginPage() {
  const { setAuth } = useAuthStore();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = async (values: FormValues) => {
    setServerError(null);
    try {
      const response = await login(values.email, values.password);
      // Temporarily set token so getMe() can attach it
      useAuthStore.setState({ accessToken: response.accessToken });
      const meData = await getMe();
      setAuth(response.accessToken, response.refreshToken, response.expiresAt, response.user, meData.memberships);
      window.location.href = '/';
    } catch (err: any) {
      const status = err?.response?.status;
      if (status === 401) {
        setServerError('Incorrect email or password.');
      } else {
        setServerError('Something went wrong. Please try again.');
      }
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-50 px-4">
      <Card className="w-full max-w-sm">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">Sign in to MyMarina</CardTitle>
          <CardDescription>Enter your email and password</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-2 mb-4">
            {SOCIAL_PROVIDERS.map((p) => (
              <Button
                key={p.id}
                variant="outline"
                type="button"
                className="w-full"
                onClick={() => { window.location.href = socialLoginUrl(p.id); }}
              >
                {p.label}
              </Button>
            ))}
          </div>

          <div className="relative mb-4">
            <div className="absolute inset-0 flex items-center">
              <span className="w-full border-t" />
            </div>
            <div className="relative flex justify-center text-xs uppercase">
              <span className="bg-white px-2 text-slate-500">Or continue with email</span>
            </div>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-1">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                placeholder="you@example.com"
                {...register('email')}
              />
              {errors.email && (
                <p className="text-sm text-red-600">{errors.email.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                {...register('password')}
              />
              {errors.password && (
                <p className="text-sm text-red-600">{errors.password.message}</p>
              )}
            </div>

            {serverError && (
              <p className="text-sm text-red-600">{serverError}</p>
            )}

            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? 'Signing in…' : 'Sign in'}
            </Button>

            <p className="text-center text-sm text-slate-500">
              <a href="/auth/forgot-password" className="hover:underline">
                Forgot your password?
              </a>
            </p>

            <p className="text-center text-sm text-slate-500">
              Don't have an account?{' '}
              <a href="/register" className="font-medium hover:underline">
                Sign up
              </a>
            </p>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
