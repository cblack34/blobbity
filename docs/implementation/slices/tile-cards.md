# Slice plan — Tile cards

## Strategic source

- **Active build pack:** The POC pack ([`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)) passed final acceptance in #43. This slice is GUI redesign work tracked in #44, outside that acceptance.
- **Human approval:** Approved in session on 2026-10-11. The maintainer chose to redesign the GUI slice by slice against #44 and two inspiration mock-ups, without writing a new UI pack first. The maintainer supplied the splat artwork (AI-generated, not copied from the physical game), chose PNG sprites with the SVG kept as the source, approved installing `librsvg` as a dev tool, and accepted a wild cell drawn as one splat split into the four colours.
- **Final acceptance advanced:** None directly. The presentation criteria in [`docs/acceptance.md`](../../acceptance.md) must stay green.

## Outcome

Every tile on the board, and the drawn tile in the turn panel, reads as its own card: a cream rounded card with a thin edge and a soft shadow, with a small gap between neighbours. Each colour cell shows the splat, tinted to its colour. The board and the turn panel draw tiles with one shared routine.

## Why this slice is next

Placed tiles currently merge into one surface: the board paints each tile's background flush against its neighbours, then paints cells over the top. Tiles always occupy their own 2×2 block, so a separate card per tile fixes this. Everything else in the redesign sits on top of the tile.

## Scope

### In scope

- Art sources in `art/` (`splat.svg`, `tile-card.svg`) and `art/export.sh`, which rasterizes them to white PNG sprites under `unity/MissionSplat/Assets/Art/`.
- One tile-card drawing routine used by the board and the turn panel.
- Splat cells with a fixed, varied rotation per cell, the same on every run.
- The wild cell as one splat split into four colours.
- Retaken `board`, `highlights`, and `after-bounce` captures.

### Out of scope

- The board backdrop and legal-spot glow, power icons, blank cells (they keep today's marks, drawn on the card), blobbities, the seat bar, buttons, and the pass-to-seat screen.
- Any UX change.
- Any Rules or App change.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| No physical-game art ships (non-negotiable 4) | The splat is the maintainer's AI-generated artwork; the card is a plain rounded square. |
| Presentation criteria in `acceptance.md` | Captures retaken; play-mode tests still pass. |
| #44: on-top tints muddy the cells | Unchanged here; the gap between cards keeps each tile readable under a tint. |

## Gates and dependencies

### Hard gates

- None. Approval and tool install are done.

### Sequencing recommendations

- Art and export first, then the shared routine, then captures.

## Architecture and contracts

- **Affected seams:** Unity player rendering only (`TablePlayView`, `CellPainter`).
- **Public contracts:** unchanged.
- **Data and migration considerations:** none.

## High-level approach

Keep artwork as SVG source and ship tintable white PNGs, the usual practice for uGUI. Draw a card as a few layers of one rounded-square sprite (shadow, edge, face), inset inside the tile's 2×2 block. Tap targets keep covering the whole block, gap included. Lay the cells over the face, and draw splat cells with a rotation derived from the tile and cell so the board looks hand-made but stable.

## Verification

- `dotnet test rules/Blobbity.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- `unity test unity/MissionSplat --mode PlayMode` on freshly copied Release DLLs.
- Retaken captures in the PR, and the maintainer's look on the phone.

## Risks and stop conditions

- Stop if tap targets shrink to the inset card; taps in the gap must still land.
- Stop if a change outside the Unity player seems necessary.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:tile-cards` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Atile-cards

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/tile-cards`, cut from `origin/main` at `9518478`.

## Amendments

- **2026-10-11, blank cells:** a blank cell now draws nothing on the card face. The scope above kept today's pale dot, but on the cream card that dot reads as a hole. The implementation lead made this call in the execution brief; it is flagged to the maintainer in the PR.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** Every board tile and the drawn tile are separate cards: shadow, edge, and cream face layers of one rounded-square sprite, inset so neighbours show a gap. Colour cells show the maintainer's splat tinted to their colour at a stable per-slot angle; a wild cell is one splat split into four colours through quarter masks. `TilePainter` is the one routine for the board and the turn panel. Art sources live in `art/`, and `art/export.sh` rasterizes them with `rsvg-convert` into white PNG sprites under `Assets/Art/Resources/`.
- **Verification:** On `3003622`, `dotnet test` passed Rules 234 and App 66, and `unity test unity/MissionSplat --mode PlayMode` passed 12 of 12 on fresh Release DLLs; the stack play-mode test now asserts a highlight covers its whole tile block. `board`, `highlights`, and `after-bounce` were retaken on `85780ba`. A fresh-context design review found two issues and several suggestions; both issues and most suggestions are applied (see the receipt).
- **Deviations:** The blank-cell amendment above. The captures come from a new seeded game, because the earlier capture states were not recoverable. They were taken before the review refactor, which switched the card from a 9-sliced to a plain sprite with the same proportions; no look change is expected.
- **Unresolved gates or risks:** Highlight tints over cards still muddy the colours (#44). Splats are small on the board because the board scales to its cell count. A `SplatAngle` unit test was skipped: the test assembly cannot reach internal Player types.
- **Refactor and handoff receipt:** In the PR body and on issue #46.
- **Final PR:** see the PR linked from issue #46.
