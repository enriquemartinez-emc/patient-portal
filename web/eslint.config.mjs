import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";

const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTs,
  // core/ is the pure functional core: no framework, no I/O, no other layer.
  {
    files: ["core/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: [
                "@/app/*",
                "@/features/*",
                "@/components/*",
                "@/lib/*",
                "**/app/**",
                "**/features/**",
                "react",
                "react-dom",
                "next",
                "next/*",
                "zod",
              ],
              message:
                "core/ must stay pure: no framework, I/O or imports from other layers.",
            },
          ],
        },
      ],
    },
  },
  // BFF: only lib/api may make HTTP calls, and it is `server-only`.
  {
    files: ["**/*.{ts,tsx}"],
    ignores: ["lib/api/**"],
    rules: {
      "no-restricted-globals": [
        "error",
        {
          name: "fetch",
          message:
            "Only lib/api may call fetch. The browser never calls the API; go through a Server Component or Server Action.",
        },
      ],
    },
  },
  // Override default ignores of eslint-config-next.
  globalIgnores([
    // Default ignores of eslint-config-next:
    ".next/**",
    "out/**",
    "build/**",
    "next-env.d.ts",
  ]),
]);

export default eslintConfig;
