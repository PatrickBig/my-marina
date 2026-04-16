import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { ScrollText, ChevronLeft, ChevronRight } from "lucide-react";
import { getAuditLogs, type AuditLogDto } from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";

function fmtTs(iso: string) {
  return new Date(iso).toLocaleString();
}

export function AuditLogPage() {
  const [page, setPage] = useState(1);
  const [tenantId, setTenantId] = useState("");
  const [userId, setUserId] = useState("");
  const [action, setAction] = useState("");
  const [entityType, setEntityType] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [selected, setSelected] = useState<AuditLogDto | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ["audit-logs", page, tenantId, userId, action, entityType, from, to],
    queryFn: () => getAuditLogs({
      tenantId: tenantId || undefined,
      userId: userId || undefined,
      action: action || undefined,
      entityType: entityType || undefined,
      from: from || undefined,
      to: to || undefined,
      page,
      pageSize: 50,
    }),
    placeholderData: (prev) => prev,
  });

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;

  const handleFilterChange = () => setPage(1);

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Audit Log</h1>
        {data && (
          <p className="text-sm text-muted-foreground">{data.totalCount} total entries</p>
        )}
      </div>

      {/* Filters */}
      <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
        <div className="space-y-1.5">
          <Label className="text-xs text-muted-foreground">Tenant ID</Label>
          <Input
            placeholder="UUID…"
            value={tenantId}
            onChange={(e) => { setTenantId(e.target.value); handleFilterChange(); }}
          />
        </div>
        <div className="space-y-1.5">
          <Label className="text-xs text-muted-foreground">User ID</Label>
          <Input
            placeholder="UUID…"
            value={userId}
            onChange={(e) => { setUserId(e.target.value); handleFilterChange(); }}
          />
        </div>
        <div className="space-y-1.5">
          <Label className="text-xs text-muted-foreground">Action</Label>
          <Input
            placeholder="e.g. invoice.created"
            value={action}
            onChange={(e) => { setAction(e.target.value); handleFilterChange(); }}
          />
        </div>
        <div className="space-y-1.5">
          <Label className="text-xs text-muted-foreground">Entity Type</Label>
          <Input
            placeholder="e.g. Invoice"
            value={entityType}
            onChange={(e) => { setEntityType(e.target.value); handleFilterChange(); }}
          />
        </div>
        <div className="space-y-1.5">
          <Label className="text-xs text-muted-foreground">From</Label>
          <Input
            type="datetime-local"
            value={from}
            onChange={(e) => { setFrom(e.target.value); handleFilterChange(); }}
          />
        </div>
        <div className="space-y-1.5">
          <Label className="text-xs text-muted-foreground">To</Label>
          <Input
            type="datetime-local"
            value={to}
            onChange={(e) => { setTo(e.target.value); handleFilterChange(); }}
          />
        </div>
      </div>

      {isLoading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : !data || data.items.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-muted-foreground gap-3">
          <ScrollText className="h-10 w-10 opacity-30" />
          <p>No audit log entries found.</p>
        </div>
      ) : (
        <>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Timestamp</TableHead>
                <TableHead>Tenant</TableHead>
                <TableHead>User</TableHead>
                <TableHead>Action</TableHead>
                <TableHead>Entity</TableHead>
                <TableHead>IP</TableHead>
                <TableHead className="text-right">Detail</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.items.map((l) => (
                <TableRow key={l.id}>
                  <TableCell className="text-xs text-muted-foreground whitespace-nowrap">{fmtTs(l.timestamp)}</TableCell>
                  <TableCell className="text-sm">{l.tenantName ?? <span className="text-muted-foreground">platform</span>}</TableCell>
                  <TableCell className="text-sm truncate max-w-[160px]">{l.userEmail}</TableCell>
                  <TableCell>
                    <Badge variant="secondary" className="font-mono text-xs">{l.action}</Badge>
                  </TableCell>
                  <TableCell className="text-sm">
                    <span className="font-medium">{l.entityType}</span>
                    <span className="text-muted-foreground text-xs ml-1 font-mono">{l.entityId.slice(0, 8)}…</span>
                  </TableCell>
                  <TableCell className="text-muted-foreground text-xs">{l.ipAddress ?? "—"}</TableCell>
                  <TableCell className="text-right">
                    {(l.before || l.after) && (
                      <Button size="sm" variant="ghost" onClick={() => setSelected(l)}>View</Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          {/* Pagination */}
          <div className="flex items-center justify-between">
            <p className="text-sm text-muted-foreground">
              Page {page} of {totalPages}
            </p>
            <div className="flex gap-2">
              <Button
                size="sm"
                variant="outline"
                disabled={page <= 1}
                onClick={() => setPage((p) => p - 1)}
              >
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <Button
                size="sm"
                variant="outline"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => p + 1)}
              >
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </>
      )}

      {/* Before/After diff dialog */}
      <Dialog open={!!selected} onOpenChange={(open) => { if (!open) setSelected(null); }}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>
              {selected?.action} — {selected?.entityType}
            </DialogTitle>
          </DialogHeader>
          <div className="grid grid-cols-2 gap-4 text-xs">
            <div>
              <p className="font-medium mb-1 text-muted-foreground">Before</p>
              <pre className="bg-muted rounded p-3 overflow-auto max-h-64 whitespace-pre-wrap break-all">
                {selected?.before
                  ? JSON.stringify(JSON.parse(selected.before), null, 2)
                  : "—"}
              </pre>
            </div>
            <div>
              <p className="font-medium mb-1 text-muted-foreground">After</p>
              <pre className="bg-muted rounded p-3 overflow-auto max-h-64 whitespace-pre-wrap break-all">
                {selected?.after
                  ? JSON.stringify(JSON.parse(selected.after), null, 2)
                  : "—"}
              </pre>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setSelected(null)}>Close</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
