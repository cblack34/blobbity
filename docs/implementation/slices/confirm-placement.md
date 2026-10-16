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

- `dotnet test rules/Blobbity.Rules.sln --configuration Release`
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

- **Outcome:** Tapping a legal spot previews the drawn tile there instead of placing it. The preview is lifted, scaled up slightly and ringed, and sits at the current Spin turn.
  - A `PlacementStrip` with Place (green) and Cancel (white) shows above the tray only while a preview is up.
  - Another tap moves the preview, Spin turns it, and Cancel clears it with no game change. Place submits through the existing place path.
  - Stack previews sit on top of the covered tile.
  - While the strip shows, the board gains slack and reveals the previewed tile above it.
  - Preview state lives in `TablePlayView`. It is kept only while no recap is pending, the drawn tile is the same, and the spot is still legal.
  - Naming follows the change: `TableView.PlaceRequested` and `TableSession.Place`.
  - No App or Rules change.
- **Verification:** On `8e0df02`, `dotnet test` passed Rules 234 and App 85, and `unity test unity/MissionSplat --mode PlayMode` passed 57 of 57 (14 preview tests). They check that:
  - a tap previews without changing the game;
  - Place commits, Cancel clears, and Spin turns the preview;
  - a second tap moves the preview;
  - a stack preview sits on top;
  - power selection, hand-off, a new game, a different drawn tile and a pending recap each drop the preview, and a safe-area re-render keeps it;
  - a bottom-row preview ends above the strip, and still does after a render that resizes the viewport.

  `placement-preview.png` was added.
- **Deviations:**
  - The hand-off-cover clause was removed from the preview check. While the cover is up, the table already offers no placements and a new tile, so the clause could never decide anything.
  - The renames above, from review.
- **Unresolved gates or risks:** #59 widens the preview to powers and will reuse the strip.
- **Refactor and handoff receipt:** In the PR body and on issue #58.
- **Final PR:** see the PR linked from issue #58.
