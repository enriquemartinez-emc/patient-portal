import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"

export const metadata = { title: "Settings · Patient Portal" }

export default function SettingsPage() {
  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold">Settings</h1>
      <Card>
        <CardHeader>
          <CardTitle>Nothing to configure yet</CardTitle>
          <CardDescription>
            Account preferences will appear here. To change how the portal
            looks, use Theme in the account menu.
          </CardDescription>
        </CardHeader>
      </Card>
    </div>
  )
}
