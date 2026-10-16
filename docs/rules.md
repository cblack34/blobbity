# Rules

This file is the behavioral contract for Blobbity. Schematic drawings in [`diagrams/`](diagrams/) illustrate it. They are not a second source of truth. Source photographs of the physical game are intentionally absent.

Names below are original. They describe the same structure as the photographed instructions: secret blobbities, square match tiles, orthogonal placement, and a claim that only the seat that wrote the completing cells, by placing or rotating, may take.

## Pieces

- A **match tile** is a square 2×2 of cells. A cell is a scoring color, a non-scoring symbol, or a wildcard.
- A **scoring color** is an id in the color catalog passed at setup. The ordinary catalog is red, blue, green, and purple. A pattern check compares the blobbity's color id to the cell. It does not special-case those four names, so another color passed at setup matches the same way.
- A **non-scoring symbol** does not complete a pattern. The ordinary symbols are blank, rotate, stack, and bounce. Blank has no power. Rotate, stack, and bounce are the power symbols, drawn gray. Gray is not a scoring color. Another symbol passed at setup is the same kind of data and also does not score.
- A **wildcard** is a quartered cell. It is not gray. It is always wild. No action names a color for it, and nothing stores a chosen color. Each blobbity check counts the cell as that blobbity's color, so one cell can count as red for one blobbity and blue for another. A wildcard on the starting tile stays wild.
- A **blobbity** is a secret objective: four in a row, four in a square, or four in an L, in one scoring color. The patterns in play are the set passed at setup. The ordinary set is the row, the square, and the L. A shape that was not passed in does not claim.
- A **claim** is a blobbity laid face up in front of the seat that completed it. The claim row is not part of the board.

## Setup

The library does not shuffle. It takes the seats in turn order, the first seat, the color catalog, the non-scoring symbols, the patterns in play, the claims required to win, and the blobbity and match decks already ordered. The base deck is [`census.md`](census.md). A fixture may use a smaller deck if it names that deck. The ordinary game uses the ordinary catalog above and 4 claims. Setup can pass another positive count.

Of the non-scoring symbols passed at setup, those that are rotate, stack, or bounce are the powers in play. Any other listed symbol is non-scoring and grants no use. A rotate, stack, or bounce symbol that setup does not list is read as a blank: it does not score and grants no use.

1. Deal two blobbities from the front of the blobbity deck to each seat, in the seat order given to setup.
2. Place the front match tile at the origin of the board, in the orientation it has in the deck.

A game has two, three, or four seats. Who is first: the physical rule is the youngest seat. The session takes the first seat as an explicit start input: the humans agree on it, and with an AI seat the human chooses. Rules never compute it. That choice is an assumption recorded in the brief. A starting tile is not a placement, so no claim resolves at setup, even when its cells already show a pattern.

## Turn

1. The current seat draws the top match tile. It stays in hand until step 3 places it.
2. If the drawn tile shows rotate or bounce, the seat may use each such cell once, before placing, under the power rules below. A tile showing two different powers is an open question, listed below, that this step does not decide. A use acts on a tile already on the board; the drawn tile is in hand and is never a target. Uses are chosen one at a time, and each is applied before the next is chosen. A rotate resolves claims at once, as under Claim.
3. The seat places the drawn tile, choosing a quarter-turn orientation as it is set down. Quarter-turns are clockwise. That choice is part of placement, not the rotate power. Ordinary placement must share at least one full side with a tile already on the board. Corner-only contact is illegal. See [`diagrams/placement.svg`](diagrams/placement.svg). If the drawn tile shows stack, the seat may instead place it on top of a tile already on the board. Stack is this placement, not a separate step. If the board is empty, the drawn tile is placed at the origin.
4. Resolve the placement's claims. Then the next seat plays. A rotate win ends the game with the turn still on the acting seat.

When a placement leaves the draw deck empty and the game is not won (or setup supplies no tile beyond the start tile), the board is reshuffled before the next seat draws. Every tile on the board, buried ones included, is gathered and shuffled into a new draw deck. The top tile is placed at the origin as a new start tile: that is not a placement, and it claims nothing. Claims, hands, the blobbity deck, and turn order are unchanged, and the next seat draws as usual, except that when the board and draw deck together hold only one tile (a one-tile game), a reshuffle cannot produce a tile to draw and play cannot continue. Until the reshuffle is played the match accepts nothing else. The library does not shuffle: it takes the new order from outside, as it takes the decks at setup.

A rejected use or placement does not change the board, the decks, or whose turn it is. A use that was accepted stays applied even if a later use or the placement is rejected; the seat chooses again. A tile that cannot be placed legally does not get discarded by a rule the photographs state. That case is an open edge: stop and return it to the user rather than inventing a discard rule.

## Placement

- The board is an irregular orthogonal cluster of tiles. It is not a filled rectangle.
- Matching reads the cell grid. Placing a tile writes that tile's four cells. The cards are the visual; the grid is the match space.
- Tiles stay axis-aligned. The rotate power turns a tile already on the board in 90-degree steps; it does not allow diagonal placement.
- Two tiles may occupy the same board position only by the stack power. Otherwise a cell of the board holds one tile.
- An empty board has no side to share. The drawn tile is then placed at the origin, in the orientation the seat chooses.
- A square may be four corner cells from four different tiles, one from each tile, where the four tiles meet.

## Blobbities

A blobbity looks at one scoring color on the cell grid. A wildcard counts as the color of the blobbity being checked. It is not assigned a color first. A blank or any other non-scoring symbol does not complete the color.

