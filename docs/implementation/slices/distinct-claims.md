# Slice plan — Distinct claims

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session on 2026-10-10. The maintainer ruled that missions claimed on one placement may not use the same connections: a line and an L may share dots and both claim, but two missions completed by the same four dots give one claim. The maintainer accepted the defaults for the cases that ruling leaves open. The comparison is by cells, not colour, so a wildcard row completing red and blue lines on identical cells is one claim. The earlier card in the hand wins a contested set. When missions have alternative sets, the assignment claims as many missions as possible, with hand order breaking ties. The maintainer asked for this as its own PR.
- **Final acceptance advanced:** Adds a Rules check to [`docs/acceptance.md`](../../acceptance.md). It is a prerequisite for the census deck with doubled missions, where identical cards in one hand become possible.

## Outcome

A placement can still claim several missions, but each claimed mission is completed by its own set of four cells. Overlapping sets are fine; identical sets are not. The pattern search reports every completing set that touches the placed cells. The rules engine assigns missions to distinct sets, claiming the most missions possible and preferring earlier cards on ties. Everything else about resolution is unchanged.

## Why this slice is next

The next slice doubles the mission deck so it cannot run out, which lets one hand hold two identical cards. Without this rule, one placement would claim both from the same four dots. The rule also covers a case the current deck can already reach: wildcards completing two colours on the same cells.

## Scope

### In scope

- `PatternSearch` returning completing cell sets.
- `Ruleset.CompletedMissions` assigning distinct sets.
- Rules tests for the five cases in the issue.
- `docs/rules.md`, `docs/acceptance.md`, `docs/build-brief.md`, and `docs/architecture.md`.

### Out of scope

- The census deck and doubled missions (next slice).
- App, Unity, the GUI redesign, the remaining open rulings.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Claim is placement-owned | Only sets touching the placed tile's cells are considered, as before. |
| One placement claims every mission it completed | Narrowed by the maintainer's ruling to missions with distinct cell sets; still as many as the board supports. |
| Rules in `rules.md` are the contract | The Claim section records the rule and both defaults. |

## Gates and dependencies

### Hard gates

- The maintainer's ruling and defaults above. Given.

### Sequencing recommendations

- This slice before the census deck.

## Architecture and contracts

- **Affected seams:** internal to the rules engine (`PatternSearch`, `Ruleset`).
- **Public contracts:** unchanged. `MissionClaimed` events and `Game` members keep their shape; only which missions claim can change.
- **Data and migration considerations:** None.

## High-level approach

Make the pattern search enumerate completing cell sets instead of stopping at the first. Treat claiming as a small matching problem: missions on one side, distinct cell sets on the other. Search for the largest assignment and prefer earlier cards when sizes tie. Hands are a few cards and boards are small, so an exact search is cheap and stays readable.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- Repository definition-of-done commands remain mandatory.

## Risks and stop conditions

- Stop if an existing test's expected claims change for a reason other than this rule.
- Stop if App or Unity needs a change.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:distinct-claims` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Adistinct-claims

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/distinct-claims`, cut from `origin/main` at `24318a0`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** One placement still claims several missions, but each claimed mission now needs its own four cells. Overlapping sets are fine; identical sets, compared by cells rather than colour, give one claim. `PatternSearch.CompletingSets` lists every completing set that touches the placed cells. `DistinctAssignment.Choose` takes missions in hand order and keeps each one only if every kept mission can still get a different set. The completing sets form a transversal matroid, so this greedy pass claims the most missions possible and prefers earlier cards. Nothing else in resolution changed, and no public API moved.
- **Verification:** On `f6950ab`, `dotnet test rules/MissionSplat.Rules.sln --configuration Release` passed 225 (214 plus 6 claim cases and 5 assignment cases), and `dotnet test app/MissionSplat.App.sln --configuration Release` passed 60. No existing test changed. A fresh-context design review of the three production files found no blocking issue. Four of its suggestions were applied: the extracted assignment helper with its own tests, a non-mutating `CellSet.Of`, naming the matroid, and no tentative add-then-remove.
- **Deviations:** None from the approved plan. The assignment search lives in its own internal type rather than inside `Ruleset`, so it can be tested on its own.
- **Unresolved gates or risks:** The census deck with 24 missions is next; identical cards in one hand are what this rule protects. GUI redesign deferred.
- **Refactor and handoff receipt:** In the PR body and on issue #37.
- **Final PR:** see the PR linked from issue #37.
