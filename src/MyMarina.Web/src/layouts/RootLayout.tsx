import { Outlet } from "@tanstack/react-router";
import { Toaster } from "sonner";
import { useAuthStore } from "@/store/authStore";
import { DemoBanner } from "@/components/DemoBanner";

export function RootLayout() {
  const { token, isDemo } = useAuthStore();

  return (
    <>
      {isDemo && token && <DemoBanner token={token} />}
      <Outlet />
      <Toaster richColors position="top-right" />
    </>
  );
}
