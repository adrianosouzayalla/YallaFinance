import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";

export default defineConfig({
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  test: {
    environment: "jsdom",
    // Limit concurrent jsdom/MUI work so component tests keep their 5s timeout
    // even when the full suite runs on machines with limited resources.
    maxWorkers: 2,
    setupFiles: ["./src/test/setup.ts"],
    css: true,
  },
});
