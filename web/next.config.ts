import type { NextConfig } from "next"

const nextConfig: NextConfig = {
  output: "standalone",
  // Keep `next dev` from writing its agent rules block into AGENTS.md and CLAUDE.md.
  agentRules: false,
}

export default nextConfig
