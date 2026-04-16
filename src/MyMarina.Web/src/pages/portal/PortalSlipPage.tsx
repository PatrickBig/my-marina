import { useQuery } from "@tanstack/react-query";
import { Anchor, Ship, Calendar, Sailboat } from "lucide-react";
import { getPortalSlip } from "@/api/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";

const ASSIGNMENT_TYPE_LABELS: Record<number, string> = {
  0: "Seasonal", 1: "Annual", 2: "Monthly", 3: "Daily",
};

export function PortalSlipPage() {
  const { data: slip, isLoading } = useQuery({ queryKey: ["portal-slip"], queryFn: getPortalSlip });

  if (isLoading) return <div className="p-8 text-muted-foreground">Loading…</div>;

  if (!slip) {
    return (
      <div className="p-8 space-y-4">
        <h1 className="text-2xl font-bold">My Slip</h1>
        <div className="flex flex-col items-center justify-center rounded-md border border-dashed p-16 text-center">
          <Anchor className="h-10 w-10 text-muted-foreground mb-4" />
          <p className="text-muted-foreground">You don't have an active slip assignment.</p>
          <p className="text-xs text-muted-foreground mt-1">Contact your marina to arrange a slip assignment.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-8 space-y-6 max-w-2xl">
      <h1 className="text-2xl font-bold">My Slip</h1>

      <div className="grid gap-4 sm:grid-cols-2">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Slip</CardTitle>
            <Ship className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">{slip.slipName}</p>
            {slip.dockName && <p className="text-sm text-muted-foreground">{slip.dockName}</p>}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Marina</CardTitle>
            <Anchor className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <p className="text-lg font-semibold">{slip.marinaName}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Vessel</CardTitle>
            <Sailboat className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <p className="text-lg font-semibold">{slip.boatName}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Assignment</CardTitle>
            <Calendar className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent className="space-y-2">
            <Badge variant="secondary">{ASSIGNMENT_TYPE_LABELS[slip.assignmentType] ?? slip.assignmentType}</Badge>
            <div className="text-sm">
              <span className="text-muted-foreground">Start: </span>{slip.startDate}
            </div>
            {slip.endDate && (
              <div className="text-sm">
                <span className="text-muted-foreground">End: </span>{slip.endDate}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {(slip.rateOverride != null || slip.notes) && (
        <Card>
          <CardContent className="pt-4 space-y-3">
            {slip.rateOverride != null && (
              <div>
                <span className="text-sm text-muted-foreground block">Rate</span>
                <span className="font-medium">{new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(slip.rateOverride)}/mo</span>
              </div>
            )}
            {slip.notes && (
              <div>
                <span className="text-sm text-muted-foreground block">Notes</span>
                <p className="text-sm">{slip.notes}</p>
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
