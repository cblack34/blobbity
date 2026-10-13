# Slice plan — Powers by tapping

## Strategic source

- **Active build pack:** The POC pack. This is UX work under #44, tracked in #59.
- **Human approval:** Approved in session on 2026-10-13, including:
  - removing the power chips and "Place instead";
  - rotate as select, turn live, then Confirm or Cancel;
  - bounce as select, then Bounce or Cancel;
  - remaining uses shown by greying spent badges on the drawn tile.
- **Final acceptance advanced:** None directly. The power criteria in [`docs/acceptance.md`](../../acceptance.md) and the rotate-claims rule (#72) must stay green.

## Outcome

When the drawn tile shows rotate or bounce with uses left, the player uses it by tapping a board tile.
- **Tap meaning:** a drawn tile never shows two different powers (still an open ruling that the rules refuse), so a tap on an occupied tile has one meaning:
  - with rotate or bounce left, it selects that tile as the power's target;
  - with stack, it previews a stack placement (#58);
  - otherwise, nothing.
- **Empty spots:** a tap on an empty legal spot always previews a placement.
- **Rotate:** the selected tile lifts with a ring.
  - The strip above the tray shows a turn button (↻), **Confirm**, and **Cancel**.
  - Each turn press rotates the selected tile a quarter on the board, live, as a view-only preview.
  - Confirm submits the rotate with that amount, and is disabled at zero turns. Cancel restores the tile.
  - A rotate that completes a mission claims at once (#72), and the recap shows it.
- **Bounce:** the strip shows **Bounce** and **Cancel**. Bounce submits; Cancel deselects.
- **Removed:** the power chips, "Place instead", and the rotate-amount hint under Spin. Spin turns only the drawn tile for placement.
- **Remaining uses:** each rotate or bounce badge on the drawn tile greys out as its use is spent.

## Why this slice is next

The maintainer asked for direct, confirmable power use (#59), and the confirm strip from #58 is ready to reuse. The power splash (#60) follows.

## Scope

### In scope

- Tap routing for occupied tiles, power selection and preview state, and the strip's rotate and bounce modes.
- Submitting rotate and bounce, greying the spent badges, and removing the chips.
- PlayMode tests and captures.

### Out of scope

- The power splash (#60), pinch-zoom (#52), the configured win count (#51), and any Rules change.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Power criteria in `acceptance.md` | Same rules, a new input path. |
| Rotate claims (#72) | Confirmed rotates claim at once, and the recap shows them. |

## Gates and dependencies

### Hard gates

- None.

### Sequencing recommendations

- Tap routing and selection first, then the strip modes and submission, then badges, chip removal, tests, and captures.

## Architecture and contracts

- **Affected seams:** Unity player (`BoardPanel`, `TablePlayView`, `PreviewStrip` (was `PlacementStrip`), `TurnTray`, `PowerChips` removed, `TableSession`).
- **Public contracts:** Rules and App unchanged. A rotate submits a `UseRotate` with its own quarter turns through the existing submit path. If the table driver needs a change to accept that amount, stop and ask.
- **Data and migration considerations:** none.

## High-level approach

Extend the view's preview state to cover three modes: a placement, a power target, or nothing. Route a tap on an occupied tile by the drawn tile's power and its remaining uses. Draw the rotate preview from the selected tile's cells, turned with the existing quarter-turn mapping. Submit only on Confirm or Bounce, and keep legality with the table's targets and the rules.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- `unity test unity/MissionSplat --mode PlayMode` on freshly copied Release DLLs.
- Captures of a rotate preview and a bounce selection, and the maintainer's look on the phone.

## Risks and stop conditions

- Stop if submitting a rotate amount needs an App or Rules change.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:power-taps` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Apower-taps

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/power-taps`, cut from `origin/main` at `bca4983`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.
