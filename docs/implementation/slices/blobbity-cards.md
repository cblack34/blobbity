# Slice plan — Blobbities

## Strategic source

- **Active build pack:** The POC pack passed final acceptance in #43. This slice is GUI redesign work tracked in #44, outside that acceptance; the maintainer chose to redesign slice by slice against #44 and the inspiration mock-ups.
- **Human approval:** Approved in session on 2026-10-11. The maintainer asked that blobbities have playing-card proportions (5:7, portrait) while tiles stay square, and accepted folding in the wild-cell fix. After the phone check, the maintainer removed captions and asked for bigger splats with a diagonal row.
- **Final acceptance advanced:** None directly. The presentation criteria in [`docs/acceptance.md`](../../acceptance.md) must stay green.

## Outcome

Blobbities, both a seat's secret blobbities and every seat's claims row, are portrait playing cards at 5:7. Each card has a cream face, an edge, and a shadow, drawn the same layered way as tile cards. Its pattern (row, square, or L) is drawn as tinted splats in the upper part, with a readable caption below. Wild cells on tiles draw their four quarters unrotated, so they read as one split splat rather than a pinwheel.

## Why this slice is next

The tile cards (#47) made the board read as cards, but blobbities still show flat dots on white boxes. On the phone, the secret blobbities are tall boxes and the claim cards near-squares. The maintainer noticed both in the phone check.

## Scope

### In scope

- `art/blobbity-card.svg` (5:7) and its sprite export via `art/export.sh`.
- One blobbity-card routine for the secrets row and the claims row, holding 5:7 wherever it is placed.
- Tinted splats for the pattern, with a stable angle per card.
- The unrotated wild cell.
- Retaken `secrets` and `claim-row` captures.

### Out of scope

- Where the rows sit and how large they are (turn panel and layout slice), the hand-off conceal screen, the seat bar, the board backdrop, power icons.
- Any Rules or App change.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Table shows the current seat's blobbities and every seat's claim row (`acceptance.md`) | Same content, redrawn; captures retaken. |
| No physical-game art ships (non-negotiable 4) | Plain rounded card and the maintainer's own splat. |

## Gates and dependencies

### Hard gates

- None.

### Sequencing recommendations

- Art, then the shared routine, then captures.

## Architecture and contracts

- **Affected seams:** Unity player rendering only: blobbity drawing (formerly `CellPainter.PaintMission`), the card layers shared with `TilePainter`, and the `TablePlayView` card slots.
- **Public contracts:** unchanged.
- **Data and migration considerations:** none.

## High-level approach

Give the blobbity its own 5:7 sprite rather than stretching the square tile sprite, which would distort the corners. Keep the proportions with uGUI's `AspectRatioFitter` inside whatever slot the layout provides. Reuse the card-layer drawing and the splat sprite from the tile cards.

## Verification

- `dotnet test rules/Blobbity.Rules.sln --configuration Release`
- `dotnet test app/Blobbity.App.sln --configuration Release`
- `unity test unity/Blobbity --mode PlayMode` on freshly copied Release DLLs.
- Retaken captures in the PR, and the maintainer's look on the phone.

## Risks and stop conditions

- Stop if a 5:7 card cannot fit a row without a layout change; report it rather than redesigning the layout.
- Stop if a change outside the Unity player seems necessary.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:blobbity-cards` label; this plan links the query, not the issues: https://github.com/cblack34/blobbity/issues?q=label%3Aslice%3Ablobbity-cards

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/mission-cards`, cut from `origin/main` at `88541ef`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** Secret blobbities and every claims row draw as portrait 5:7 cards: the shared shadow, edge, and cream face from `CardPainter`, and the pattern in tinted splats filling the face on a 2 × 4 lattice, with no caption. A row is drawn diagonally, as the physical cards show a line; the rules allow any direction. `BlobbityCardPainter` is the one routine; `AspectRatioFitter` keeps the sprite's own ratio in any slot. Wild cells draw their quarters unrotated, so they read as one split splat.
- **Verification:** On `ada3e5a`, `dotnet test` passed Rules 234 and App 66, and `unity test unity/Blobbity --mode PlayMode` passed 13 of 13 on fresh Release DLLs; the thirteenth test checks that secret and claim slots each hold a 5:7 card with four splats. The maintainer play-tested the result and found it good. The Unity run used a scratch copy of the project, because the maintainer's editor had the project open. Every capture is now rendered in phone portrait at 1179×2556 instead of 1920×1080 landscape. A fresh-context design review raised 2 [Issue] and 7 [Suggestion], with no blockers; both issues and most suggestions are applied (see the receipt).
- **Deviations:** The maintainer's phone check found the cards wrong in portrait: oversized, uneven captions, and claim cards that looked cut off at the bottom. The maintainer then chose to drop captions entirely and enlarge the splats, with the row drawn diagonally; claim slots returned to the strip's bottom. Card edges and shadows gained a minimum thickness (3 and 2 canvas units), so small cards keep their outline when the simulator window downscales. A portrait render never reproduced a true clip. Captures moved to portrait, so they match the device. The plan's "readable caption" outcome is superseded by this choice.
- **Unresolved gates or risks:** Portrait layout problems, all for the layout slice:
  - The board is tiny in long games.
  - There are large empty bands.
  - The pending tile is tiny next to the secret blobbities.
  - The quarter-turn and power buttons are badly proportioned.
  - The pending and secrets areas are blank at game end.
  - Claim cards are small.
  - The conceal overlay is larger than the card area it covers. Highlight tints still muddy the board (#44).
- **Refactor and handoff receipt:** In the PR body and on issue #48.
- **Final PR:** see the PR linked from issue #48.
