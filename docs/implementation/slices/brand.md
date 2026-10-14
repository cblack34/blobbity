# Slice plan — Brand

## Strategic source

- **Active build pack:** The POC pack passed final acceptance in #43. This is brand and presentation work outside that acceptance, following the look slice under #44.
- **Human approval:** Approved in session on 2026-10-14. The maintainer's decisions:
  - "Mission Splat" is a working title. Trademark concerns mean it must change.
  - The game is **Blobbity**, and so are the secret target cards ("a blobbity"). The game is named for what you collect.
  - The player critters are **Blobkins**.
  - Colour-blind support: every colour keeps today's splat, with a symbol upright in the middle of the splat body. The symbols are red heart, blue triangle, green square, purple star, and a wild four-point sparkle. A–D are outlines with no fill, drawn in a deeper shade of the splat's own colour (OKLCH lightness −0.16, chroma kept, so the shade reads as colour, not black). The wild's sparkle is filled, and each quarter is deepened from its own colour. These were chosen on 2026-10-14 from rendered sheets, after per-colour splat silhouettes were rejected as losing the splat's feel. A colour-blind mode in options stays the fallback.
  - The app icon is a Blobkin.
  - The guide is the source of truth. A human-readable board and the player's code are checked against it.
  - The type system uses two or three fonts: Baloo 2 for display, plus Atkinson Hyperlegible Next for text, chosen from rendered samples on 2026-10-14.
  - Package scope: brand guide, board, style tile, mascot sheet, logo package, and app icons now; the rest later.
- **Final acceptance advanced:** None directly. The presentation criteria in [`docs/acceptance.md`](../../acceptance.md) must stay green. Non-negotiable 4 (no copied trade dress) becomes concrete in the guide.

## Outcome

The game has an identity a contracted designer can pick up without asking:
- a name (Blobbity), a card word (blobbity), and a mascot name (Blobkins), screened for obvious conflicts;
- a brand guide covering:
  - who it is for and what it promises;
  - principles;
  - colour roles;
  - type hierarchy;
  - shape language;
  - mascot rules;
  - iconography;
  - voice;
  - originality rules;
  - the art pipeline;
- a visual board, rendered from the repo's own art and fonts, that shows the guide applied;
- colours a colour-blind player can tell apart, with a symbol for each colour;
- a mascot sheet, a wordmark and logo package, and app icons for iOS and Android.

## Why this slice is next

The table looks right, but the game has no name it can keep, no icon, no logo, and no written rules for its look. Any new art, or a hired designer, would otherwise guess. A colour-blind simulation of the current palette showed blue/purple and red/green nearly merging (CIE76 ΔE about 13–15 for deuteranopia). Colour is the scoring mechanic, so this is a playability gap, not polish.

## Scope

### In scope

- Renaming the secret cards to blobbity in the player and the docs (#68).
- Adopting Blobbity and Blobkins in player-facing text, the product name, and the pack's product description. This includes a deeper screen covering exact spellings, sound-alikes, and direct store search.
- The brand guide and its board, the fonts (closes #67), and the guide-to-board-to-code drift check.
- Symbols in every colour cell and on blobbity cards, with retaken captures. Hues are re-tuned only if a simulation shows the symbols are not enough.
- The Blobkin mascot sheet, the wordmark and logo package, app icons wired into the Unity player, and a real `companyName`.

### Out of scope

- Renaming code identifiers, namespaces, solutions, or the repo (`MissionSplat.*` stays).
- Legal trademark clearance; that is a human step.
- Store listing assets (feature graphic, screenshots, video), merch and packaging mockups, a brand portal, a design-token pipeline, and a UI kit.
- New power icons (#69), new shapes (#70), and new powers (#71).
- Any Rules or App behaviour change.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Non-negotiable 4: no copied trade dress | Original name, mascot, and art. The guide states the originality rules. |
| Intended users include a child who can follow a secret card | Colour plus silhouette makes each card readable without perfect colour vision. |
| Presentation criteria in `acceptance.md` | Same content, renamed and restyled; captures retaken. |

## Gates and dependencies

### Hard gates

- The wordmark and logo wait on the name screen. If the screen finds a conflict, stop and return to the maintainer.

### Sequencing recommendations

1. The card word, then the name. These are text-only and unblock the guide's voice section.
2. The guide and board.
3. The colour pass, which records its result in the guide.
4. The mascot, the logo, and the icons.

## Architecture and contracts

- **Affected seams:** Unity player presentation (`SplatPalette`, `Ui`, the painters, `art/`, `ProjectSettings`) and the docs.
- **Public contracts:** unchanged. Colour ids (`red`, `blue`, `green`, `purple`) stay; only rendered values and silhouettes change.
- **Data and migration considerations:** none.

## High-level approach

- **The guide:** Markdown at `docs/brand/guide.md`, for designers and agents, and the source of truth for every colour, font, and measurement.
- **The board:** an HTML page under `docs/brand/`, built from `art/*.svg` and the shipped fonts, and printed to PDF with headless Chrome.
- **Drift:** AGENTS.md gains one rule: when the guide changes, the board and `SplatPalette.cs` must still match it. A small check fails if a colour in the guide is missing from either.
- **New art:** it goes through `art/*.svg` and `export.sh`, as today.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- `unity test unity/MissionSplat --mode PlayMode` on freshly copied Release DLLs.
- The drift check passes.
- Captures are retaken after the rename and the colour pass.
- A colour-blind simulation of the final palette and silhouettes.
- The maintainer's look on the phone.

## Risks and stop conditions

- Stop if the name screen finds a confusingly similar mark in software, games, or toys.
- Stop if the symbols are unreadable at board size on the device. A colour-blind mode is then the fallback, and that is a scope decision.
- Stop if a style needs a shader or a new package.

## Execution issues

GitHub issues are the WIP tracker and the source of task-level detail. Every issue for this slice carries the `slice:brand` label: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Abrand

## Delivery shape

- **Topology:** Direct PRs to `main`, one per issue.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.
