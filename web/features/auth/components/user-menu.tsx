"use client"

import { LogOut, Monitor, Moon, Settings, Sun } from "lucide-react"
import Link from "next/link"
import { useTheme, type Theme } from "@/components/theme-provider"

import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

// "Dr. Sarah Thompson" -> "ST": first and last name, skipping titles such as "Dr.".
function initials(name: string): string {
  const words = name.split(" ").filter((word) => word && !word.endsWith("."))
  const first = words[0]?.[0] ?? ""
  const last = words.length > 1 ? words[words.length - 1][0] : ""
  return (first + last).toUpperCase()
}

export function UserMenu({
  name,
  role,
  signOutAction,
}: {
  name: string
  role: string
  signOutAction: () => Promise<void>
}) {
  const { theme, setTheme } = useTheme()

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={
          <Button
            variant="ghost"
            className="h-9 gap-2 px-2"
            aria-label="Account menu"
          />
        }
      >
        <Avatar size="sm">
          <AvatarFallback>{initials(name)}</AvatarFallback>
        </Avatar>
        <span className="hidden text-sm sm:inline">{name}</span>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuGroup>
          <DropdownMenuLabel className="flex flex-col gap-0.5">
            <span className="text-sm font-medium text-foreground">{name}</span>
            <span className="text-xs font-normal capitalize">{role}</span>
          </DropdownMenuLabel>
        </DropdownMenuGroup>

        <DropdownMenuSeparator />

        <DropdownMenuGroup>
          <DropdownMenuLabel>Theme</DropdownMenuLabel>
          <DropdownMenuRadioGroup
            value={theme}
            onValueChange={(value) => setTheme(value as Theme)}
          >
            <DropdownMenuRadioItem value="light">
              <Sun /> Light
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="dark">
              <Moon /> Dark
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="system">
              <Monitor /> System
            </DropdownMenuRadioItem>
          </DropdownMenuRadioGroup>
        </DropdownMenuGroup>

        <DropdownMenuSeparator />

        <DropdownMenuItem render={<Link href="/settings" />}>
          <Settings /> Settings
        </DropdownMenuItem>
        <form action={signOutAction}>
          {/* Stays open on click so the form submits before the menu unmounts. */}
          <DropdownMenuItem
            closeOnClick={false}
            render={<button type="submit" className="w-full" />}
          >
            <LogOut /> Log out
          </DropdownMenuItem>
        </form>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
