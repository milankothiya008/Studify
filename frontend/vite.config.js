import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    // Must match "FrontendUrl" in backend/appsettings.json (CORS)
    port: 5173,
  },
});
