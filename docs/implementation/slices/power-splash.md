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

- `dotnet test rules/Blobbity.Rules.sln --configuration Release`
- `dotnet test app/Blobbity.App.sln --configuration Release`
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

- **2026-10-13, after the design review:** a drawn tile is marked as announced when its splash is *queued*, not when the splash finishes or is skipped. A cancelled, failed, or instant splash therefore never loops. The approach above said "when its splash queue finishes or is skipped".

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** When the human holding the device can act with a drawn tile that shows a power, one splash per power kind plays after any recap for that human:
  - the power's badge zooms out from its cell on the tray tile, spins, grows to about 3.5 times its size towards the middle of the table, and shrinks back, in about one second;
  - a soft dim sits behind it;
  - the heading reads "You got a rotate!", "You got a bounce!", or "You got a stack!", and the instruction line is blank;
  - a tap skips the current splash, and input is blocked while it plays.

  `RecapPlayer` runs a recap or a splash queue through one `Reel`, so the recap's acknowledgement still fires once. A drawn tile is announced once: re-renders, Spin, and previews don't replay it, and a new game resets it. There is no splash for AI seats, or behind the hand-off cover.
- **Verification:** On `0472728`, Rules 241, App 86, and `unity test unity/MissionSplat --mode PlayMode` 84 of 84, run by the implementation lead. The 14 splash tests cover:
  - a power tile splashes and a plain tile does not;
  - one splash per kind;
  - a tap skips exactly one;
  - the recap plays first and is acknowledged before the splash;
  - no replay on a re-render or Spin, and a returning tile splashes again;
  - no splash for AI seats or behind the cover;
  - input blocked during a splash;
  - a stack tile;
  - the start position equals the tray badge;
  - a throwing splash still ends;
  - the instruction line is blank during a splash.

  `power-splash.png` was added.
- **Deviations:** the amendment above.
- **Unresolved gates or risks:**
  - #76: the player infers "is a power" from the badge art; the App should expose the powers in play.
  - A layout change mid-splash returns the badge to its earlier position for under a second.
- **Refactor and handoff receipt:** In the PR body and on issue #60.
- **Final PR:** see the PR linked from issue #60.
