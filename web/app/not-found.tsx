import Link from "next/link"

import { Button } from "@/components/ui/button"

export default function NotFound() {
  return (
    <main className="mx-auto flex min-h-svh w-full max-w-md flex-col items-start justify-center gap-4 px-6">
      <h1 className="text-2xl font-semibold">Page not found</h1>
      <p className="text-muted-foreground">
        It may have moved, or it isn&apos;t something you can see.
      </p>
      <Button render={<Link href="/" />}>Go to the start</Button>
    </main>
  )
}
