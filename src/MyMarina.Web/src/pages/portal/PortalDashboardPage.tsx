import { useQuery } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import { Ship, FileText, Wrench, Megaphone, AlertCircle } from "lucide-react";
import { getPortalMe, getPortalSlip, getPortalInvoices, getPortalMaintenanceRequests } from "@/api/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";

const STATUS_LABELS: Record<number, string> = {
  0: "Draft", 1: "Sent", 2: "Partially Paid", 3: "Paid", 4: "Overdue", 5: "Voided",
};

function fmt(n: number) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(n);
}

export function PortalDashboardPage() {
  const { data: me } = useQuery({ queryKey: ["portal-me"], queryFn: getPortalMe });
  const { data: slip } = useQuery({ queryKey: ["portal-slip"], queryFn: getPortalSlip });
  const { data: invoices } = useQuery({ queryKey: ["portal-invoices"], queryFn: getPortalInvoices });
  const { data: requests } = useQuery({ queryKey: ["portal-maintenance"], queryFn: getPortalMaintenanceRequests });

  const overdueInvoices = invoices?.filter((i) => i.status === 4) ?? [];
  const totalBalanceDue = invoices?.reduce((sum, i) => sum + i.balanceDue, 0) ?? 0;
  const openRequests = requests?.filter((r) => r.status !== 3) ?? []; // not resolved

  return (
    <div className="p-8 space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Welcome back, {me?.firstName ?? "…"}</h1>
        <p className="text-muted-foreground">{me?.accountDisplayName}</p>
      </div>

      {/* Alert: overdue invoices */}
      {overdueInvoices.length > 0 && (
        <div className="flex items-center gap-3 rounded-md border border-destructive/50 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          <AlertCircle className="h-4 w-4 shrink-0" />
          <span>
            You have {overdueInvoices.length} overdue invoice{overdueInvoices.length > 1 ? "s" : ""}.{" "}
          </span>
          <Link to="/portal/invoices" className="underline font-medium ml-auto">View invoices</Link>
        </div>
      )}

      {/* Summary cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Current Slip</CardTitle>
            <Ship className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {slip ? (
              <>
                <p className="text-xl font-bold">{slip.slipName}</p>
                <p className="text-xs text-muted-foreground">{slip.dockName ? `${slip.dockName} · ` : ""}{slip.marinaName}</p>
              </>
            ) : (
              <p className="text-sm text-muted-foreground">No active slip assignment</p>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Balance Due</CardTitle>
            <FileText className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <p className={`text-xl font-bold ${totalBalanceDue > 0 ? "text-destructive" : ""}`}>
              {fmt(totalBalanceDue)}
            </p>
            <p className="text-xs text-muted-foreground">{invoices?.length ?? 0} invoice{invoices?.length !== 1 ? "s" : ""} total</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Maintenance Requests</CardTitle>
            <Wrench className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <p className="text-xl font-bold">{openRequests.length}</p>
            <p className="text-xs text-muted-foreground">open request{openRequests.length !== 1 ? "s" : ""}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Quick Actions</CardTitle>
            <Megaphone className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent className="space-y-1">
            <Link to="/portal/maintenance">
              <Button variant="link" className="p-0 h-auto text-xs">Submit maintenance request</Button>
            </Link>
            <br />
            <Link to="/portal/announcements">
              <Button variant="link" className="p-0 h-auto text-xs">View announcements</Button>
            </Link>
          </CardContent>
        </Card>
      </div>

      {/* Recent invoices */}
      {invoices && invoices.length > 0 && (
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold">Recent Invoices</h2>
            <Link to="/portal/invoices">
              <Button variant="ghost" size="sm">View all</Button>
            </Link>
          </div>
          <div className="space-y-2">
            {invoices.slice(0, 3).map((inv) => (
              <Link key={inv.id} to="/portal/invoices/$invoiceId" params={{ invoiceId: inv.id }}>
                <div className="flex items-center justify-between rounded-md border px-4 py-3 hover:bg-accent transition-colors">
                  <div>
                    <p className="text-sm font-medium font-mono">{inv.invoiceNumber}</p>
                    <p className="text-xs text-muted-foreground">Due {inv.dueDate}</p>
                  </div>
                  <div className="flex items-center gap-3">
                    <Badge variant={inv.status === 4 ? "destructive" : inv.status === 3 ? "success" : "secondary"}>
                      {STATUS_LABELS[inv.status] ?? inv.status}
                    </Badge>
                    <span className={`text-sm font-medium ${inv.balanceDue > 0 ? "text-destructive" : "text-muted-foreground"}`}>
                      {fmt(inv.balanceDue)} due
                    </span>
                  </div>
                </div>
              </Link>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
