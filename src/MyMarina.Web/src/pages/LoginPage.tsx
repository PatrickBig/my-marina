import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { login, getMe, getClientConfig } from '@/api/api';
import { apiBaseUrl } from '@/api/client';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

const ALL_SOCIAL_PROVIDERS = [
  { id: 'google',   label: 'Continue with Google' },
  { id: 'apple',    label: 'Continue with Apple' },
  { id: 'facebook', label: 'Continue with Facebook' },
] as const;

function socialLoginUrl(provider: string) {
  const returnUrl = encodeURIComponent(`${window.location.origin}/auth/callback`);
  return `${apiBaseUrl}/auth/external/${provider}?returnUrl=${returnUrl}`;
}

const schema = z.object({
  email: z.string().email('Enter a valid email address'),
  password: z.string().min(1, 'Password is required'),
});

type FormValues = z.infer<typeof schema>;

export function LoginPage() {
  const { setAuth } = useAuthStore();
  const [serverError, setServerError] = useState<string | null>(null);
  const [socialProviders, setSocialProviders] = useState<string[]>([]);

  useEffect(() => {
    getClientConfig()
      .then((cfg) => setSocialProviders(cfg.socialProviders))
      .catch(() => setSocialProviders([]));
  }, []);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = async (values: FormValues) => {
    setServerError(null);
    try {
      const response = await login(values.email, values.password);
      useAuthStore.setState({ accessToken: response.accessToken });
      const meData = await getMe();
      setAuth(response.accessToken, response.refreshToken, response.expiresAt, response.user, meData.memberships, meData.isPlatformOperator);
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

  const form = (
    <div className="w-full max-w-sm mx-auto">
      {/* Mobile-only brand header */}
      <div className="md:hidden text-center mb-8">
        <div className="text-4xl mb-2">⚓</div>
        <h1 className="text-xl font-bold text-foreground">MyMarina</h1>
        <p className="text-sm text-muted-foreground mt-1">Your marina, your way.</p>
      </div>

      <div className="bg-card rounded-xl border border-border shadow-sm p-8">
        <h2 className="text-2xl font-semibold text-foreground mb-1">Sign in</h2>
        <p className="text-sm text-muted-foreground mb-6">Enter your email and password</p>

        {socialProviders.length > 0 && (
          <>
            <div className="space-y-2 mb-4">
              {ALL_SOCIAL_PROVIDERS.filter((p) => socialProviders.includes(p.id)).map((p) => (
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
                <span className="w-full border-t border-border" />
              </div>
              <div className="relative flex justify-center text-xs uppercase">
                <span className="bg-card px-2 text-muted-foreground">Or continue with email</span>
              </div>
            </div>
          </>
        )}

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
              <p className="text-sm text-destructive">{errors.email.message}</p>
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
              <p className="text-sm text-destructive">{errors.password.message}</p>
            )}
          </div>

          {serverError && (
            <p className="text-sm text-destructive">{serverError}</p>
          )}

          <Button type="submit" className="w-full" disabled={isSubmitting}>
            {isSubmitting ? 'Signing in…' : 'Sign in'}
          </Button>

          <p className="text-center text-sm text-muted-foreground">
            <a href="/auth/forgot-password" className="hover:underline hover:text-foreground">
              Forgot your password?
            </a>
          </p>

          <p className="text-center text-sm text-muted-foreground">
            Don't have an account?{' '}
            <a href="/register" className="font-medium hover:underline hover:text-foreground">
              Sign up
            </a>
          </p>
        </form>
      </div>
    </div>
  );

  return (
    <div className="min-h-screen grid md:grid-cols-2">
      {/* Brand panel — desktop only */}
      <div
        className="hidden md:flex flex-col items-center justify-center p-12 text-white"
        style={{ background: 'linear-gradient(135deg, oklch(52% 0.18 235) 0%, oklch(35% 0.14 245) 50%, oklch(80% 0.10 185) 100%)' }}
      >
        <div className="text-7xl mb-6 drop-shadow-lg">⚓</div>
        <h1 className="text-4xl font-bold tracking-tight mb-3">MyMarina</h1>
        <p className="text-lg text-white/80 text-center max-w-xs leading-relaxed">
          Discover slips, manage your marina, and connect with boaters — all in one place.
        </p>
        <div className="mt-10 flex gap-6 text-sm text-white/60">
          <span>Transient dockage</span>
          <span>·</span>
          <span>Seasonal leases</span>
          <span>·</span>
          <span>Marina management</span>
        </div>
      </div>

      {/* Form panel */}
      <div className="flex items-center justify-center bg-background px-6 py-12">
        {form}
      </div>
    </div>
  );
}
