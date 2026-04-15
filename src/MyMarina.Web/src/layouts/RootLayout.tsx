import { Outlet } from "@tanstack/react-router";
import { Toaster } from "sonner";

export function RootLayout() {
  return (
    <>
      <Outlet />
      <Toaster richColors position="top-right" />
    </>
  );
}
