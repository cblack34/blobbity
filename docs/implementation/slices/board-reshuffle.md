# Slice plan — Board reshuffle

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session on 2026-10-10. The maintainer ruled, from the physical game, that when the draw deck runs out every tile on the board — buried ones included — is gathered and shuffled into a new draw deck, the top tile is placed at the origin as a new start tile, and play continues with claims, hands, and turn order unchanged. The maintainer asked for this as its own PR, separate from the census deck and the distinct-cells claim rule that follow.
- **Final acceptance advanced:** Closes the "match-tile deck exhausted on a draw" item in [`docs/rules.md`](../../rules.md)'s open list; adds a Rules check and a Session check to [`docs/acceptance.md`](../../acceptance.md). The device games can now run to a win on any deck of at least two tiles instead of stopping at an open ruling.

## Outcome

A placement that empties the draw deck leaves the match awaiting a reshuffle: no tile is pending and no move is legal. The match accepts one new action, `ReshuffleBoard(order)`, only in that state and only for an exact permutation of the board's tiles; it rebuilds the board from the first tile at the origin, makes the rest the draw deck, and emits an event, with seats, hands, claims, and the current seat untouched. Rules never shuffles — the App table supplies the order, from its start seed when the table was shuffled and in board order otherwise, and submits the action itself with no human tap. The GUI shows a notice until the next human action. The pack records the rule.

## Why this slice is next

The census deck that follows holds 30 tiles, so a four-seat game will run the draw deck dry before anyone claims four times. Today that stops the table at an open ruling; the maintainer has now given the ruling. Landing it before the deck means the first device validation plays the real game end to end. Keeping it separate from the deck and from the claim rule gives each its own review.

## Scope

### In scope

- `Game.AwaitsReshuffle`, `Game.TilesToReshuffle`, the `ReshuffleBoard` action with permutation validation, `AwaitingReshuffle`/`NotAwaitingReshuffle`/`InvalidReshuffleOrder` rejections, the `BoardReshuffled` event, a `Grid` read of every tile including buried layers; the exhausted-deck exception removed.
- `SeatView.AwaitsReshuffle` and `TilesToReshuffle`; `Table` holding a seeded `Random` and submitting the reshuffle; `TableSnapshot.Notice`.
- A one-line notice in the Unity view.
- Rules, App, and play-mode tests, including buried tiles and seeded determinism.
- `docs/rules.md`, `docs/census.md`, `docs/acceptance.md`, `docs/architecture.md`, `docs/build-brief.md` recording the rule.

### Out of scope

- The census deck (next slice) and the distinct-cells claim rule (the slice after).
- Blobbity-deck exhaustion; the maintainer chose to resolve it by deck size in the census slice.
- Any choice by a human of the reshuffle order; the physical shuffle is random, and the table's seed stands in for it.
- The GUI redesign, `RemoteSession`, the remaining open rulings.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Rules in `rules.md` are the behavioral contract; open edges return to the user | The user's ruling is encoded and the open item closes. |
| The library does not shuffle; decks arrive ordered | The order arrives in the action; the table draws it from its seed at the edge. |
| Claim is placement-owned; a start tile is not a placement | The new start tile claims nothing, as at setup. |
| A seeded table replays | The same seed reshuffles into the same order. |
| Device games run to a win | No open ruling stops a long game on the match deck. |

## Gates and dependencies

### Hard gates

- The maintainer's ruling above, including buried tiles. Given.

### Sequencing recommendations

- Rules first, then App, docs, and the Unity notice on one branch; then the census deck slice.

## Architecture and contracts

- **Affected seams:** Rules' action set grows by one and its event set by one; `SeatView` and `TableSnapshot` gain fields. `ISession`'s methods are unchanged.
- **Public contracts:** `GameAction` (closed set +1), `GameEvent` (+1), `RejectionReason` (+3), `Game` (+2 reads), `SeatView` (+2), `TableSnapshot` (+1). Intentional, approved here.
- **Data and migration considerations:** None.

## High-level approach

Treat exhaustion as a state the match reports, not an exception it throws. The only action that state accepts is the reshuffle, whose argument is the order the physical shuffle would have produced; validation is a permutation check against every tile the grid holds, and the effect reuses the setup path that places a start tile at the origin. The table recognises the state after any accepted action and resolves it before anything else, drawing the order from its seed so a replay is exact. The notice is snapshot state so the GUI only draws it.

## Verification

- `dotnet test rules/Blobbity.Rules.sln --configuration Release`
- `dotnet test app/Blobbity.App.sln --configuration Release`
- `unity test unity/Blobbity --mode PlayMode` on freshly copied Release DLLs, with a clean-import run.
- Repository definition-of-done commands remain mandatory.

## Risks and stop conditions

- Stop if a fixture needs a ruling the maintainer did not give (for example, what happens if the board itself is empty when the deck runs out — a bounce emptied it and the drawn tile was the last; the order is then empty and there is nothing to place: report rather than invent).
- Stop if `ISession` needs a new method.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:board-reshuffle` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Aboard-reshuffle

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/board-reshuffle`, cut from `origin/main` at `7c25796`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** A placement that empties the draw deck leaves the match awaiting a reshuffle; only `ReshuffleBoard(order)` is accepted, and only for an exact permutation of every board tile including buried ones. It rebuilds the board from the first tile at the origin, makes the rest the draw deck, and keeps seats, hands, claims, and the current seat. The App table supplies the order from a seeded `Random` (or the match's order when unshuffled) and submits it with no human tap; a notice shows until the next human action. The exhausted-deck open ruling is closed in the pack.
- **Verification:** On `e224b3b` (after the PR review round, which added a test pinning full power uses on the tile pending after a reshuffle and aligned two doc sentences), `dotnet test rules/Blobbity.Rules.sln --configuration Release` passed 214, `dotnet test app/Blobbity.App.sln --configuration Release` passed 60, and `unity test unity/Blobbity --mode PlayMode` passed 12 of 12, run by the implementation lead independently of the execution agents' receipts (Unity and the clean import on `08d996a`; the review round changed only a Rules test and docs); a clean import in a detached worktree passed the same 12 with no tracked file altered. A fresh-context design review of all eleven production files found no blocking or issue-level finding; five of eight suggestions were applied.
- **Deviations:** A one-tile match deck at setup starts the game awaiting a reshuffle and the table stops with a message. The notice lasts until the next human action rather than the next action, so an AI turn cannot hide it.
- **Unresolved gates or risks:** Blobbity-deck exhaustion is resolved by deck size in the census slice; the distinct-cells claim rule follows; the GUI redesign is deferred.
- **Refactor and handoff receipt:** In the PR body, https://github.com/cblack34/mission-splat/pull/36, and on issues #34 and #35.
- **Final PR:** https://github.com/cblack34/mission-splat/pull/36
