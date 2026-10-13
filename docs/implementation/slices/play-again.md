# Slice plan — Play again

## Strategic source

- **Active build pack:** The POC pack passed final acceptance in #43. This is UX work under #44, tracked in #54.
- **Human approval:** Approved in session on 2026-10-13. The maintainer did not choose whether the first player repeats or rotates; the implementation lead's recommendation stands: Play again rotates the first player to the next seat. That is easy to change on request.
- **Final acceptance advanced:** None directly. The presentation criteria in [`docs/acceptance.md`](../../acceptance.md) must stay green.

## Outcome

When a seat wins, the turn tray shows the winner, a large **Play again** button, and a smaller **New setup** button.
- **Play again** starts a new game at once with the same seat count and the same human/AI choices. The first player moves on to the next seat, wrapping around. Every game a player starts is shuffled, so it is a fresh deal (see the amendment below).
- **New setup** returns to the setup screen, pre-filled with the finished game's choices.
- The board's scroll position resets for the new game.

## Why this slice is next

A finished game currently leaves the player with no way forward except restarting the app, and every game reaches that point.

## Scope

### In scope

- Game-end tray buttons and their wiring through the existing setup and start path.
- PlayMode tests and a game-end capture.

### Out of scope

- The claim pop-up, pinch-zoom (#52), the configured win count (#51), the power display redesign, critter colour choice.
- Any Rules or App change.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Pass-and-play is 2–4 seats on one in-process session (non-negotiable 3) | A new session per game, same seats, no network. |
| Presentation criteria in `acceptance.md` | Unchanged; game end gains two actions. |

## Gates and dependencies

### Hard gates

- None.

### Sequencing recommendations

- Buttons and wiring, then tests, then the capture.

## Architecture and contracts

- **Affected seams:** Unity player only (`TurnTray`, `TablePlayView`, `TableView`). The next start is a new `TableStart`, built from the finished one.
- **Public contracts:** unchanged.
- **Data and migration considerations:** none.

## High-level approach

Remember the `TableStart` the current game began from. On Play again, build the next one with the first seat advanced and start through the same path setup uses. On New setup, show setup with that `TableStart` as its draft.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- `unity test unity/MissionSplat --mode PlayMode` on freshly copied Release DLLs.
- A game-end capture, and the maintainer's look on the phone.

## Risks and stop conditions

- Stop if starting again needs an App or Rules change.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:play-again` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Aplay-again

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/play-again`, cut from `origin/main` at `ac1f5ec`.

## Amendments

- **2026-10-13, by the maintainer:** the setup screen loses its Shuffle toggle, and every game a player starts is shuffled. Play again is therefore always a fresh deal. The App's `TableStart` keeps its shuffle option, because tests and seeded captures rely on fixed deck orders.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** The game-end tray shows the winner, a large **Play again** button and a smaller **New setup** button.
  - **Play again** starts a new game at once with the same seats and AI choices. The first seat moves on to the next seat, wrapping around.
  - **New setup** opens setup pre-filled with the finished game's choices, including that game's first seat.
  - The setup screen no longer has a Shuffle toggle, so every game a player starts is shuffled.
  - The seat bar highlights the winner when the game is won, and no seat when a game ends without a winner.
  - A new game resets the board scroll and closes the claims panel.
  - `TableSession` remembers the `TableStart` the current game began from and starts the next one through the same path as setup. No Rules or App change.
- **Verification:** On `95659c5`, `dotnet test` passed Rules 234 and App 66, and `unity test unity/MissionSplat --mode PlayMode` passed 31 of 31.
  - New tests check that Play again starts a fresh game with the next first seat, empty claims and a closed claims panel.
  - They check that New setup pre-fills the finished game's choices and shows no Shuffle button.
  - They check that the winner is the highlighted seat.
  - `game-end.png` is new and `setup.png` was retaken.
- **Deviations:**
  - The shuffle amendment above.
  - The seat-bar winner highlight. It was found during the slice and fixed here, because it sits on the screen this slice adds.
- **Unresolved gates or risks:**
  - The "game over without a winner" highlight path has no test fixture.
  - Moving the next-game rule into `TableStart` would make it testable with `dotnet test`. That is an App change for a later slice.
- **Refactor and handoff receipt:** In the PR body and on issue #54.
- **Final PR:** see the PR linked from issue #54.
