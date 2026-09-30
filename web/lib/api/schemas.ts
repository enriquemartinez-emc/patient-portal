import { z } from "zod"

export function pagedSchema<T extends z.ZodType>(item: T) {
  return z.object({
    items: z.array(item),
    page: z.number(),
    pageSize: z.number(),
    hasNextPage: z.boolean(),
  })
}

export type Paged<T> = {
  items: T[]
  page: number
  pageSize: number
  hasNextPage: boolean
}
