# Slice plan — Portrait layout

## Strategic source

- **Active build pack:** The POC pack passed final acceptance in #43. This slice is GUI redesign work tracked in #44, built toward the design reference in [`docs/design/`](../../design/README.md).
- **Human approval:** Approved in session on 2026-10-12. The maintainer chose the "splat party" direction from several mock-ups, decided that players see scores rather than a permanent claims row (with a seat's claims opened on tap), chose one Spin button over the four quarter-turn buttons, and accepted critter colours assigned by seat order. The look itself (background splats, toy tiles, legal-spot style, fonts) is the next slice.
- **Final acceptance advanced:** Changes one presentation criterion in [`docs/acceptance.md`](../../acceptance.md), from "every seat's claim row" to "every seat's score, with claims opened on demand", by the maintainer's decision.

## Outcome

On a portrait phone, the table follows the reference layout with today's art: a seat bar of splat critters with names and score pips, the current seat highlighted; a heading naming whose turn it is; the board filling the middle; and a turn tray holding a large drawn tile, one Spin button, power chips with remaining uses, and the secret missions. Tapping a player opens their claimed missions. The claims row is gone. The hand-off cover and the game-end state fit the new layout.

## Why this slice is next

The portrait captures show that most of what looks wrong is layout: a tiny board in long games, empty bands, a tiny drawn tile, badly proportioned buttons, a blank top area at game end, small claim cards, and an oversized hand-off cover. Fixing structure first lets the next slice restyle a layout that already fits the phone.

## Scope

### In scope

- Portrait screen regions and a portrait canvas reference.
- The seat bar: critters (splat plus googly eyes), names, four score pips, current-seat highlight.
- The heading and one instruction line.
- The board sized to its region with a minimum tile size.
- The turn tray: large drawn tile, Spin, power chips, secret missions.
- A tap-a-seat panel showing that seat's claims.
- The hand-off cover over the secret missions only, and a game-end tray showing the winner.
- The design reference in `docs/design/`, the acceptance line, and the architecture render line.
- Play-mode tests for Spin, score pips, and the claims panel; existing tests moved off the quarter-turn buttons; every capture retaken in portrait.

### Out of scope

- The look: background splats and drips, toy tile styling, legal-spot styling, power icons, tilted cards, bundled fonts (next slice).
- The claim pop-up animation, choosing critter colours at setup, and any rules or App change.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| The table shows the board, the current seat's secret missions, and every seat's score, with claims on demand | Seat bar, tray, and claims panel; captures retaken. |
| Missions are hidden between human seats until the incoming seat confirms | The hand-off cover moves with the missions into the tray. |
| No physical-game art ships (non-negotiable 4) | Critters are built from the project's own splat. |

## Gates and dependencies

### Hard gates

- None. Direction and decisions are approved.

### Sequencing recommendations

- Regions first, then the seat bar and tray, then the claims panel, hand-off, and game end, then captures.

## Architecture and contracts

- **Affected seams:** Unity player only (`TableView`, `TablePlayView`, painters). Spin steps the quarter-turn the table driver already holds, so Rules and App are unchanged.
- **Public contracts:** unchanged in code. One acceptance criterion changes as above.
- **Data and migration considerations:** none.

## High-level approach

Lay the screen out from a portrait reference so regions are sized for the phone. Reuse the existing tile and mission-card painters at new sizes. Draw critters from the existing splat sprite tinted per seat, with eyes from the existing circle sprite. Keep Spin as a thin view action over the existing quarter-turn state.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- `unity test unity/MissionSplat --mode PlayMode` on freshly copied Release DLLs.
- Every capture retaken in portrait, and the maintainer's look on the phone.

## Risks and stop conditions

- Stop if Spin or the claims panel seems to need an App or Rules change.
- Stop if the board cannot keep a usable minimum tile size in long games without scrolling or zoom; report it rather than adding either.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:portrait-layout` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Aportrait-layout

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/portrait-layout`, cut from `origin/main` at `083a1d9`.

## Amendments

- **2026-10-12, after the design review, by the maintainer's decisions:**
  - Win count: the seat bar keeps the ordinary 4 for now, documented in code; showing the configured count is #51.
  - Big boards: tiles keep a minimum size and the board scrolls, so every placement stays reachable. This replaces the plan's "stop rather than add scroll" condition. Pinch-zoom is #52.
  - Power chips: shown only when the drawn tile has a power with uses left, as an icon without words. The quarter-turn count chip under Spin is removed; a turn hint shows only while rotate is selected.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** The table follows the splat party layout on a portrait canvas (1080 wide reference):
  - a seat bar of googly-eyed splat critters in seat-order colours, with names, score pips, and the current seat boxed;
  - a heading naming whose turn it is, with one instruction line;
  - a scrolling board panel;
  - a turn tray with a large drawn tile, Spin, and icon-only power chips, which show only for powers the drawn tile still has and grey out with no target;
  - the secret missions;
  - tapping a seat opens its claims, and the claims row is gone;
  - the hand-off cover sits over the missions only, and the tray names the winner at game end.

  New Player classes: `SeatBar`, `TurnTray`, `ClaimsPanel`, `BoardPanel`, `CritterPainter`, `Seats`, `RotateAmount`. No Rules or App change.
- **Verification:** On `05316d4`, `dotnet test` passed Rules 234 and App 66, and `unity test unity/MissionSplat --mode PlayMode` passed 24 of 24, run by the implementation lead. The tests cover Spin, score pips, the claims panel (open, close, closed by a render), the hand-off cover hiding the missions, the winner tray, a drag that scrolls without placing next to a tap that places, scroll memory across renders and its reset for a new game, and a greyed chip. Nine portrait captures are committed; `seat-claims.png` and `board-scrolled.png` are new, and `claim-row.png` is retired.
- **Deviations:** The amendment above, from the maintainer's decisions after the first design review.
- **Unresolved gates or risks:**
  - #51: the configured win count.
  - #52: pinch-zoom.
  - The look slice: toy tiles, background splats, legal-spot and highlight styling, power icons, fonts, two-seat bar spacing, setup restyle.
  - The reshuffle capture still uses a blank-tile fixture.
- **Refactor and handoff receipt:** In the PR body and on issue #50.
- **Final PR:** see the PR linked from issue #50.
