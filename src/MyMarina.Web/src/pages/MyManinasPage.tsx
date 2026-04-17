import { useQuery } from "@tanstack/react-query";
import { getMarinas } from "@/api/api";
import { MarinaCard } from "@/components/MarinaCard";
import { Building2 } from "lucide-react";

export function MyMarinasPage() {
  const { data: marinas = [], isLoading } = useQuery({
    queryKey: ["marinas"],
    queryFn: getMarinas,
  });

  if (isLoading) {
    return (
      <div className="p-8">
        <div className="animate-pulse space-y-4">
          <div className="h-8 bg-gray-200 rounded w-1/4"></div>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="h-64 bg-gray-200 rounded"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="p-8 space-y-6">
      <div>
        <h1 className="text-3xl font-bold flex items-center gap-2">
          <Building2 className="h-8 w-8" />
          My Marinas
        </h1>
        <p className="text-muted-foreground mt-2">
          {marinas.length === 0
            ? "No marinas yet. Create your first marina to get started."
            : `You manage ${marinas.length} marina${marinas.length !== 1 ? "s" : ""}`}
        </p>
      </div>

      {marinas.length > 0 ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {marinas.map((marina) => (
            <MarinaCard key={marina.id} marina={marina} />
          ))}
        </div>
      ) : (
        <div className="text-center py-12">
          <Building2 className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
          <p className="text-muted-foreground">No marinas to display</p>
        </div>
      )}
    </div>
  );
}
