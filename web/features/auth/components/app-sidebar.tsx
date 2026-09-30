"use client"

import { HeartPulse } from "lucide-react"
import Link from "next/link"
import { usePathname } from "next/navigation"

import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
} from "@/components/ui/sidebar"
import { UserMenu } from "@/features/auth/components/user-menu"
import { navigation } from "@/features/auth/navigation"
import type { SessionKind } from "@/features/auth/types"

export function AppSidebar({
  kind,
  name,
  signOutAction,
}: {
  kind: SessionKind
  name: string
  signOutAction: () => Promise<void>
}) {
  const pathname = usePathname()

  return (
    <Sidebar collapsible="icon">
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" render={<Link href={`/${kind}`} />}>
              <div className="flex size-8 items-center justify-center rounded-lg bg-primary text-primary-foreground">
                <HeartPulse className="size-4" />
              </div>
              <span className="font-semibold">Patient Portal</span>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>

      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <nav aria-label="Main">
              <SidebarMenu>
                {navigation[kind].map((item) => {
                  const current =
                    pathname === item.href ||
                    item.activeOn.some((path) => pathname.startsWith(path))
                  const Icon = item.icon
                  return (
                    <SidebarMenuItem key={item.href}>
                      <SidebarMenuButton
                        render={<Link href={item.href} />}
                        isActive={current}
                        tooltip={item.label}
                        aria-current={current ? "page" : undefined}
                      >
                        <Icon />
                        <span>{item.label}</span>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  )
                })}
              </SidebarMenu>
            </nav>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      <SidebarFooter>
        <UserMenu name={name} role={kind} signOutAction={signOutAction} />
      </SidebarFooter>

      <SidebarRail />
    </Sidebar>
  )
}
