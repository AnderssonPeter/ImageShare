# Workflow

1. Read `TODO.md` to find the next pending item.

2. Work on **exactly one** todo item at a time:
   - Find the first unchecked item (`[ ]`).
   - Mark it in progress by changing `[ ]` → `[/]` before starting.
   - Implement only that single item — do not start or batch other items in the same phase.

3. When the item is complete:
   - Verify the change
   - **Always run `pnpm lint:fix`, `pnpm test`, `pnpm test:ui` and `pnpm build` and fix every resulting error** before marking the item done. Warnings are not acceptable, all unit tests and linting errors must be fixed even if they have nothing to do with what was changed. (If you know of a permanent fix to the issue, suggest it to me.)
   - **Never disable lint rules.** Always fix warnings/errors by changing the code you own. Only if you have exhausted every possible code-level fix may you ask the user for permission to disable a rule — never do it without asking first.
   - Mark it done by changing `[/]` → `[X]`.

4. Ask the user to review the change, and allow them to tell you what can be done better.

5. Wait for the user's feedback before moving to the next item. Incorporate any requested fixes before proceeding.

6. Repeat from step 1.
