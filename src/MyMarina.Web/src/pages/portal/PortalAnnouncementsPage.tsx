import { useQuery } from "@tanstack/react-query";
import { Megaphone, Pin } from "lucide-react";
import { getPortalAnnouncements } from "@/api/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";

export function PortalAnnouncementsPage() {
  const { data: announcements, isLoading } = useQuery({
    queryKey: ["portal-announcements"],
    queryFn: getPortalAnnouncements,
  });

  if (isLoading) return <div className="p-8 text-muted-foreground">Loading…</div>;

  return (
    <div className="p-8 space-y-6 max-w-3xl">
      <h1 className="text-2xl font-bold">Announcements</h1>

      {!announcements || announcements.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-md border border-dashed p-16 text-center">
          <Megaphone className="h-10 w-10 text-muted-foreground mb-4" />
          <p className="text-muted-foreground">No announcements at this time.</p>
        </div>
      ) : (
        <div className="space-y-4">
          {announcements.map((a) => (
            <Card key={a.id} className={a.isPinned ? "border-primary" : ""}>
              <CardHeader className="pb-2">
                <div className="flex items-start gap-2">
                  {a.isPinned && <Pin className="h-4 w-4 text-primary mt-0.5 shrink-0" />}
                  <CardTitle className="text-base">{a.title}</CardTitle>
                  {a.isPinned && <Badge variant="secondary" className="ml-auto shrink-0">Pinned</Badge>}
                </div>
                <p className="text-xs text-muted-foreground">
                  {a.marinaName} · {new Date(a.publishedAt).toLocaleDateString()}
                  {a.expiresAt && ` · Expires ${new Date(a.expiresAt).toLocaleDateString()}`}
                </p>
              </CardHeader>
              <CardContent>
                <p className="text-sm whitespace-pre-wrap">{a.body}</p>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
