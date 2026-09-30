import { describe, expect, it } from "vitest"

import { participantLabel } from "./research.rules"

describe("participantLabel", () => {
  it("is a short stable reference that carries no name", () => {
    expect(participantLabel("b0000000-0000-0000-0000-00000000abcd")).toBe(
      "Participant 0000ABCD"
    )
  })
})
