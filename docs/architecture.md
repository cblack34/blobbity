# Architecture

Descriptive. Code wins after it exists; update this document when a boundary changes.

## Containers

The POC is one process on the device. The later room is the same action and event seam, drawn dashed in [`diagrams/c4.svg`](diagrams/c4.svg).

```text
rules/                 netstandard2.1 rules library and dotnet test
app/                   ISession, IPlayer, LocalSession, AiPlayer, turn driver, deck content
unity/MissionSplat/    player: setup form, input adapter, table view, composition
packages/com.mission-splat.game/   local UPM package holding the built Rules and App DLLs
server/                absent in the POC; later ASP.NET project reference
```

Names are illustrative. The dependency rule is not: Unity consumes Rules and App only through the single local UPM package. It does not also reference the App project. App references Rules by project reference. The later server references Rules by project reference. Rules references neither.

## Components

Rules has two parts with a named seam.

- **Rules engine** (Rules) is stateless. Given the board, the drawn tile, the acting seat's hand, and the catalog, it answers: where the drawn tile may legally go at an orientation, as positions tagged beside or on-top, where on-top appears only when the drawn tile shows stack and an empty board yields only the origin; which board tiles a rotate or bounce may act on; what a use does to the board; and which of the hand's blobbities the cells just written (a placed tile's, or a rotated tile's) completed, each claimed blobbity taking its own four cells, so no two claims use the identical set (as many as possible, earlier cards first). It knows nothing of seat count, turn order, decks, or scores. `Grid`, `PatternSearch`, and `DistinctAssignment` are its internals. Each power's legality and effect lives in its own part of the engine; a ruleset enables a set of them.
- **Match** (Rules; the class is `Game`) is the game in progress: seats in turn order, hands, both decks, claim rows, the current seat, and the turn phase — which rotate or bounce cells on the drawn tile are still unused. It accepts one action from the current seat, asks the rules engine, applies the bookkeeping, and returns the next match with the events that say what happened. A rejected action returns the same match. Setup does not claim. A placement or a rotate can return more than one claimed event, then won when the configured count is met. There is no wildcard-chosen event: a wild cell stays wild, and each blobbity check counts it as that blobbity's color. The match never shuffles; decks arrive ordered. Whenever the draw deck is empty and the game is not won — after the placement that emptied it, or from setup when no tile follows the start tile — the match awaits a reshuffle: it accepts only `ReshuffleBoard` with the order from outside, an exact permutation of every board tile, buried ones included. Setup supplies the color catalog, the non-scoring symbols (those of them that are rotate, stack, or bounce are the powers in play), the active patterns, and the claims required to win.
- **ISession** (App) is the seam a GUI and the turn driver talk to: start, which seat is current, the view for a seat, submit an action for a seat, the legal-move queries forwarded from the match, a preview of an action (whether it is accepted and which blobbities it would claim, without changing the match), and the powers in play. **LocalSession** wraps one match in process. **RemoteSession** is not in the POC; when it exists it implements the same interface and stays outside Unity.
- **Turn driver** (App) runs the table: which seats are automated, running them after each accepted action until a human seat is current, the hand-off between humans on one device, supplying the reshuffle order from its seed (board order when it has none) as soon as the match awaits one, and the render model a GUI draws, including a notice that the deck was reshuffled, kept until the next human action, and a recap feed: an ordered log of every accepted event projected to public recap items (placed with its cells as placed, rotated, bounced, claimed with the blobbity, won, reshuffled with no order), and a cursor per human seat. The render model carries the items after the cursor of the human holding the device, the last human to confirm or act, named by `RecapSeat` (during a hand-off the outgoing holder, not the incoming seat), so an outgoing player still sees their own turn before the cover and an incoming one sees everyone else's after confirming. Each item carries what a screen draws: a placement's, rotate's, and bounce's cells (a bounce's both lifted and revealed), never a tile id of a lifted tile or the deck. The GUI plays them and acknowledges; the driver does not block input on a pending recap. The Unity recap player draws each item over the table (about half a second each, a second for a claim), names it in the heading, scrolls the board to it, blocks input with a full-screen layer while it runs so a tap skips only the current item, and reports done; the session adapter then acknowledges. The outgoing human's recap plays before the hand-off cover is drawn, the incoming human's after Confirm, and a one-line summary stays until the next action.
- **IPlayer** (App) is the action source for an automated seat. **AiPlayer** chooses from its own blobbities and the public board, and may decline every power.
- **Deck content** (App, `TableDeck`) is the named deck, dealt with the ordinary catalog from Rules. The shipped deck is the census, with the blobbities doubled. A seeded shuffle at the edge orders them before `Start`, so a game can be replayed. Automated play keeps a fixed order. The ordinary win count stays 4.
- **Unity player** owns the setup form, the table view, and the composition root. It submits a human's tap as an action for the current seat (a tap on a legal placement spot previews the drawn tile, and Place submits the placement) and draws the render model. It computes no legality, runs no turn loop, holds no deck, and reaches Rules and App only through the local UPM package.

A tap becomes an action, except a tap on a legal placement spot, which previews the drawn tile there until Place submits it as the action or Cancel drops it. The session hands it to the match. The match asks the rules engine, applies it, and returns events. The driver builds the render model for the current seat's view from the match's state, not by replaying events, and the GUI draws it; events are for animation, history, and shipping over a network. On an automated seat, the driver asks `IPlayer` for the next action. Pass-and-play is 2–4 human seats submitting on one `LocalSession`.

## Contracts

The only types that cross the Unity boundary are actions, events, seat-scoped views, legal-move query results, the render model, and the two interfaces. Prefabs, touch, and scenes do not appear in Rules or App. Anything handed to a seat, event or view, is scoped to that seat: another seat's unclaimed blobbity identities never appear in it. Claimed blobbities are public. A seat view includes the one match tile the next placement will consume, that tile's four cells, and which of its power cells are still unused. The rest of either deck stays hidden, and after the game has ended that next tile is absent. While the draw deck is empty the view also says the match awaits a reshuffle and lists every board tile, buried ones included, that the order must hold. The local UPM package holds both the Rules and App DLLs; there is no second package and no NuGet feed inside Unity.

The action set is closed per ruleset and typed, never a string: use rotate, use bounce, place, and reshuffle the board (accepted only while the draw deck is empty). Adding one is a change the compiler can point at. The GUI never decides legality: it highlights what the session reports and submits what was tapped; the rules engine accepts or rejects. The powers in play are setup data. A rotate, stack, or bounce symbol the setup does not list is a blank. The match can list the powers in play so a GUI can refuse to start a game it cannot present. Adding a power touches a closed set of places: a symbol in the catalog, typed legality and effect in the rules engine, and a GUI target shape only if the power needs one the GUI does not already have. A power adds its own action type and event only when it is used independently of the placement, as rotate and bounce are before it. A power that only changes where the drawn tile may go, as stack does, rides on `Place` and `TilePlaced` and adds neither.

The player must not take the rules package from a NuGet feed. The App Release build copies both DLLs (its own and Rules') into the local UPM package. Those DLLs are build output and are not committed. A later server uses a project reference, not the UPM package.

## Illustrative sketch

Guidance, not mandate. Names and shapes are the agreed contract at the level a reviewer checks; the code chooses its own identifiers.

```text
ROLES
  RulesEngine  stateless; judges one action against the board, the drawn tile, the acting seat's hand, the catalog
  Match        the game in progress: seats, hands, both decks, claim rows, turn, phase (unused charges)
  Session      the seam; Local wraps one Match, Remote talks to a server — same interface
  TurnDriver   runs the table: which seats are AI, hands the device between humans, builds the render model
  GUI          draws the render model; turns taps into actions; never decides legality

ACTIONS   (closed set per ruleset; adding one is a compiler-visible change)
  UseRotate(targetTile, quarterTurns)
  UseBounce(targetTile)
  Place(position, quarterTurns)          # on an occupied position = stack, if the drawn tile shows stack
  ReshuffleBoard(order)                  # only while the draw deck is empty; order comes from outside

EVENTS    (what happened; the GUI animates these, the network ships them)
  TileRotated · TileBounced · TilePlaced(covered?) · BlobbityClaimed · GameWon · BoardReshuffled(order)

MATCH
  Match.Start(setup) -> Match                       # decks arrive already ordered; no shuffle here
  Match.Apply(seat, action) -> Accepted(nextMatch, events) | Rejected(reason)
    reject unless seat is current and the game has not ended
    awaiting a reshuffle (draw deck empty, game not won): accept only ReshuffleBoard(order) for an exact permutation of every board tile,
      buried included; rebuild the board from the first at the origin, the rest the draw deck; seats, hands, claims, current seat unchanged
    UseRotate / UseBounce:
      reject unless the drawn tile still has an unused cell of that symbol
      reject unless RulesEngine says the target is legal (on the board; rotate: not surrounded)
      board = RulesEngine.apply(board, action); spend the charge; events += the power event
      UseRotate only: resolve claims on the rotated tile's cells (below); the turn does not pass,
        and the drawn tile stays; a win ends the game on the acting seat; UseBounce never claims
    Place:
      reject unless position is in RulesEngine.legalPlacements(board, drawnTile, quarterTurns)
      board = RulesEngine.place(board, drawnTile, position, quarterTurns)
      resolve claims on cellsJustWritten (below)
      deck advances; turn passes; phase resets

  RESOLVE CLAIMS (shared by Place and UseRotate; pure per pass, no memory of earlier passes this turn)
    completed = RulesEngine.completedBy(board, cellsJustWritten, hand, patternsInPlay)
    for each completed: move to claim row, draw a replacement, events += BlobbityClaimed
    if claim row >= claimsToWin: events += GameWon

  QUERIES  (read-only)
    Session.View(seat)           -> that seat's hand, every claim row, the board, the drawn tile, phase, charges left   # LocalSession builds it from the match's read-only accessors
    Match.LegalPlacements(turns) -> positions tagged Beside | OnTop     # empty board -> [origin]
    Match.LegalTargets(symbol)   -> board tiles that power may act on   # empty when no charge remains

TURN DRIVER  (lives in App; identical for any GUI)
  loop until GameWon or an unresolved ruling:
    seat = session.currentSeat                      # the driver goes through the session, never a Match, so RemoteSession fits
    if seat is AI:   action = ai.choose(session.view(seat)); session.submit(seat, action)
    else:            if control moved to a different human seat: conceal the hand until that seat confirms it holds the device
                     # a power use leaves the same seat current, so it does not re-conceal
                     render(model(session.view(seat))); wait for GUI submit(seat, action)

GUI  (Unity today, anything tomorrow)
  on render model: draw board, hand, each seat's score, drawn tile; show a seat's claims when its score is tapped; highlight legalPlacements(chosen turns) and legalTargets
  on tap:          build the action, submit it — never compute legality (a placement tap previews first; Place submits it)

ONLINE LATER
  same actions, same events, same driver; Session = RemoteSession that posts Apply to a server and returns the resulting state and events; the driver builds the render model and the GUI draws it, as with LocalSession

ADDING A POWER
  data:   a symbol in the catalog               (setup data — already exists)
  rules:  typed legality and board effect, always
          plus a distinct action type and event only if the power is used independently of the placement (rotate, bounce);
          a power that only changes where the drawn tile may go (stack) rides on Place and TilePlaced(covered?)
  GUI:    only if it needs a target shape the GUI doesn't already have
  AI:     optional — the AI may keep declining powers
```

## Rejected alternatives

- A JS game inside a WebView. Rejected for this horizon so the exercise matches the store-game stack.
- FastAPI as the authority. Rejected because it cannot reference the C# rules library.
- Unity as a WebView host. Rejected because the player is the game, not an installer.

## Dependency policy

Add a dependency only when the BCL or a package already required by the player cannot do the job. The license must be MIT, Apache-2.0, or BSD, or the Unity package must be under the Unity companion license. It must target `netstandard2.1` if Rules or App reference it, and it must load in Unity 6.3. Do not add a package with a known high-severity advisory. Review rejects a NuGet feed inside the Unity project for the rules library.
