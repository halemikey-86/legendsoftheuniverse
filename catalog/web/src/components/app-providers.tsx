import { useState, type ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { Toaster } from "sonner";

export function AppProviders({ children }: { children: ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: { staleTime: 15_000, refetchOnWindowFocus: false },
        },
      }),
  );

  return (
    <QueryClientProvider client={queryClient}>
      {children}
      <Toaster
        theme="dark"
        position="bottom-center"
        toastOptions={{
          className:
            "border-border bg-card text-foreground font-sans shadow-[var(--shadow-border)]",
        }}
      />
    </QueryClientProvider>
  );
}
