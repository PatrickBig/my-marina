import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useEffect } from "react";
import { toast } from "sonner";
import axios from "axios";
import { useRouter } from "@tanstack/react-router";
import { ArrowLeft } from "lucide-react";
import { getProfile, updateProfile, changeEmail, changePassword } from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

// ─── Schemas ──────────────────────────────────────────────────────────────────

const profileSchema = z.object({
  firstName: z.string().min(1, "First name is required"),
  lastName: z.string().min(1, "Last name is required"),
  phoneNumber: z.string().optional().nullable(),
});
type ProfileForm = z.infer<typeof profileSchema>;

const emailSchema = z.object({
  newEmail: z.string().email("Invalid email address"),
  currentPassword: z.string().min(1, "Current password is required"),
});
type EmailForm = z.infer<typeof emailSchema>;

const passwordSchema = z
  .object({
    currentPassword: z.string().min(1, "Current password is required"),
    newPassword: z.string().min(10, "Password must be at least 10 characters"),
    confirmPassword: z.string().min(1, "Please confirm your new password"),
  })
  .refine((d) => d.newPassword === d.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  });
type PasswordForm = z.infer<typeof passwordSchema>;

// ─── Field helper ─────────────────────────────────────────────────────────────

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1">
      <Label>{label}</Label>
      {children}
      {error && <p className="text-sm text-destructive">{error}</p>}
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export function ProfilePage() {
  const router = useRouter();
  const qc = useQueryClient();
  const { data: profile, isLoading } = useQuery({
    queryKey: ["profile"],
    queryFn: getProfile,
  });

  // ── Personal info form ─────────────────────────────────────────────────────
  const {
    register: regProfile,
    handleSubmit: submitProfile,
    reset: resetProfile,
    formState: { errors: profileErrors, isSubmitting: profileSubmitting, isDirty: profileDirty },
  } = useForm<ProfileForm>({ resolver: zodResolver(profileSchema) });

  useEffect(() => {
    if (profile)
      resetProfile({
        firstName: profile.firstName,
        lastName: profile.lastName,
        phoneNumber: profile.phoneNumber ?? "",
      });
  }, [profile, resetProfile]);

  const updateProfileMut = useMutation({
    mutationFn: (values: ProfileForm) =>
      updateProfile({
        firstName: values.firstName,
        lastName: values.lastName,
        phoneNumber: values.phoneNumber || null,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["profile"] });
      toast.success("Profile updated");
    },
    onError: () => toast.error("Failed to update profile"),
  });

  // ── Change email form ──────────────────────────────────────────────────────
  const {
    register: regEmail,
    handleSubmit: submitEmail,
    reset: resetEmail,
    setError: setEmailError,
    formState: { errors: emailErrors, isSubmitting: emailSubmitting },
  } = useForm<EmailForm>({ resolver: zodResolver(emailSchema) });

  const changeEmailMut = useMutation({
    mutationFn: (values: EmailForm) =>
      changeEmail({ newEmail: values.newEmail, currentPassword: values.currentPassword }),
    onSuccess: () => {
      toast.success("Email updated. Please log in again with your new email.");
      resetEmail();
    },
    onError: (error) => {
      if (axios.isAxiosError(error) && error.response?.status === 409) {
        setEmailError("newEmail", { message: "This email address is already in use" });
      } else if (axios.isAxiosError(error) && error.response?.status === 400) {
        setEmailError("currentPassword", {
          message: error.response.data?.message ?? "Incorrect password",
        });
      } else {
        toast.error("Failed to update email");
      }
    },
  });

  // ── Change password form ───────────────────────────────────────────────────
  const {
    register: regPassword,
    handleSubmit: submitPassword,
    reset: resetPassword,
    setError: setPasswordError,
    formState: { errors: passwordErrors, isSubmitting: passwordSubmitting },
  } = useForm<PasswordForm>({ resolver: zodResolver(passwordSchema) });

  const changePasswordMut = useMutation({
    mutationFn: (values: PasswordForm) =>
      changePassword({ currentPassword: values.currentPassword, newPassword: values.newPassword }),
    onSuccess: () => {
      toast.success("Password changed successfully");
      resetPassword();
    },
    onError: (error) => {
      if (axios.isAxiosError(error) && error.response?.status === 400) {
        const errors: string[] = error.response.data?.errors ?? [];
        const message = errors.length > 0 ? errors.join(" ") : (error.response.data?.message ?? "Password change failed");
        setPasswordError("newPassword", { message });
      } else {
        toast.error("Failed to change password");
      }
    },
  });

  if (isLoading) return <div className="p-8 text-muted-foreground">Loading…</div>;

  return (
    <div className="p-8 max-w-2xl space-y-8">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => router.history.back()} aria-label="Go back">
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <h1 className="text-2xl font-bold">My Profile</h1>
      </div>

      {/* Personal Info */}
      <Card>
        <CardHeader>
          <CardTitle>Personal Information</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={submitProfile((v) => updateProfileMut.mutate(v))} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Field label="First Name" error={profileErrors.firstName?.message}>
                <Input {...regProfile("firstName")} />
              </Field>
              <Field label="Last Name" error={profileErrors.lastName?.message}>
                <Input {...regProfile("lastName")} />
              </Field>
            </div>
            <Field label="Phone Number" error={profileErrors.phoneNumber?.message}>
              <Input {...regProfile("phoneNumber")} placeholder="Optional" />
            </Field>
            <div className="flex justify-end">
              <Button
                type="submit"
                disabled={!profileDirty || profileSubmitting || updateProfileMut.isPending}
              >
                {updateProfileMut.isPending ? "Saving…" : "Save Changes"}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Change Email */}
      <Card>
        <CardHeader>
          <CardTitle>Change Email</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={submitEmail((v) => changeEmailMut.mutate(v))} className="space-y-4">
            <Field label="New Email Address" error={emailErrors.newEmail?.message}>
              <Input {...regEmail("newEmail")} type="email" />
            </Field>
            <Field label="Current Password" error={emailErrors.currentPassword?.message}>
              <Input {...regEmail("currentPassword")} type="password" />
            </Field>
            <div className="flex justify-end">
              <Button type="submit" disabled={emailSubmitting || changeEmailMut.isPending}>
                {changeEmailMut.isPending ? "Updating…" : "Update Email"}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Change Password */}
      <Card>
        <CardHeader>
          <CardTitle>Change Password</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={submitPassword((v) => changePasswordMut.mutate(v))} className="space-y-4">
            <Field label="Current Password" error={passwordErrors.currentPassword?.message}>
              <Input {...regPassword("currentPassword")} type="password" />
            </Field>
            <Field label="New Password" error={passwordErrors.newPassword?.message}>
              <Input {...regPassword("newPassword")} type="password" />
            </Field>
            <Field label="Confirm New Password" error={passwordErrors.confirmPassword?.message}>
              <Input {...regPassword("confirmPassword")} type="password" />
            </Field>
            <div className="flex justify-end">
              <Button type="submit" disabled={passwordSubmitting || changePasswordMut.isPending}>
                {changePasswordMut.isPending ? "Changing…" : "Change Password"}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
