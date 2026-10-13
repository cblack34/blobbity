# Slice plan — The look

## Strategic source

- **Active build pack:** The POC pack passed final acceptance in #43. This slice is GUI redesign work tracked in #44, built toward the design reference in [`docs/design/`](../../design/README.md) on the layout from #53.
- **Human approval:** Approved in session on 2026-10-13. The maintainer chose Baloo 2 as the typeface after comparing it with Fredoka and Nunito, noting it is easy to change later.
- **Final acceptance advanced:** None directly. The presentation criteria in [`docs/acceptance.md`](../../acceptance.md) must stay green.

## Outcome

The table looks like the splat party reference:
- toy-like tiles and cards, with white faces, a thick dark outline, a hard offset shadow, and chunkier corners; the drawn tile and mission cards share the style, and secret missions sit slightly tilted;
- big faint splats spilling off the screen edges, and paint drips from the top;
- legal placements as dashed rounded squares with a faint "+";
- stack and power-target highlights as a coloured outline around the tile instead of a tint over it;
- power marks as coloured badges (rotate yellow, stack blue, bounce pink) on tiles and chips;
- the wavy white tray edge;
- Baloo 2 throughout;
- seat bar spacing that suits two and three seats;
- a setup screen restyled to match.

## Why this slice is next

#53 settled where everything sits and how it behaves. What still reads as unfinished is the look: cream cards with soft shadows, flat tints that muddy the splats (#44), plain chips, and the system font.

## Scope

### In scope

- Card styling shared by tiles and mission cards, highlights as outlines, legal-spot styling, coloured power badges, the background art, the tray edge, the font, seat bar spacing, and the setup restyle.
- New art through the existing pipeline (SVG in `art/`, exported sprites), and Baloo 2 as fixed-weight files cut from the variable font with its OFL licence.
- Every capture retaken in portrait.

### Out of scope

- The claim pop-up, pinch-zoom (#52), the configured win count (#51), a new way to show powers, choosing critter colours, the hand-off screen redesign, playing again after a win (#54).
- Any Rules or App change, and any change to what is shown or when.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Presentation criteria in `acceptance.md` | Same content, restyled; captures retaken. |
| No physical-game art ships (non-negotiable 4) | Original art from the project's splat; an OFL font. |
| #44: tints over tiles muddy the colours | Highlights become outlines. |

## Gates and dependencies

### Hard gates

- None.

### Sequencing recommendations

- Font and card style first, since everything uses them; then highlights and badges; then background, tray and seat bar; then setup; then captures.

## Architecture and contracts

- **Affected seams:** Unity player rendering only (painters, `Ui`, `SplatPalette`, the views).
- **Public contracts:** unchanged.
- **Data and migration considerations:** none.

## High-level approach

Keep one card painter so tiles and mission cards change together. Draw outlines and hard shadows with sprite layers rather than shaders. Put the background art behind everything as non-interactive images. Load fonts through one `Ui` font accessor, so a later font change is one line.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- `unity test unity/MissionSplat --mode PlayMode` on freshly copied Release DLLs.
- Every capture retaken in portrait, and the maintainer's look on the phone.

## Risks and stop conditions

- Stop if a style needs a shader or a new package.
- Stop if background art competes with the board for legibility; keep it faint, and keep it clear of text and tiles.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:look` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Alook

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/look`, cut from `origin/main` at `9d0cddc`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.
