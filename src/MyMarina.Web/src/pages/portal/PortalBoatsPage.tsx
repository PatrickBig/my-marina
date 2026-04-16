import { useQuery } from "@tanstack/react-query";
import { Sailboat } from "lucide-react";
import { getPortalBoats } from "@/api/api";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";

const BOAT_TYPE_LABELS: Record<number, string> = {
  0: "Powerboat", 1: "Sailboat", 2: "Personal Watercraft", 3: "Catamaran",
  4: "Trawler", 5: "Houseboat", 6: "Dinghy", 7: "Other",
};

export function PortalBoatsPage() {
  const { data: boats, isLoading } = useQuery({ queryKey: ["portal-boats"], queryFn: getPortalBoats });

  if (isLoading) return <div className="p-8 text-muted-foreground">Loading…</div>;

  return (
    <div className="p-8 space-y-6">
      <h1 className="text-2xl font-bold">My Boats</h1>

      {!boats || boats.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-md border border-dashed p-16 text-center">
          <Sailboat className="h-10 w-10 text-muted-foreground mb-4" />
          <p className="text-muted-foreground">No registered boats yet.</p>
          <p className="text-xs text-muted-foreground mt-1">Contact your marina to add a vessel to your account.</p>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {boats.map((boat) => (
            <Card key={boat.id}>
              <CardContent className="pt-4 space-y-3">
                <div className="flex items-start justify-between">
                  <div>
                    <p className="font-semibold text-lg">{boat.name}</p>
                    {(boat.make || boat.model) && (
                      <p className="text-sm text-muted-foreground">
                        {[boat.make, boat.model, boat.year].filter(Boolean).join(" · ")}
                      </p>
                    )}
                  </div>
                  <Badge variant="secondary">{BOAT_TYPE_LABELS[boat.boatType] ?? "Other"}</Badge>
                </div>

                <div className="grid grid-cols-3 gap-2 text-sm">
                  <div>
                    <span className="text-muted-foreground block text-xs">Length</span>
                    <span>{boat.length} ft</span>
                  </div>
                  <div>
                    <span className="text-muted-foreground block text-xs">Beam</span>
                    <span>{boat.beam} ft</span>
                  </div>
                  <div>
                    <span className="text-muted-foreground block text-xs">Draft</span>
                    <span>{boat.draft} ft</span>
                  </div>
                </div>

                {boat.registrationNumber && (
                  <div className="text-sm">
                    <span className="text-muted-foreground">Reg #: </span>{boat.registrationNumber}
                  </div>
                )}

                {boat.insuranceExpiresOn && (
                  <div className="text-sm">
                    <span className="text-muted-foreground">Insurance expires: </span>{boat.insuranceExpiresOn}
                  </div>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
