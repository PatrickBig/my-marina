import type { MarinaDto, MarinaMetricsDto, HealthTargetsDto } from "@/api/api";
import { getMarinaMetrics, getMarinaHealthTargets } from "@/api/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, AlertCircle, AlertTriangle, CheckCircle } from "lucide-react";
import { Link, useNavigate } from "@tanstack/react-router";
import { HealthTargetsDialog } from "./HealthTargetsDialog";
import { useState } from "react";

interface MarinaCardProps {
  marina: MarinaDto;
}

const toNum = (v: null | number | string | undefined) =>
  v == null ? null : typeof v === "string" ? parseFloat(v) : v;

const toInt = (v: null | number | string | undefined) =>
  v == null ? null : typeof v === "string" ? parseInt(v) : v;

export function MarinaCard({ marina }: MarinaCardProps) {
  const navigate = useNavigate();
  const [showHealthDialog, setShowHealthDialog] = useState(false);

  const { data: metrics, isLoading } = useQuery<MarinaMetricsDto | undefined>({
    queryKey: ["marina-metrics", marina.id],
    queryFn: () => getMarinaMetrics(marina.id),
  });

  const { data: targets } = useQuery<HealthTargetsDto | undefined>({
    queryKey: ["marina-health-targets", marina.id],
    queryFn: () => getMarinaHealthTargets(marina.id),
  });

  const healthStatus = metrics?.healthStatus ?? 0;
  const statusConfig = [
    { color: "bg-green-100 text-green-800", icon: CheckCircle, label: "Healthy" },
    { color: "bg-yellow-100 text-yellow-800", icon: AlertTriangle, label: "Warning" },
    { color: "bg-red-100 text-red-800", icon: AlertCircle, label: "Alert" },
  ];

  const config = statusConfig[Math.min(healthStatus, 2)];
  const StatusIcon = config.icon;

  const getFormattedAR = (amount: string | number | undefined) => {
    const num = typeof amount === "string" ? parseFloat(amount) : (amount ?? 0);
    return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(num);
  };

  const occupancyRate =
    typeof metrics?.occupancyRate === "string"
      ? parseFloat(metrics.occupancyRate)
      : (metrics?.occupancyRate ?? 0);

  const oldestOverdueDays =
    typeof metrics?.oldestOverdueDays === "string"
      ? parseInt(metrics.oldestOverdueDays)
      : (metrics?.oldestOverdueDays ?? 0);

  const occupancyWarning = toNum(targets?.occupancyWarningThreshold);
  const occupancyAlert = toNum(targets?.occupancyAlertThreshold);
  const overdueWarningDays = toInt(targets?.overdueWarningDays);
  const overdueAlertDays = toInt(targets?.overdueAlertDays);

  // Bar color mirrors server-side HealthStatusCalculator logic
  const occupancyBarColor =
    occupancyAlert != null && occupancyRate < occupancyAlert
      ? "bg-red-500"
      : occupancyWarning != null && occupancyRate < occupancyWarning
        ? "bg-amber-500"
        : "bg-green-600";

  const overdueIsAlert = overdueAlertDays != null && oldestOverdueDays > overdueAlertDays;
  const overdueIsWarning = overdueWarningDays != null && oldestOverdueDays > overdueWarningDays;

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <div className="animate-pulse space-y-2">
            <div className="h-5 bg-gray-200 rounded w-3/4"></div>
            <div className="h-4 bg-gray-200 rounded w-1/2"></div>
          </div>
        </CardHeader>
      </Card>
    );
  }

  return (
    <>
      <Card className="hover:shadow-lg transition-shadow">
        <CardHeader>
          <div className="flex items-start justify-between">
            <div className="flex-1">
              <CardTitle className="text-xl">{marina.name}</CardTitle>
              <p className="text-sm text-muted-foreground mt-1">
                {marina.address.city}, {marina.address.state}
              </p>
            </div>
            <Badge className={`${config.color} flex gap-1`}>
              <StatusIcon className="h-3 w-3" />
              {config.label}
            </Badge>
          </div>
        </CardHeader>

        <CardContent className="space-y-4">
          {/* Occupancy */}
          <div className="space-y-2 pb-4 border-b">
            <div className="flex justify-between items-center">
              <h4 className="text-sm font-semibold">Occupancy</h4>
              <span className="text-xs text-muted-foreground">
                {metrics?.occupiedSlips} / {metrics?.totalSlips}
              </span>
            </div>
            <div className="relative w-full bg-gray-200 rounded-full h-2">
              <div
                className={`${occupancyBarColor} h-2 rounded-full`}
                style={{ width: `${Math.min(occupancyRate, 100)}%` }}
              />
              {/* Warning tick (amber) */}
              {occupancyWarning != null && (
                <div
                  className="absolute top-0 bottom-0 w-0.5 bg-amber-500"
                  style={{ left: `${Math.min(occupancyWarning, 100)}%` }}
                  title={`Warning below ${occupancyWarning}%`}
                />
              )}
              {/* Alert tick (red) */}
              {occupancyAlert != null && (
                <div
                  className="absolute top-0 bottom-0 w-0.5 bg-red-500"
                  style={{ left: `${Math.min(occupancyAlert, 100)}%` }}
                  title={`Alert below ${occupancyAlert}%`}
                />
              )}
            </div>
            <div className="flex justify-between items-center">
              <p className="text-xs text-muted-foreground">
                {occupancyRate.toFixed(1)}% occupied
              </p>
              {(occupancyWarning != null || occupancyAlert != null) && (
                <p className="text-xs text-muted-foreground flex gap-2">
                  {occupancyWarning != null && (
                    <span className="text-amber-600">⚠ {occupancyWarning}%</span>
                  )}
                  {occupancyAlert != null && (
                    <span className="text-red-600">✕ {occupancyAlert}%</span>
                  )}
                </p>
              )}
            </div>
          </div>

          {/* Billing & Customers */}
          <div className="space-y-3">
            <div>
              <p className="text-xs text-muted-foreground">Outstanding AR</p>
              <p className="font-semibold">{getFormattedAR(metrics?.outstandingAR)}</p>
            </div>
            {oldestOverdueDays > 0 && (
              <div>
                <div className="flex items-center gap-1">
                  <p className={`text-xs ${overdueIsAlert ? "text-red-600" : overdueIsWarning ? "text-amber-600" : "text-muted-foreground"}`}>
                    Oldest Overdue Invoice
                  </p>
                  {overdueIsAlert && overdueAlertDays != null && (
                    <span className="text-xs text-red-600">(alert: {overdueAlertDays}d)</span>
                  )}
                  {!overdueIsAlert && overdueIsWarning && overdueWarningDays != null && (
                    <span className="text-xs text-amber-600">(warning: {overdueWarningDays}d)</span>
                  )}
                </div>
                <Link
                  to="/invoices"
                  search={{ status: 4, marinaId: marina.id, sortBy: "dueDate", sortDescending: false }}
                  className={`font-semibold hover:underline ${overdueIsAlert ? "text-red-600" : overdueIsWarning ? "text-amber-600" : ""}`}
                >
                  {oldestOverdueDays} days
                </Link>
              </div>
            )}
            <div>
              <p className="text-xs text-muted-foreground">Active Customers</p>
              <p className="font-semibold">{metrics?.activeCustomerCount}</p>
            </div>
          </div>

          {/* Actions */}
          <div className="flex gap-2 pt-2 border-t">
            <Button
              variant="outline"
              size="sm"
              className="flex-1"
              onClick={() => setShowHealthDialog(true)}
            >
              Edit Goals
            </Button>
            <Button
              size="sm"
              className="flex-1"
              onClick={() => navigate({ to: "/marinas/$marinaId/profile", params: { marinaId: marina.id } })}
            >
              View
              <ArrowRight className="h-3 w-3 ml-1" />
            </Button>
          </div>
        </CardContent>
      </Card>

      {showHealthDialog && (
        <HealthTargetsDialog
          marina={marina}
          onClose={() => setShowHealthDialog(false)}
        />
      )}
    </>
  );
}
