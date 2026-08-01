import "./index.css";
import { type ReactNode, StrictMode } from "react";
import { queryClient, router } from "./router";
import { QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";
import { createRoot } from "react-dom/client";

const rootElement: HTMLElement | null = document.querySelector("#root");
if (rootElement === null) {
  throw new Error("Root element #root not found");
}

const children: ReactNode = (
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>
);

createRoot(rootElement).render(children);
