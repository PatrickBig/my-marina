import { useQuery, useMutation } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState } from "react";
import { toast } from "sonner";
import { UserPlus, Copy, Check } from "lucide-react";
import { getMarinas, inviteStaff, type InviteStaffResult } from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
// UserRole enum values matching backend: MarinaOwner=1, MarinaStaff=2
const staffRoles = [
  { value: "2", label: "Staff" },
  { value: "1", label: "Marina Owner" },
];

const schema = z.object({
  email: z.string().email("Invalid email"),
  firstName: z.string().min(1, "Required"),
  lastName: z.string().min(1, "Required"),
  role: z.number(),
});
type FormValues = z.infer<typeof schema>;

export function StaffPage() {
  const [result, setResult] = useState<InviteStaffResult | null>(null);
  const [copied, setCopied] = useState(false);

  const { data: marinas } = useQuery({ queryKey: ["marinas"], queryFn: getMarinas });
  const marina = marinas?.[0];

  const { register, handleSubmit, reset, control, formState: { errors, isSubmitting } } = useForm<FormValues, any, FormValues>({
    resolver: zodResolver(schema) as any,
    defaultValues: { role: 2 },
  });

  const inviteMut = useMutation({
    mutationFn: (v: FormValues) => inviteStaff({
      marinaId: marina!.id, email: v.email, firstName: v.firstName, lastName: v.lastName, role: v.role,
    }),
    onSuccess: (data) => {
      setResult(data);
      reset();
      toast.success("Staff member invited");
    },
    onError: () => toast.error("Failed to invite staff member"),
  });

  const copyPassword = async () => {
    if (!result) return;
    await navigator.clipboard.writeText(result.temporaryPassword);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  if (!marina) return <div className="p-8 text-muted-foreground">No marina configured yet.</div>;

  return (
    <div className="p-8 max-w-lg space-y-6">
      <h1 className="text-2xl font-bold">Invite Staff</h1>
      <p className="text-muted-foreground text-sm">
        Invite a staff member to access <strong>{marina.name}</strong>. A temporary password will be generated — share it with them securely.
      </p>

      <Card>
        <CardHeader><CardTitle>New Staff Invitation</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit((v) => inviteMut.mutate(v))} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>First Name</Label>
                <Input {...register("firstName")} />
                {errors.firstName && <p className="text-xs text-destructive">{errors.firstName.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Last Name</Label>
                <Input {...register("lastName")} />
                {errors.lastName && <p className="text-xs text-destructive">{errors.lastName.message}</p>}
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Email</Label>
              <Input type="email" {...register("email")} />
              {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Role</Label>
              <Controller control={control} name="role" render={({ field }) => (
                <Select value={String(field.value)} onValueChange={(v) => field.onChange(Number(v))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {staffRoles.map(r => <SelectItem key={r.value} value={r.value}>{r.label}</SelectItem>)}
                  </SelectContent>
                </Select>
              )} />
            </div>
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              <UserPlus className="h-4 w-4" />
              {isSubmitting ? "Inviting…" : "Send Invitation"}
            </Button>
          </form>
        </CardContent>
      </Card>

      {/* Temporary password result */}
      {result && (
        <Card className="border-green-200 bg-green-50">
          <CardHeader>
            <CardTitle className="text-green-800">Invitation Created</CardTitle>
            <CardDescription className="text-green-700">
              Share this temporary password with the new staff member. It will not be shown again.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="space-y-1.5">
              <Label className="text-green-800">Temporary Password</Label>
              <div className="flex gap-2">
                <Input
                  readOnly
                  value={result.temporaryPassword}
                  className="font-mono bg-white border-green-200"
                />
                <Button type="button" variant="outline" size="icon" onClick={copyPassword} className="border-green-200 shrink-0">
                  {copied ? <Check className="h-4 w-4 text-green-600" /> : <Copy className="h-4 w-4" />}
                </Button>
              </div>
            </div>
            <Button variant="ghost" size="sm" className="text-green-700 hover:text-green-800 hover:bg-green-100 -ml-2" onClick={() => setResult(null)}>
              Dismiss
            </Button>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
