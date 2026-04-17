import type { MarinaDto, HealthTargetsDto } from "@/api/api";
import { updateMarinaHealthTargets } from "@/api/api";
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
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";

interface HealthTargetsDialogProps {
  marina: MarinaDto;
  onClose: () => void;
}

export function HealthTargetsDialog({
  marina,
  onClose,
}: HealthTargetsDialogProps) {
  const [error, setError] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const { register, handleSubmit, formState: { errors } } = useForm<HealthTargetsDto>({
    defaultValues: {
      occupancyRateTarget: 70,
      overdueARThresholdDays: 30,
      targetMonthlyRevenue: undefined,
    },
  });

  const mutation = useMutation({
    mutationFn: (data: HealthTargetsDto) => updateMarinaHealthTargets(marina.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["marina-metrics", marina.id] });
      onClose();
    },
    onError: (err: any) => {
      setError(err.message || "Failed to update health targets");
    },
  });

  const onSubmit = async (data: HealthTargetsDto) => {
    setError(null);
    mutation.mutate(data);
  };

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit Health Targets for {marina.name}</DialogTitle>
          <DialogDescription>
            Set performance goals that matter to your marina. These targets help track
            occupancy and billing health.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          {error && (
            <div className="p-3 bg-red-100 text-red-800 rounded text-sm">{error}</div>
          )}

          <div className="space-y-2">
            <Label htmlFor="occupancy">
              Target Occupancy Rate (%)
            </Label>
            <Input
              id="occupancy"
              type="number"
              min="0"
              max="100"
              step="1"
              placeholder="70"
              {...register("occupancyRateTarget", {
                valueAsNumber: true,
                min: { value: 0, message: "Must be 0 or higher" },
                max: { value: 100, message: "Must be 100 or lower" },
              })}
            />
            {errors.occupancyRateTarget && (
              <p className="text-sm text-red-600">{errors.occupancyRateTarget.message}</p>
            )}
            <p className="text-xs text-muted-foreground">
              Alert if occupancy drops below 50% of this target
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="threshold">
              Overdue AR Threshold (days)
            </Label>
            <Input
              id="threshold"
              type="number"
              min="1"
              placeholder="30"
              {...register("overdueARThresholdDays", {
                valueAsNumber: true,
                min: { value: 1, message: "Must be 1 or higher" },
              })}
            />
            {errors.overdueARThresholdDays && (
              <p className="text-sm text-red-600">{errors.overdueARThresholdDays.message}</p>
            )}
            <p className="text-xs text-muted-foreground">
              Alert if invoices are overdue beyond this duration
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="revenue">
              Target Monthly Revenue (optional)
            </Label>
            <Input
              id="revenue"
              type="number"
              min="0"
              step="100"
              placeholder="50000"
              {...register("targetMonthlyRevenue", {
                valueAsNumber: true,
              })}
            />
            <p className="text-xs text-muted-foreground">
              For future analytics and reporting
            </p>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={onClose} disabled={mutation.isPending}>
              Cancel
            </Button>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? "Saving..." : "Save"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
