"use client"

import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts"

import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart"
import type { DayActivity } from "@/features/audit/activity"

const config = {
  views: { label: "Times viewed", color: "var(--chart-2)" },
} satisfies ChartConfig

const shortDay = new Intl.DateTimeFormat("en-GB", {
  day: "numeric",
  month: "short",
  timeZone: "UTC",
})

export function AccessActivityChart({ days }: { days: DayActivity[] }) {
  const total = days.reduce((sum, day) => sum + day.views, 0)

  if (total === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        No one has opened your results in the last {days.length} days. When a
        clinician or researcher does, it shows up here.
      </p>
    )
  }

  const data = days.map((day) => ({
    ...day,
    label: shortDay.format(new Date(day.date)),
  }))

  return (
    <>
      <ChartContainer
        config={config}
        className="h-48 w-full"
        role="img"
        aria-label={`Times your results were opened each day over the last ${days.length} days: ${total} in total.`}
      >
        <BarChart data={data} margin={{ left: -16, right: 4 }}>
          <CartesianGrid vertical={false} />
          <XAxis
            dataKey="label"
            tickLine={false}
            axisLine={false}
            tickMargin={8}
            minTickGap={28}
          />
          <YAxis
            allowDecimals={false}
            tickLine={false}
            axisLine={false}
            width={32}
          />
          <ChartTooltip
            cursor={false}
            content={
              <ChartTooltipContent
                hideIndicator
                labelFormatter={(_, payload) => payload?.[0]?.payload?.label}
              />
            }
          />
          <Bar
            dataKey="views"
            fill="var(--color-views)"
            radius={[4, 4, 0, 0]}
            maxBarSize={24}
            isAnimationActive={false}
          />
        </BarChart>
      </ChartContainer>
      {/* The same numbers as a table, for screen readers. */}
      <table className="sr-only">
        <caption>Times your results were opened each day</caption>
        <thead>
          <tr>
            <th>Day</th>
            <th>Times opened</th>
          </tr>
        </thead>
        <tbody>
          {data.map((day) => (
            <tr key={day.date}>
              <td>{day.label}</td>
              <td>{day.views}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  )
}
