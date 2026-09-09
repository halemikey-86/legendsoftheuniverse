import path from "node:path";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: process.env.VITE_API_PROXY ?? "http://localhost:8787",
        changeOrigin: true,
      },
      "/uploads": {
        target: process.env.VITE_API_PROXY ?? "http://localhost:8787",
        changeOrigin: true,
      },
    },
  },
});
