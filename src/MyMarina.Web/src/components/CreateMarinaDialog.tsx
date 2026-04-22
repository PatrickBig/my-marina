import { createMarina } from "@/api/api";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useForm } from "react-hook-form";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { useState } from "react";

interface FormValues {
  name: string;
  phoneNumber: string;
  email: string;
  timeZoneId: string;
  website: string;
  street: string;
  city: string;
  state: string;
  zip: string;
  country: string;
}

interface CreateMarinaDialogProps {
  onClose: () => void;
}

export function CreateMarinaDialog({ onClose }: CreateMarinaDialogProps) {
  const [error, setError] = useState<string | null>(null);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    defaultValues: { country: "US", timeZoneId: "America/New_York" },
  });

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      createMarina({
        name: values.name,
        phoneNumber: values.phoneNumber,
        email: values.email,
        timeZoneId: values.timeZoneId,
        website: values.website || null,
        address: {
          street: values.street,
          city: values.city,
          state: values.state,
          zip: values.zip,
          country: values.country,
        },
      }),
    onSuccess: (newMarinaId) => {
      queryClient.invalidateQueries({ queryKey: ["marinas"] });
      onClose();
      navigate({ to: "/marinas/$marinaId/profile", params: { marinaId: newMarinaId } });
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || err.message || "Failed to create marina.");
    },
  });

  const req = (label: string) => ({ required: `${label} is required` });

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Create Marina</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="space-y-4">
          {error && (
            <div className="p-3 bg-red-100 text-red-800 rounded text-sm">{error}</div>
          )}

          <div className="space-y-2">
            <Label htmlFor="name">Marina Name</Label>
            <Input id="name" placeholder="Sunset Harbor Marina" {...register("name", req("Marina name"))} />
            {errors.name && <p className="text-xs text-red-600">{errors.name.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="phoneNumber">Phone</Label>
              <Input id="phoneNumber" placeholder="555-0100" {...register("phoneNumber", req("Phone"))} />
              {errors.phoneNumber && <p className="text-xs text-red-600">{errors.phoneNumber.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input id="email" type="email" placeholder="info@marina.com" {...register("email", req("Email"))} />
              {errors.email && <p className="text-xs text-red-600">{errors.email.message}</p>}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="street">Street Address</Label>
            <Input id="street" placeholder="1 Harbor Way" {...register("street", req("Street"))} />
            {errors.street && <p className="text-xs text-red-600">{errors.street.message}</p>}
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div className="space-y-2">
              <Label htmlFor="city">City</Label>
              <Input id="city" placeholder="Marina Bay" {...register("city", req("City"))} />
              {errors.city && <p className="text-xs text-red-600">{errors.city.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="state">State</Label>
              <Input id="state" placeholder="CA" maxLength={2} {...register("state", req("State"))} />
              {errors.state && <p className="text-xs text-red-600">{errors.state.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="zip">ZIP</Label>
              <Input id="zip" placeholder="94000" {...register("zip", req("ZIP"))} />
              {errors.zip && <p className="text-xs text-red-600">{errors.zip.message}</p>}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="timeZoneId">Time Zone</Label>
              <select
                id="timeZoneId"
                className="w-full border rounded-md px-3 py-2 text-sm bg-background"
                {...register("timeZoneId", req("Time zone"))}
              >
                <option value="America/New_York">Eastern</option>
                <option value="America/Chicago">Central</option>
                <option value="America/Denver">Mountain</option>
                <option value="America/Los_Angeles">Pacific</option>
                <option value="America/Anchorage">Alaska</option>
                <option value="Pacific/Honolulu">Hawaii</option>
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="website">Website (optional)</Label>
              <Input id="website" placeholder="https://..." {...register("website")} />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" type="button" onClick={onClose} disabled={mutation.isPending}>
              Cancel
            </Button>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? "Creating…" : "Create Marina"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
