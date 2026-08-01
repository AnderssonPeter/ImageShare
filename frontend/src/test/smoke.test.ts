import { describe, expect, it } from "vitest";

describe("smoke", () => {
  it("runs the test harness", () => {
    expect.assertions(1);
    const expected = 2;
    expect(1 + 1).toBe(expected);
  }, 1000);
});
