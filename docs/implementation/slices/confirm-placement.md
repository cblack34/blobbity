# Slice plan — Confirm placement

## Strategic source

- **Active build pack:** The POC pack passed final acceptance in #43. This is UX work under #44, tracked in #58.
- **Human approval:** Approved in session on 2026-10-13, including the button placement: a strip just above the tray, over the bottom edge of the board, shown only while a preview is up.
- **Final acceptance advanced:** None directly. Placement legality and claims are unchanged, and the criteria in [`docs/acceptance.md`](../../acceptance.md) must stay green.

## Outcome

Tapping a legal spot previews the drawn tile there instead of placing it:
- The tile shows lifted, with a soft outline, at the current Spin turn.
- A large green **Place** and a white **Cancel** appear in a strip just above the tray.
- Tapping another legal spot moves the preview, and Spin turns the previewed tile.
- **Cancel** clears the preview and leaves the game untouched.
- **Place** submits the placement; the recap then plays it and any claims.
- **Stack:** when the drawn tile can stack, tapping an occupied stack target previews it on top, with the same buttons.

## Why this slice is next

A tap placed the tile at once, so a slip of the finger was a move. #59 (powers by tapping) reuses these Place and Cancel buttons, and #60 (the power splash) follows both.

## Scope

### In scope

- The preview state, the Place and Cancel strip, and their wiring to the existing place action.
- PlayMode tests and a capture.

### Out of scope

- Powers by tapping (#59), the power splash (#60), pinch-zoom (#52), and the configured win count (#51).
- Any Rules or App change.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| A claim scores only when the current seat placed the completing tile (non-negotiable 2) | Unchanged: Place submits the same placement. |
| Presentation criteria | Unchanged content; placement gains a confirm step. |

## Gates and dependencies

### Hard gates

- None.

### Sequencing recommendations

- The preview and the strip first, then wire Spin and stack, then tests and the capture.

## Architecture and contracts

- **Affected seams:** Unity player only (`BoardPanel`, `TurnTray` or a new small strip view, `TablePlayView`, `TableSession`).
- **Public contracts:** unchanged. The preview is view state. Place calls the existing `Table.Place`.
- **Data and migration considerations:** none.

## High-level approach

Keep the chosen spot as view state. Draw the drawn tile there with the existing tile painter at the current quarter turns. Clear the preview on Cancel, on a new render that no longer lists the spot as legal, and on any committed action. Place forwards to the existing place path, so legality stays with the table.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- `unity test unity/MissionSplat --mode PlayMode` on freshly copied Release DLLs.
- A preview capture, and the maintainer's look on the phone.

## Risks and stop conditions

- Stop if the preview needs an App change.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:confirm-placement` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Aconfirm-placement

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/confirm-placement`, cut from `origin/main` at `d0ec833`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.
