import { z } from "zod"

export const pageParam = z.coerce.number().int().min(1).catch(1)
