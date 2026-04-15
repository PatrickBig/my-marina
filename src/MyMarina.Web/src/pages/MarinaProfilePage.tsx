import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useEffect } from "react";
import { toast } from "sonner";
import { getMarinas, updateMarina, createMarina, type MarinaDto } from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

const schema = z.object({
  name: z.string().min(1, "Name is required"),
  phoneNumber: z.string().min(1, "Phone is required"),
  email: z.string().email("Invalid email"),
  timeZoneId: z.string().min(1, "Time zone is required"),
  website: z.string().optional().nullable(),
  description: z.string().optional().nullable(),
  street: z.string().min(1, "Street is required"),
  city: z.string().min(1, "City is required"),
  state: z.string().min(1, "State is required"),
  zip: z.string().min(1, "ZIP is required"),
  country: z.string().min(1, "Country is required"),
});
type FormValues = z.infer<typeof schema>;

function marinaToForm(m: MarinaDto): FormValues {
  return {
    name: m.name, phoneNumber: m.phoneNumber, email: m.email,
    timeZoneId: m.timeZoneId, website: m.website ?? "", description: m.description ?? "",
    street: m.address.street, city: m.address.city, state: m.address.state,
    zip: m.address.zip, country: m.address.country,
  };
}

export function MarinaProfilePage() {
  const qc = useQueryClient();
  const { data: marinas, isLoading } = useQuery({ queryKey: ["marinas"], queryFn: getMarinas });
  const marina = marinas?.[0];

  const { register, handleSubmit, reset, formState: { errors, isSubmitting, isDirty } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  });

  useEffect(() => {
    if (marina) reset(marinaToForm(marina));
  }, [marina, reset]);

  const updateMut = useMutation({
    mutationFn: async (values: FormValues) => {
      const payload = {
        name: values.name, phoneNumber: values.phoneNumber, email: values.email,
        timeZoneId: values.timeZoneId, website: values.website || null, description: values.description || null,
        address: { street: values.street, city: values.city, state: values.state, zip: values.zip, country: values.country },
      };
      if (marina) { await updateMarina(marina.id, payload); } else { await createMarina(payload); }
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["marinas"] });
      toast.success("Marina profile saved");
    },
    onError: () => toast.error("Failed to save marina profile"),
  });

  if (isLoading) return <div className="p-8 text-muted-foreground">Loading…</div>;

  return (
    <div className="p-8 max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold">Marina Profile</h1>

      <form onSubmit={handleSubmit((v) => updateMut.mutate(v))} className="space-y-6">
        <Card>
          <CardHeader><CardTitle>General</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <Field label="Marina Name" error={errors.name?.message}>
              <Input {...register("name")} />
            </Field>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Phone" error={errors.phoneNumber?.message}>
                <Input {...register("phoneNumber")} />
              </Field>
              <Field label="Email" error={errors.email?.message}>
                <Input type="email" {...register("email")} />
              </Field>
            </div>
            <Field label="Time Zone" error={errors.timeZoneId?.message}>
              <Input {...register("timeZoneId")} placeholder="America/New_York" />
            </Field>
            <Field label="Website">
              <Input {...register("website")} placeholder="https://" />
            </Field>
            <Field label="Description">
              <Input {...register("description")} />
            </Field>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>Address</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <Field label="Street" error={errors.street?.message}>
              <Input {...register("street")} />
            </Field>
            <div className="grid grid-cols-2 gap-4">
              <Field label="City" error={errors.city?.message}>
                <Input {...register("city")} />
              </Field>
              <Field label="State" error={errors.state?.message}>
                <Input {...register("state")} />
              </Field>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <Field label="ZIP" error={errors.zip?.message}>
                <Input {...register("zip")} />
              </Field>
              <Field label="Country" error={errors.country?.message}>
                <Input {...register("country")} />
              </Field>
            </div>
          </CardContent>
        </Card>

        <Button type="submit" disabled={isSubmitting || !isDirty}>
          {isSubmitting ? "Saving…" : "Save Changes"}
        </Button>
      </form>
    </div>
  );
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <Label>{label}</Label>
      {children}
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  );
}
