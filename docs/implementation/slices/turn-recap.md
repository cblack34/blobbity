# Slice plan — Turn recap

## Strategic source

- **Active build pack:** The POC pack passed final acceptance in #43. This is UX work under #44.
- **Human approval:** Approved in session on 2026-10-13.
  - The maintainer asked that every human sees what happened since they last held the device: live for their own actions, and replayed at the start of their turn in pass-and-play and against AI.
  - The maintainer approved an App change for this: an event log and per-human seen tracking in the table driver.
  - The pace is about half a second per placement or power use and one second per claim.
  - **A tap skips only the current item**, so a recap of a placement and a claim takes two taps.
  - The recap covers everything since that human's last turn.
- **Final acceptance advanced:** None directly. The concealment criterion in [`docs/acceptance.md`](../../acceptance.md) must stay green; the recap shows public information only.

## Outcome

Each human sees every committed public event they have not yet seen, in order: placements, rotates, bounces, claims, the win, and reshuffles.
- **Own actions play live.** The acting human's own events play immediately, before any hand-off cover.
- **Others' actions play at the start of the turn.** In pass-and-play, events from other seats play after the incoming human confirms the hand-off. Against AI, the AI's moves play before the human acts.
- **Each item animates in turn:**
  - a placement drops the tile into its position with a short pulse;
  - a rotate turns the tile;
  - a bounce lifts the tile off;
  - a claim pops the mission card up large, then flies it into the claiming seat's score pip.
- **Input waits.** Nothing can be done while an item plays, and a tap skips only the current item.
- **A trace stays.** A one-line summary of what the recap showed stays visible afterwards.
- **Nothing hidden is revealed.** No item reveals hidden information: a reshuffle appears without the new deck order, and only claimed missions, which are public, are shown.

## Why this slice is next

In games with three or four seats, or pass-and-play, a returning player cannot tell what changed. The claim pop-up and the placement animation share that timing problem, so one mechanism carries both. Confirming actions (#58), powers by tapping (#59), and the power splash (#60) build on the same timing.

## Scope

### In scope

- **PR 1, App:**
  - an ordered log of accepted events in the table driver;
  - a per-human-seat cursor of what that seat has seen;
  - a public recap item type that the snapshot exposes for the human holding the device;
  - an acknowledge call that advances that seat's cursor;
  - `dotnet test` coverage.
- **PR 2, Unity:**
  - a recap player that animates each item, gates input, and skips per item;
  - the trailing summary line;
  - PlayMode tests and captures.

### Out of scope

- Confirm or cancel for actions (#58), powers by tapping (#59), the power splash (#60).
- Sound, pinch-zoom (#52), the configured win count (#51), critter colours.
- Any change to the rules.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Rules do not reference Unity (non-negotiable 1) | The log and recap live in App; Unity only plays them. |
| Pass-and-play on one in-process session (non-negotiable 3) | Per-human cursors on the one table; no network. |
| Secret missions hidden between human seats | Recap items are a public projection, and the hand-off cover rules are unchanged. |

## Gates and dependencies

### Hard gates

- PR 1 merges before PR 2 starts. PR 2 consumes the recap contract.

### Sequencing recommendations

- PR 1 first: the log, cursors, items, acknowledge, and tests. Then PR 2: the player, the animations, and captures.

## Architecture and contracts

- **Affected seams:**
  - App: `Table` and `TableSnapshot`, plus new recap item types.
  - Unity: the table views and a new recap player.
- **Public contracts:** Additive.
  - `TableSnapshot` gains the recap items for the human holding the device.
  - `Table` gains an acknowledge call.
  - The recap item types are a closed set (placed, rotated, bounced, claimed, won, reshuffled). Each carries what a screen needs to draw it: the placed tile's cells and orientation, the position, the claimed mission, and the seat. They never carry deck order.
- **Viewer rule:** the recap is for the human holding the device.
  - That human's own events stay pending until acknowledged, even after the turn passes, so the outgoing player sees their claims before the cover.
  - The incoming human's recap appears once they confirm the hand-off.
  - With no human seat, there is no recap.
- **Data and migration considerations:** none.

## High-level approach

Record every accepted session result's events in order. Give each human seat a cursor into the log. The snapshot projects the events after the holder's cursor into public recap items. Unity plays the items and acknowledges when the last item is done or skipped, which advances the holder's cursor to the end.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`, including recap tests:
  - a human against an AI;
  - two humans with hand-off;
  - three seats where one human's recap spans two other turns;
  - a claim and a win;
  - a reshuffle with no order exposed;
  - acknowledge advancing only the holder's cursor.
- PR 2: `unity test unity/MissionSplat --mode PlayMode`, captures of a placement and a claim mid-animation, and the maintainer's look on the phone.

## Risks and stop conditions

- Stop if the viewer rule conflicts with the existing hand-off rule (last human holder).
- Stop if a recap item would need hidden information to draw.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:turn-recap` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Aturn-recap

## Delivery shape

- **Topology:** Direct PRs to `main`, two in sequence.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **PR 1:** `feat/recap-feed`, cut from `origin/main` at `5623e8c`.
- **PR 2:** `feat/recap-playback`, cut from `main` after PR 1 merges.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** Each human sees, in order, every public event since they last held the device.
  - **The feed (PR 1, #63):** `Table` logs accepted events, tracks a per-human seen cursor, and exposes `TableSnapshot.Recap` and `RecapSeat` for the holder. `AcknowledgeRecap()` marks them seen. Items are a closed public set with the cells needed to draw them, and they never carry the deck order.
  - **The playback (PR 2):** Unity plays the items over the table:
    - a placement drops with a pulse;
    - a rotate turns the tile, and a bounce lifts it off;
    - a claim pops the card up, then flies it to the seat's pip;
    - a reshuffle or a win shows a banner.

    The board scrolls to a tile that is out of view. Pacing is 0.5 s per board item and 1 s per claim or win. A tap finishes only the current item. Input is blocked twice over, by the overlay and in `TableSession`. While a recap plays, the heading names the item; the next turn's heading, the hand-off cover and the winner tray wait for the acknowledge.
  - **Timing:** the outgoing player's own items play before the cover, and the incoming player's play after Confirm. A one-line summary stays until the next action.
- **Verification:**
  - **PR 1:** on `823063b`, Rules 234 and App 85.
  - **PR 2:** on `8f2e3cd`, Rules 234, App 85, and `unity test unity/MissionSplat --mode PlayMode` 41 of 41 (9 recap tests). They cover:
    - human against AI;
    - two humans, where the outgoing recap plays before the cover, no incoming missions are drawn, and a Confirm during the recap is ignored;
    - one tap skips one item;
    - the summary line;
    - a failing item still ends and acknowledges;
    - the board scrolling to an off-screen tile;
    - the skip layer taking every tap.
  - **Captures:** `recap-placement.png` and `recap-claim.png`.
- **Deviations:**
  - The win plays as its own one-second banner after the claim that wins, so a winning claim takes two taps.
  - The heading names the playing item, an addition from review, so the outgoing player is never prompted to hand off mid-recap.
- **Unresolved gates or risks:**
  - The seat bar's winner highlight appears before the win banner plays.
  - If automated seats win before any human has confirmed, there is no recap; the win still shows.
  - A layout change mid-recap leaves that item's overlay at the old position.
  - Confirm or cancel (#58), powers by tapping (#59), and the power splash (#60) build on this timing.
- **Refactor and handoff receipt:** In each PR body and on issues #61 and #62.
- **Final PR:** see the PR linked from issue #62.
