export const DEMO_PASSWORD = "demo-password"

export const demoAccounts = [
  { email: "emily.carter@demo.example", name: "Emily Carter", role: "patient" },
  { email: "james.wilson@demo.example", name: "James Wilson", role: "patient" },
  {
    email: "sarah.thompson@demo.example",
    name: "Dr. Sarah Thompson",
    role: "clinician",
  },
  {
    email: "michael.brown@demo.example",
    name: "Dr. Michael Brown",
    role: "clinician",
  },
  {
    email: "laura.davies@demo.example",
    name: "Dr. Laura Davies",
    role: "researcher",
  },
] as const
