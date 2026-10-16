# Slice plan — Final acceptance

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/census.md`](../../census.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session on 2026-10-10, after the census deck merged. The maintainer chose to complete acceptance on the current GUI and to treat the redesign as later work. The maintainer will play the device games on an iPhone.
- **Final acceptance advanced:** All of it. This is the completing PR that `AGENTS.md`'s definition of done requires.

## Outcome

Every criterion in [`docs/acceptance.md`](../../acceptance.md) is green by a named test or backed by a committed capture. Both definition-of-done commands and the Unity play-mode tests pass on the final head. The descriptive docs match the shipped code. The repository passes a trade-dress review. The maintainer's device games are recorded on the PR. When that record lands, the active pack's outcome is met.

## Why this slice is next

Every behaviour the pack defines is on `main`: the corrected turn, all three powers in the player, the board reshuffle, distinct-cell claims, and the census deck. An audit against the acceptance document found two automated checks missing a sub-clause. The captures still show the stand-in deck. What remains is verification and evidence.

## Scope

### In scope

- Fixtures for a blank or power cell blocking a square and an L.
- A test double proving the AI sees only its own hand and the public board.
- Captures retaken on the census deck, with superseded captures retired.
- Reconciling `build-brief.md`, `architecture.md`, the C4 caption, and `acceptance.md`'s run section with the code.
- A trade-dress review of every tracked file and image.
- The acceptance record in the PR body.
- The maintainer's two device games.

### Out of scope

- The GUI redesign; it is later work, outside this pack's acceptance.
- `RemoteSession`, a desktop build, store listings.
- Any production code change. If one is needed, it becomes its own slice.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Final acceptance is required on the completing PR | This is that PR. |
| Agent-run Unity CLI evidence is recorded | `unity test` results and fresh captures are in the PR. |
| Device evidence stays human | The maintainer's two games close the second issue. |
| No copied trade dress | A repository-wide review is recorded in the receipt. |

## Gates and dependencies

### Hard gates

- The census deck (PR #40) is on `main`. It is.
- The maintainer's device games, for the final checkbox.

### Sequencing recommendations

- Tests, captures, and docs first. Then the maintainer builds from the branch head and plays.

## Architecture and contracts

- **Affected seams:** none. Tests, captures, and docs only.
- **Public contracts:** unchanged.
- **Data and migration considerations:** none.

## High-level approach

Close the two automated gaps with focused fixtures. Retake the Presentation captures against the shipped deck using the headless capture path. Walk each descriptive doc against the code and fix only what is stale. Search the tree and view every image for trade dress. Assemble a line-by-line acceptance table for the PR body, then hand the branch head to the maintainer for the device games.

## Verification

- `dotnet test rules/Blobbity.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- `unity test unity/MissionSplat --mode PlayMode` on freshly copied Release DLLs, with a clean-import run.
- The maintainer's device games recorded on the PR.

## Risks and stop conditions

- Stop if a gap test fails. That is a production defect, and fixing it needs its own slice.
- Stop if the trade-dress review finds anything; the maintainer decides the fix.
- Stop if a device game turns up a defect; triage it with the maintainer before merging.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:final-acceptance` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Afinal-acceptance

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `chore/final-acceptance`, cut from `origin/main` at `b1f0a9c`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** Every criterion in [`docs/acceptance.md`](../../acceptance.md) is green by a named test or backed by a committed capture, recorded line by line in the PR body. New fixtures show a blank or a power cell blocking a square and an L. A test double shows the AI seat sees only its own blobbities and the public board. The captures are retaken on the census deck, including the reshuffle notice. The build brief, architecture, C4 caption, and acceptance run section now match the code. Unity player build output is ignored.
- **Verification:** On `8aa3baf`, `dotnet test` passed Rules 234 and App 66. `unity test unity/MissionSplat --mode PlayMode` passed 12 of 12 on freshly copied Release DLLs, with a clean-import run. The trade-dress review was an agent search of every tracked file and a view of every image; it was clean. The maintainer built the player from this branch, tested it on an iPhone, and accepted it as the device pass.
- **Deviations:** The plan asked for two named device games: a pass-and-play game with three or more seats, and a game with an AI seat. The maintainer instead gave a general pass on the iPhone. Seat mixes are covered by the App table tests and the Unity play-mode tests. No fresh-context design review ran, because no production code changed.
- **Unresolved gates or risks:** The maintainer dislikes the current look; the GUI redesign is #44, outside this pack's acceptance. Two rulings remain open in `rules.md`: a drawn tile showing two different powers, and a drawn tile with no legal placement on a non-empty board.
- **Refactor and handoff receipt:** In the PR body and on issue #41.
- **Final PR:** see the PR linked from issue #41.
