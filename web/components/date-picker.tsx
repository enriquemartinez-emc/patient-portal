"use client"

import { CalendarIcon } from "lucide-react"
import * as React from "react"

import { Button } from "@/components/ui/button"
import { Calendar } from "@/components/ui/calendar"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"
import { dateToValue, valueToDate } from "@/lib/date-value"
import { formatDate } from "@/lib/format"
import { cn } from "@/lib/utils"

export function DatePicker({
  id,
  name,
  min,
  placeholder = "Pick a date",
  "aria-labelledby": labelledBy,
}: {
  id: string
  name: string
  min?: string
  placeholder?: string
  "aria-labelledby"?: string
}) {
  const [value, setValue] = React.useState("")
  const [open, setOpen] = React.useState(false)

  const selected = value ? valueToDate(value) : undefined
  const earliest = min ? valueToDate(min) : undefined

  return (
    <>
      <input type="hidden" name={name} value={value} />
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger
          render={
            <Button
              id={id}
              type="button"
              variant="outline"
              aria-labelledby={labelledBy}
              className={cn(
                "w-fit justify-start font-normal",
                !value && "text-muted-foreground"
              )}
            />
          }
        >
          <CalendarIcon data-icon="inline-start" />
          {value ? formatDate(value) : placeholder}
        </PopoverTrigger>
        <PopoverContent align="start" className="w-auto gap-0 p-0">
          <Calendar
            mode="single"
            selected={selected}
            defaultMonth={selected ?? earliest}
            disabled={earliest ? { before: earliest } : undefined}
            onSelect={(date) => {
              setValue(date ? dateToValue(date) : "")
              setOpen(false)
            }}
            autoFocus
          />
          {value ? (
            <div className="border-t p-2">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => {
                  setValue("")
                  setOpen(false)
                }}
              >
                Clear date
              </Button>
            </div>
          ) : null}
        </PopoverContent>
      </Popover>
    </>
  )
}