- **Four in a row.** Four cells in a horizontal, vertical, or diagonal line. See [`diagrams/mission-row.svg`](diagrams/mission-row.svg).
- **Four in a square.** Four cells in a 2×2 block. See [`diagrams/mission-square.svg`](diagrams/mission-square.svg). The block may be the four cells where four tiles meet.
- **Four in an L.** Three cells in a line plus a fourth adjacent to one end, any rotation or reflection. That is eight orientations. It is not a T, a skew, or a square. See [`diagrams/mission-l.svg`](diagrams/mission-l.svg).

Only a pattern in the set passed at setup can be claimed. A seat claims a pattern only when the cells it just wrote, by placing a tile or rotating one, completed it.

## Claim

A seat claims a blobbity only by writing cells that complete that blobbity's pattern, on that seat's turn: by placing the match tile, or by rotating a tile already on the board. If another seat's placement or rotate completes your pattern, you do not claim it, including when you later place a tile that adds no cell to that pattern.

One placement or rotate claims every secret blobbity the acting seat currently holds whose pattern that resolution completed, but each claimed blobbity must be completed by its own four cells. Cells may be shared between claims, but two claims never use the identical four cells. The comparison is by cells, not colour: four wildcards in a row complete a red line and a blue line on the same four cells, so they claim only one of them. When blobbities compete for the same cells, the resolution claims as many as it can, and earlier cards in the hand win ties. A blobbity left unclaimed stays in hand. Other seats do not claim from that placement or rotate.

Only cells the acting seat wrote this turn can complete a blobbity, and a claim resolves when the cells are written. A placement writes the placed tile's four cells. A rotate writes the rotated tile's four cells in their new orientation, so it resolves claims at once, scoped to those four cells, the same way a placement does. A rotate writes all four of the tile's cells even if their colours come out unchanged; a turn of 1 to 3 quarters is what counts. A bounce writes nothing: it removes a tile and reveals cells nobody wrote this turn, so it never claims, and a pattern it exposes is claimed only if the placed tile's cells complete it. Stack writes the drawn tile's cells on top of the covered position, so a pattern through those cells claims normally.

Distinct cells apply per resolution: each rotate and each placement is its own resolution, so the same four cells may complete one blobbity on a rotate and a different one on the turn's placement. A replacement drawn after a rotate claim may be claimed later that turn by the placement, if the placement completes it.

On those claims, as one resolution:

1. For each such blobbity, lay it face up in the seat's claim row and draw one replacement.
2. Do not claim a replacement drawn during this resolution, even if the board already shows its pattern.
3. A placement then ends the turn, and one placement resolution passes the turn once. A rotate resolution does not end the turn: the seat keeps the drawn tile and may place it, or use another power.

The ordinary win count is 4. Setup can require a different positive count. The first seat whose claim row reaches that count wins. If one placement's claims cross the count together, every claim from that placement still counts, and the win is emitted after them. The winning placement is not rejected. A later action is rejected. A rotate whose claims reach the count wins at once: the game ends there, the drawn tile is never placed, and a later action is rejected. The claim row is the score. It is not scanned as board cells.

## Powers

A power is printed on the tile that was drawn. Rotate and bounce are used before that tile is placed, one use per cell, each on a tile already on the board. Stack changes where that tile may be placed. On a tile whose power cells are all the same power, using that power is optional; a tile showing two different powers is an open question, listed below. The photographs describe these effects. See [`diagrams/power-ups.svg`](diagrams/power-ups.svg).

- **Rotate.** Turn one tile on the board by one, two, or three clockwise quarter-turns if that tile is not completely surrounded. Completely surrounded means all four orthogonal neighbors are occupied, counting only tiles on the board. Only the chosen tile's visible cells turn. The turned tile's four cells are written, so claims resolve at once from them, as under Claim; a rotate claim does not end the turn.
- **Stack.** Place the drawn tile on top of any tile on the board instead of beside it. The top tile's cells are the cells that match at that position. The covered tiles remain under that position until a bounce uncovers the top one.
- **Bounce.** Never claims, even if the cells it reveals form a pattern. Remove any one tile from the board and put it on the bottom of the draw deck. If the named position has a tile buried beneath it, only the top tile is removed; the tile beneath becomes visible again, with the cells it had at the moment it was covered, not the cells its own placement orientation would otherwise show. If the named position has no buried tile, the position is removed from the board entirely. The drawn tile is in hand and cannot be bounced. A bounce may empty the board; the drawn tile is then placed at the origin. A bounce may split the board into separate regions with no shared side between them; that is legal. The photographs never address board connectivity, and nothing else in these rules requires the board to stay one connected cluster.
- **Blank.** No effect.

A tile may show more than one cell of the same power. Each rotate or bounce cell on the drawn tile may be used once on the turn it is drawn. Stack is not a repeatable action: one or more stack cells on the drawn tile grant the optional stack placement once, and extra stack cells do not grant a second placement. Powers on tiles already on the board are not reused. The base deck has no tile showing two different powers. Whether such a tile is allowed, and whether a seat may use one of its powers and pass on the other, is a ruleset question, open until a deck has such a tile. The statements above about using each cell do not decide it.

## AI

The AI seat has its own two secret blobbities. It does not observe any other seat's blobbities, human or AI. It must play a legal placement. A heuristic that prefers its own blobbities is enough for the POC. Search with hidden information is out of scope.

## What the photographs do not decide

These are open. Do not invent a ruling in code without returning to the user.

- A drawn tile that has no legal orthogonal neighbor and no stack power, on a board that is not empty.
- A drawn tile that shows two different powers: whether one may be used and the other passed. The turn and power rules above cover a tile whose power cells are all one power.
