import {
  Activity,
  Bug,
  Droplets,
  FlaskConical,
  HeartPulse,
  Shield,
  TestTube,
  type LucideIcon,
} from "lucide-react"

import { cn } from "@/lib/utils"
import { Badge } from "@/components/ui/badge"
import type { LabCategory } from "@/features/lab-results/types"

export const categoryIcons: Record<LabCategory, LucideIcon> = {
  hematology: Droplets,
  biochemistry: FlaskConical,
  lipids: HeartPulse,
  endocrinology: Activity,
  immunology: Shield,
  microbiology: Bug,
  urinalysis: TestTube,
}

export function CategoryIconTile({
  category,
  size = "md",
}: {
  category: LabCategory
  size?: "sm" | "md"
}) {
  const Icon = categoryIcons[category]
  return (
    <div
      className={cn(
        "flex items-center justify-center rounded-lg bg-(--lab)/12 text-(--lab)",
        size === "sm" ? "size-7" : "size-9"
      )}
      style={{ "--lab": `var(--lab-${category})` } as React.CSSProperties}
    >
      <Icon className={cn(size === "sm" ? "size-4" : "size-5")} aria-hidden />
    </div>
  )
}

export function CategoryBadge({ category }: { category: LabCategory }) {
  const Icon = categoryIcons[category]
  return (
    <Badge
      variant="outline"
      className="border-transparent bg-(--lab)/12 text-(--lab) capitalize"
      style={{ "--lab": `var(--lab-${category})` } as React.CSSProperties}
    >
      <Icon data-icon="inline-start" />
      {category}
    </Badge>
  )
}
