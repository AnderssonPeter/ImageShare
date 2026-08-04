/// <reference types="vitest" />
import babel from "@rolldown/plugin-babel";
import react, { reactCompilerPreset } from "@vitejs/plugin-react";
import svgr from "vite-plugin-svgr";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig } from "vitest/config";
import { tanstackRouter } from "@tanstack/router-plugin/vite";
import { heyApiPlugin } from "@hey-api/vite-plugin";

export default defineConfig({
  plugins: [
    tailwindcss(),
    tanstackRouter({
      routesDirectory: "./src/routes",
      generatedRouteTree: "./src/routeTree.gen.ts",
      autoCodeSplitting: true,
    }),
    react(),
    babel({ presets: [reactCompilerPreset()] }),
    svgr(),
    heyApiPlugin({
      config: {
        input: "../ImageShare/openapi.json",
        output: {
          path: "src/lib/api/generated",
          clean: true,
          postProcess: ["oxlint", "oxfmt"],
        },
        plugins: [
          "@hey-api/typescript",
          {
            name: "@hey-api/client-fetch",
            throwOnError: true,
          },
          {
            name: "@hey-api/sdk",
          },
          {
            name: "@tanstack/react-query",
          },
        ],
      },
    }),
  ],
  resolve: {
    tsconfigPaths: true,
  },
  server: {
    port: 5000,
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
    include: ["src/**/*.{test,spec}.{ts,tsx}"],
  },
});
