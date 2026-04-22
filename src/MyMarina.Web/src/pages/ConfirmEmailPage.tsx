import { useEffect, useState } from "react";
import { Link } from "@tanstack/react-router";
import { Anchor, CheckCircle, XCircle, Loader2 } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { confirmEmail } from "@/api/api";

type State = "loading" | "success" | "error";

export function ConfirmEmailPage() {
  const [state, setState] = useState<State>("loading");
  const [message, setMessage] = useState("");

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const userId = params.get("userId") ?? "";
    const token = params.get("token") ?? "";

    if (!userId || !token) {
      setState("error");
      setMessage("Confirmation link is missing required parameters.");
      return;
    }

    confirmEmail(userId, token)
      .then((res) => {
        setState("success");
        setMessage(res.message);
      })
      .catch(() => {
        setState("error");
        setMessage("Confirmation link is invalid or has expired.");
      });
  }, []);

  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-gray-50 p-4">
      <div className="flex items-center gap-2 mb-8">
        <Anchor className="h-6 w-6 text-blue-600" />
        <span className="text-xl font-bold text-gray-900">MyMarina</span>
      </div>

      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          {state === "loading" && (
            <>
              <Loader2 className="h-10 w-10 text-blue-600 mx-auto mb-2 animate-spin" />
              <CardTitle>Confirming your email…</CardTitle>
            </>
          )}
          {state === "success" && (
            <>
              <CheckCircle className="h-10 w-10 text-green-600 mx-auto mb-2" />
              <CardTitle>Email confirmed!</CardTitle>
            </>
          )}
          {state === "error" && (
            <>
              <XCircle className="h-10 w-10 text-red-600 mx-auto mb-2" />
              <CardTitle>Confirmation failed</CardTitle>
            </>
          )}
          {message && <CardDescription className="mt-1">{message}</CardDescription>}
        </CardHeader>
        {state !== "loading" && (
          <CardContent className="flex justify-center">
            <Button asChild>
              <Link to="/login">Go to login</Link>
            </Button>
          </CardContent>
        )}
      </Card>
    </div>
  );
}
