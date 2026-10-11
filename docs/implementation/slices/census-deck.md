# Slice plan — Census deck

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/census.md`](../../census.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session on 2026-10-10. The maintainer verified `census.md` when it was written, which clears the research gate on uncorrected rows. The maintainer chose to double the mission cards so the mission deck cannot run out, and asked for this as its own PR after the reshuffle and distinct-claims slices.
- **Final acceptance advanced:** Clears the census gate in [`docs/acceptance.md`](../../acceptance.md) and adds a check that the shipped deck matches the census. It also removes the last two deck-related items from [`docs/rules.md`](../../rules.md)'s open list.

## Outcome

The player ships the census: the 30 match tiles exactly as `census.md` lists them, and 24 mission cards, two of each colour and shape. The stand-in deck is gone. `census.md` records the doubled missions as a deliberate digital adjustment, with the arithmetic that rules out mission-deck exhaustion.

## Why this slice is next

It is the last decision gate before final acceptance. The two rulings this deck relies on are already on `main`: the board reshuffle, which a 30-tile deck will trigger, and the distinct-cells claim rule, which identical cards in one hand need. After this slice, final acceptance only verifies.

## Scope

### In scope

- `TableDeck`'s content becomes the census (same API, same seeded shuffle).
- A cell-by-cell census-match test, plus updates to App tests and one play-mode test that depended on the stand-in deck.
- `census.md`, `rules.md`, `acceptance.md`, `architecture.md`, and `build-brief.md`.

### Out of scope

- Retaking captures (final acceptance does that once).
- Any Rules or Unity script change.
- The GUI redesign.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| The base deck is `census.md` | The shipped deck is the census, pinned by a test. |
| A claim that the shipped deck matches the box uses the census definition | The test compares every cell against the census rows. |
| Open edges return to the user | Mission-deck exhaustion is unreachable with 24 missions; it leaves the open list with the reason recorded. |

## Gates and dependencies

### Hard gates

- The maintainer's verification of `census.md` and the doubling decision. Given.
- The board reshuffle and distinct-cells claim rule on `main`. Done (#36, #38).

### Sequencing recommendations

- This slice, then final acceptance.

## Architecture and contracts

- **Affected seams:** App deck content only.
- **Public contracts:** unchanged. `TableDeck.Ordinary` keeps its signature; `TableDeck.Name` changes to `"census"`.
- **Data and migration considerations:** None.

## High-level approach

Encode the census rows as data, translating its top-row-first cell order into the tile constructor's y-up order. Double the missions. Pin the result with a test whose expected rows are written independently from `census.md`, so a transcription slip in either place fails. Move any test that relied on the stand-in deck's specific tiles onto the census or a small named deck.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- `unity test unity/MissionSplat --mode PlayMode` on freshly copied Release DLLs.
- Repository definition-of-done commands remain mandatory.

## Risks and stop conditions

- Stop if a census row reads ambiguously; the census is the maintainer's data.
- Stop if a Rules change appears necessary.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:census-deck` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Acensus-deck

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/census-deck`, cut from `origin/main` at `037d2ae`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.
