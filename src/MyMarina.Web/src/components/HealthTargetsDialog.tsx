import type { MarinaDto, HealthTargetsDto } from "@/api/api";
import { getMarinaHealthTargets, updateMarinaHealthTargets } from "@/api/api";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useForm } from "react-hook-form";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";

interface HealthTargetsDialogProps {
  marina: MarinaDto;
  onClose: () => void;
}

type FormValues = {
  occupancyWarningThreshold: number | null;
  occupancyAlertThreshold: number | null;
  overdueWarningDays: number | null;
  overdueAlertDays: number | null;
};

export function HealthTargetsDialog({ marina, onClose }: HealthTargetsDialogProps) {
  const [error, setError] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const { data: existing, isLoading } = useQuery<HealthTargetsDto | undefined>({
    queryKey: ["marina-health-targets", marina.id],
    queryFn: () => getMarinaHealthTargets(marina.id),
  });

  const toNum = (v: null | number | string | undefined) =>
    v == null ? null : typeof v === "string" ? parseFloat(v) : v;

  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    values: existing
      ? {
          occupancyWarningThreshold: toNum(existing.occupancyWarningThreshold),
          occupancyAlertThreshold: toNum(existing.occupancyAlertThreshold),
          overdueWarningDays: toNum(existing.overdueWarningDays),
          overdueAlertDays: toNum(existing.overdueAlertDays),
        }
      : {
          occupancyWarningThreshold: null,
          occupancyAlertThreshold: null,
          overdueWarningDays: null,
          overdueAlertDays: null,
        },
  });

  const mutation = useMutation({
    mutationFn: (data: FormValues) =>
      updateMarinaHealthTargets(marina.id, {
        occupancyWarningThreshold: data.occupancyWarningThreshold,
        occupancyAlertThreshold: data.occupancyAlertThreshold,
        overdueWarningDays: data.overdueWarningDays,
        overdueAlertDays: data.overdueAlertDays,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["marina-metrics", marina.id] });
      queryClient.invalidateQueries({ queryKey: ["marina-health-targets", marina.id] });
      onClose();
    },
    onError: (err: any) => {
      setError(err.message || "Failed to update health targets");
    },
  });

  const onSubmit = (data: FormValues) => {
    setError(null);
    mutation.mutate(data);
  };

  const numericField = (name: keyof FormValues, label: string, hint: string, placeholder: string, min: number, max?: number) => (
    <div className="space-y-1">
      <Label htmlFor={name}>{label}</Label>
      <Input
        id={name}
        type="number"
        min={min}
        max={max}
        step="1"
        placeholder={placeholder}
        {...register(name, {
          setValueAs: (v) => (v === "" || v == null ? null : Number(v)),
          min: { value: min, message: `Must be ${min} or higher` },
          ...(max != null ? { max: { value: max, message: `Must be ${max} or lower` } } : {}),
        })}
      />
      {errors[name] && <p className="text-xs text-red-600">{errors[name]!.message}</p>}
      <p className="text-xs text-muted-foreground">{hint}</p>
    </div>
  );

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit Health Thresholds — {marina.name}</DialogTitle>
          <DialogDescription>
            Set explicit warning and alert thresholds for each metric. The marina badge will reflect the worst-case state.
          </DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className="py-6 text-center text-sm text-muted-foreground">Loading…</div>
        ) : (
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            {error && (
              <div className="p-3 bg-red-100 text-red-800 rounded text-sm">{error}</div>
            )}

            <div className="space-y-3">
              <h4 className="text-sm font-semibold">Occupancy (%)</h4>
              {numericField(
                "occupancyWarningThreshold",
                "Warning below",
                "Shows amber warning when occupancy drops below this value.",
                "e.g. 60",
                0, 100,
              )}
              {numericField(
                "occupancyAlertThreshold",
                "Alert below",
                "Shows red alert when occupancy drops below this value. Must be lower than the warning threshold.",
                "e.g. 35",
                0, 100,
              )}
            </div>

            <div className="space-y-3">
              <h4 className="text-sm font-semibold">Overdue Invoices (days)</h4>
              {numericField(
                "overdueWarningDays",
                "Warning after",
                "Shows amber warning when the oldest overdue invoice exceeds this many days.",
                "e.g. 30",
                1,
              )}
              {numericField(
                "overdueAlertDays",
                "Alert after",
                "Shows red alert when the oldest overdue invoice exceeds this many days.",
                "e.g. 60",
                1,
              )}
            </div>

            <DialogFooter>
              <Button variant="outline" onClick={onClose} disabled={mutation.isPending}>
                Cancel
              </Button>
              <Button type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? "Saving…" : "Save"}
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
