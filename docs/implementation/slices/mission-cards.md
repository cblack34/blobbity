# Slice plan — Mission cards

## Strategic source

- **Active build pack:** The POC pack passed final acceptance in #43. This slice is GUI redesign work tracked in #44, outside that acceptance; the maintainer chose to redesign slice by slice against #44 and the inspiration mock-ups.
- **Human approval:** Approved in session on 2026-10-11. The maintainer asked that mission cards have playing-card proportions (5:7, portrait) while tiles stay square, and accepted folding in the wild-cell fix.
- **Final acceptance advanced:** None directly. The presentation criteria in [`docs/acceptance.md`](../../acceptance.md) must stay green.

## Outcome

Mission cards, both a seat's secret missions and every seat's claims row, are portrait playing cards at 5:7. Each card has a cream face, an edge, and a shadow, drawn the same layered way as tile cards. Its pattern (row, square, or L) is drawn as tinted splats in the upper part, with a readable caption below. Wild cells on tiles draw their four quarters unrotated, so they read as one split splat rather than a pinwheel.

## Why this slice is next

The tile cards (#47) made the board read as cards, but mission cards still show flat dots on white boxes. On the phone, the secret cards are tall boxes and the claim cards near-squares. The maintainer noticed both in the phone check.

## Scope

### In scope

- `art/mission-card.svg` (5:7) and its sprite export via `art/export.sh`.
- One mission-card routine for the secrets row and the claims row, holding 5:7 wherever it is placed.
- Tinted splats for the pattern, with a stable angle per card.
- The unrotated wild cell.
- Retaken `secrets` and `claim-row` captures.

### Out of scope

- Where the rows sit and how large they are (turn panel and layout slice), the hand-off conceal screen, the seat bar, the board backdrop, power icons.
- Any Rules or App change.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Table shows the current seat's missions and every seat's claim row (`acceptance.md`) | Same content, redrawn; captures retaken. |
| No physical-game art ships (non-negotiable 4) | Plain rounded card and the maintainer's own splat. |

## Gates and dependencies

### Hard gates

- None.

### Sequencing recommendations

- Art, then the shared routine, then captures.

## Architecture and contracts

- **Affected seams:** Unity player rendering only (`CellPainter.PaintMission`, `TilePainter`, `TablePlayView` card slots).
- **Public contracts:** unchanged.
- **Data and migration considerations:** none.

## High-level approach

Give the mission card its own 5:7 sprite rather than stretching the square tile sprite, which would distort the corners. Keep the proportions with uGUI's `AspectRatioFitter` inside whatever slot the layout provides. Reuse the card-layer drawing and the splat sprite from the tile cards.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- `unity test unity/MissionSplat --mode PlayMode` on freshly copied Release DLLs.
- Retaken captures in the PR, and the maintainer's look on the phone.

## Risks and stop conditions

- Stop if a 5:7 card cannot fit a row without a layout change; report it rather than redesigning the layout.
- Stop if a change outside the Unity player seems necessary.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:mission-cards` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Amission-cards

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/mission-cards`, cut from `origin/main` at `88541ef`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.
