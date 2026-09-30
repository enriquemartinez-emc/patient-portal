import type { Metadata } from "next"
import { Geist_Mono, Manrope } from "next/font/google"

import "./globals.css"
import { ThemeProvider } from "@/components/theme-provider"
import { cn } from "@/lib/utils"

export const metadata: Metadata = {
  title: "Patient Portal",
}

const manrope = Manrope({ subsets: ["latin"], variable: "--font-sans" })

const fontMono = Geist_Mono({
  subsets: ["latin"],
  variable: "--font-mono",
})

// Sets the theme before first paint so the page never flashes the wrong one. It mirrors what
// next-themes applies (class + color-scheme, storage key "theme"). It lives here, in a server
// component, because React 19 logs an error for script tags that client components create, which
// is why ThemeProvider turns next-themes' own script into an inert data block.
const themeScript = `try{var t=localStorage.getItem("theme")||"system",d=t==="dark"||(t==="system"&&matchMedia("(prefers-color-scheme: dark)").matches),r=document.documentElement;r.classList.remove("light","dark");r.classList.add(d?"dark":"light");r.style.colorScheme=d?"dark":"light"}catch(e){}`

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html
      lang="en"
      suppressHydrationWarning
      className={cn(
        "antialiased",
        fontMono.variable,
        "font-sans",
        manrope.variable
      )}
    >
      <head>
        <script dangerouslySetInnerHTML={{ __html: themeScript }} />
      </head>
      <body>
        <ThemeProvider>{children}</ThemeProvider>
      </body>
    </html>
  )
}
