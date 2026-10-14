# Slice plan — Power splash

## Strategic source

- **Active build pack:** The POC pack. This is UX work under #44, tracked in #60.
- **Human approval:** Approved in session on 2026-10-13: one splash per power kind on the drawn tile, stack included, played after the recap, for the seat holding the device, about one second, with a tap to skip.
- **Final acceptance advanced:** None directly. Existing criteria must stay green.

## Outcome

When the human holding the device has a drawn tile showing a power (rotate, bounce, or stack), that power's badge plays a short splash, about one second per power kind, before the player can act.
- The badge zooms out from its cell on the drawn tile in the tray, spins as it grows towards the centre of the screen, then shrinks back into the cell.
- A tap skips the current splash.
- **Order:** it plays once per drawn tile, after any recap for that human. In pass-and-play, that means after Confirm and after the recap.
- **No replay:** a re-render, Spin, or preview does not replay it.
- **AI seats:** they get no splash.

## Why this slice is next

It is the last of the turn-flow items the maintainer asked for (#58, #59, #60). It tells a player, a child especially, that they drew a power.

## Scope

### In scope

- The splash animation and its sequencing after the recap, one per power kind.
- Remembering which drawn tile has been announced.
- PlayMode tests and one capture.

### Out of scope

- Pinch-zoom (#52), the configured win count (#51), the App cleanup (#74), and sound.
- Any Rules or App change.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Presentation criteria | Unchanged content; a short announcement before acting. |
| Turn recap timing | The splash follows the recap and uses the same input gating. |

## Gates and dependencies

### Hard gates

- None.

### Sequencing recommendations

- The animation first, then the sequencing after the recap, then the no-replay rule, then tests and the capture.

## Architecture and contracts

- **Affected seams:** Unity player only. The recap player and stage (`RecapPlayer`, `RecapStage`, `RecapAnimations`, or a sibling), `TurnTray` (the badge positions on the drawn tile), and `TablePlayView`.
- **Public contracts:** unchanged.
- **Data and migration considerations:** none.

## High-level approach

Reuse the recap player's overlay, input blocking, and per-item skip. Queue one splash item per power kind on the drawn tile after the recap's items for the holder. Mark the drawn tile as announced when its splash queue finishes or is skipped.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- `unity test unity/MissionSplat --mode PlayMode` on freshly copied Release DLLs.
- One mid-splash capture, and the maintainer's look on the phone.

## Risks and stop conditions

- Stop if the splash needs an App change, for example to know which drawn tile the holder has already seen.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:power-splash` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Apower-splash

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/power-splash`, cut from `origin/main` at `dbd52f3`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.
