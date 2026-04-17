import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useRouter } from "@tanstack/react-router";
import { toast } from "sonner";
import { Anchor } from "lucide-react";
import { useAuthStore } from "@/store/authStore";
import { login, chooseContext, type AvailableContext } from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";

const schema = z.object({
  email: z.string().email("Invalid email"),
  password: z.string().min(1, "Password is required"),
});
type FormValues = z.infer<typeof schema>;

export function LoginPage() {
  const router = useRouter();
  const { login: storeLogin } = useAuthStore();
  const [contextSelection, setContextSelection] = useState<{
    userId: string;
    contexts: AvailableContext[];
  } | null>(null);
  const [selectingContext, setSelectingContext] = useState(false);

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  });

  const onSubmit = async (values: FormValues) => {
    try {
      const result = await login(values.email, values.password);

      // If no token and multiple contexts, show selection screen
      if (!result.token && result.availableContexts && result.availableContexts.length > 1) {
        setContextSelection({
          userId: result.userId,
          contexts: result.availableContexts,
        });
        return;
      }

      // Single context or immediate token available
      const token = result.token || "";
      const role = result.role as number;
      storeLogin(token, {
        userId: result.userId,
        email: result.email,
        firstName: result.firstName,
        lastName: result.lastName,
        role,
        tenantId: result.tenantId ?? null,
        marinaId: result.marinaId ?? null,
      });
      // Route by role: platform operators → /platform, customers → /portal, operators → /
      if (role === 0) router.navigate({ to: "/platform/tenants" });
      else if (role === 3) router.navigate({ to: "/portal" });
      else router.navigate({ to: "/" });
    } catch {
      toast.error("Invalid email or password");
    }
  };

  const handleContextSelect = async (context: AvailableContext) => {
    if (!contextSelection) return;
    setSelectingContext(true);
    try {
      const tokenResult = await chooseContext(contextSelection.userId, context);
      const role = context.role as number;
      storeLogin(tokenResult.token, {
        userId: contextSelection.userId,
        email: "", // Not available in context selection
        firstName: "",
        lastName: "",
        role,
        tenantId: context.tenantId,
        marinaId: context.marinaId ?? null,
      });
      // Route by role
      if (role === 0) router.navigate({ to: "/platform/tenants" });
      else if (role === 3) router.navigate({ to: "/portal" });
      else router.navigate({ to: "/" });
    } catch {
      toast.error("Failed to select context");
    } finally {
      setSelectingContext(false);
    }
  };

  // Show context selection UI
  if (contextSelection) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-muted/40 px-4">
        <Card className="w-full max-w-sm">
          <CardHeader className="text-center">
            <div className="flex justify-center mb-2">
              <Anchor className="h-8 w-8 text-primary" />
            </div>
            <CardTitle className="text-2xl">Choose Your Account</CardTitle>
            <CardDescription>Select which account to access</CardDescription>
          </CardHeader>
          <CardContent className="space-y-2">
            {contextSelection.contexts.map((context) => (
              <Button
                key={`${context.role}-${context.tenantId}-${context.customerAccountId ?? ''}`}
                onClick={() => handleContextSelect(context)}
                disabled={selectingContext}
                variant="outline"
                className="w-full justify-start text-left"
              >
                {context.displayName}
              </Button>
            ))}
            <Button
              onClick={() => setContextSelection(null)}
              variant="ghost"
              className="w-full"
              disabled={selectingContext}
            >
              Back to Login
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-muted/40 px-4">
      <Card className="w-full max-w-sm">
        <CardHeader className="text-center">
          <div className="flex justify-center mb-2">
            <Anchor className="h-8 w-8 text-primary" />
          </div>
          <CardTitle className="text-2xl">MyMarina</CardTitle>
          <CardDescription>Sign in to your marina dashboard</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="email">Email</Label>
              <Input id="email" type="email" placeholder="you@marina.com" {...register("email")} />
              {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="password">Password</Label>
              <Input id="password" type="password" {...register("password")} />
              {errors.password && <p className="text-xs text-destructive">{errors.password.message}</p>}
            </div>
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? "Signing in…" : "Sign in"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
