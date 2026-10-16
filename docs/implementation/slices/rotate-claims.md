# Slice plan — Rotate claims

## Strategic source

- **Active build pack:** The POC pack. This slice changes one of its rules, by the maintainer's ruling on #72.
- **Human approval:** Ruled and approved in session on 2026-10-13; the ruling is recorded on #72.
  - A rotate that completes a blobbity claims it. A bounce never claims.
  - The rotated tile goes through the same claim check as a placed tile, scoped to its four cells.
  - It claims at the moment of the rotation, and only the acting seat claims.
  - A rotate claim does not end the turn. Placing a tile always ends it.
  - A replacement drawn after a rotate claim can be claimed by the turn's placement.
  - A rotate claim that reaches the win count wins at once.
  - Distinct cells apply per resolution.
  - The maintainer approved changing non-negotiable 2, `rules.md`, and `acceptance.md` to match.
- **Final acceptance advanced:** The rotate-or-bounce criterion in [`docs/acceptance.md`](../../acceptance.md) splits in two. A rotate that completes a blobbity claims it. A bounce that exposes a pattern does not.

## Outcome

The principle: **cells the acting seat writes this turn can complete its blobbities, and are claimed when written.**
- A rotate writes the rotated tile's four cells in a new orientation, so the same claim pass a placement uses runs on those cells.
- Each pass is pure and stateless: `(board after the write, cells just written, acting seat's hand) → claims`.
- The match commits each pass's claims and replacement draws before the next action, and a claim that reaches the win count ends the game at once.
- A bounce reveals cells nobody wrote this turn, so it never claims.
- The recap already plays claims, and plays rotate claims too.

## Why this slice is next

The maintainer found a rotate that formed a blobbity's shape and did not claim it. Powers by tapping (#59) adds a rotate preview, which should show the corrected rule.

## Scope

### In scope

- `AGENTS.md` non-negotiable 2, `docs/rules.md` (Claim, Powers), `docs/acceptance.md`, and any active doc that states the old rule (`build-brief.md`, `architecture.md`).
- Rules: run the claim pass after an accepted rotate, and emit claim and win events from it.
- Rules tests for each ruled case; App and Unity tests only where they assert the old rule.

### Out of scope

- Bounce claims, a stricter whole-turn distinct-cells rule, UI changes beyond what the recap already shows, and #59 and #60.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Non-negotiable 2 (as amended) | A claim scores only from cells the current seat wrote, by placing or rotating. |
| Rules do not reference Unity (non-negotiable 1) | The change is in Rules, and `dotnet test` covers it. |
| `acceptance.md` rotate and bounce criteria | Split and checked as ruled. |

## Gates and dependencies

### Hard gates

- The maintainer's ruling on #72. Given.

### Sequencing recommendations

- Contract docs first, then the Rules change and tests, then any App or Unity test updates.

## Architecture and contracts

- **Affected seams:** the Rules `Ruleset` claim pass and `Game` (rotate application, claim and win bookkeeping).
- **Public contracts:** the event stream gains `BlobbityClaimed` (and `GameWon`) after `TileRotated` when a rotate completes a blobbity. No API shape change.
- **Data and migration considerations:** none.

## High-level approach

Reuse the placement's claim pass with the rotated tile's cells as the written cells. Apply the same claim resolution after an accepted rotate: claim, draw replacements, check for the win. Keep the pass stateless, and let the match sequence the passes.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`, with tests for:
  - a rotate claim;
  - a replacement claimed later by the placement;
  - a win on a rotate that ends the game before the placement;
  - a bounce that exposes a pattern and does not claim;
  - distinct cells within a rotate's pass;
  - a rotate claim that does not end the turn.
- `dotnet test app/MissionSplat.App.sln --configuration Release`, and a recap of a rotate claim.
- `unity test unity/MissionSplat --mode PlayMode`, and the maintainer's look on the phone.

## Risks and stop conditions

- Stop if a ruled case is ambiguous in code (for example, a rotate claim on a tile that is also part of the drawn tile's future placement).

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:rotate-claims` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Arotate-claims

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/rotate-claims`, cut from `origin/main` at `96ef207`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** A rotate now claims the blobbities its four written cells complete, at the moment of the rotation, through the same claim pass a placement uses.
  - **Claim pass:** `Game.ResolveClaims(grid, writtenCells)` returns a result without changing anything. Placement and rotate both call it, and `Grid.CellsOfTile` supplies a rotated tile's cells.
  - **Turn:** a rotate claim draws replacements and keeps the turn and the drawn tile. Reaching the win count ends the game at once, on the acting seat.
  - **Bounce:** it never claims.
  - **Unchanged colours:** by a later ruling, any rotate of 1–3 quarter turns writes all four cells, even if their colours come out unchanged.
  - **Contract wording:** non-negotiable 2, `rules.md` (intro, Turn, Claim, Powers), `acceptance.md` (the rotate and bounce criteria are split), `build-brief.md`, and `architecture.md` now state the rule as cells the acting seat writes, by placing or rotating.
- **Verification:** On `eb01636`, `dotnet test` passed Rules 241 and App 86, and `unity test unity/MissionSplat --mode PlayMode` passed 57 of 57. Unity ran in a scratch copy because the maintainer's editor was open. The Rules tests cover:
  - a rotate claim at once, with the turn and the drawn tile kept;
  - a replacement claimed later by the placement;
  - a rotate win, which leaves the turn on the acting seat and rejects any later action;
  - a bounce that exposes a pattern without claiming;
  - distinct cells within one rotate, and per resolution across two rotates;
  - another seat's blobbity not claimed;
  - a rotate whose claims the blobbity deck cannot replace, which leaves the match and the charge unchanged;
  - a single-colour tile whose rotate leaves its cells unchanged but still claims.

  An App test shows the recap as rotated, then claimed.
- **Deviations:**
  - The unchanged-colour case was ruled mid-slice, after review.
  - The cross-resolution test uses two rotates, because a tile cannot show both rotate and stack (an open ruling).
- **Unresolved gates or risks:** none. A stricter whole-turn distinct-cells rule would add one input to the pass.
- **Refactor and handoff receipt:** In the PR body and on issue #72.
- **Final PR:** see the PR linked from issue #72.
