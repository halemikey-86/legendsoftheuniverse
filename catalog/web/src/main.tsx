import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { AppProviders } from "@/components/app-providers";
import { WillboundApp } from "@/components/willbound-app";
import "@/styles.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <AppProviders>
      <WillboundApp initialCards={[]} />
    </AppProviders>
  </StrictMode>,
);
