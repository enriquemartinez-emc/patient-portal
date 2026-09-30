import { z } from "zod"

// A ?page= query value: a whole number from 1, anything else reads as page 1.
export const pageParam = z.coerce.number().int().min(1).catch(1)
