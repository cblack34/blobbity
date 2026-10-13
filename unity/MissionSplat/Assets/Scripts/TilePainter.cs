namespace MissionSplat.Player
{

using System;
using MissionSplat.Rules;
using UnityEngine;

internal static class TilePainter
{
    // The edge line around the face, as a fraction of the block's width.
    private const float CardEdge = 0.035f;

    // The card and its cells, drawn the same way on the board and for the drawn tile. block is the tile's 2×2 cells (sizeDelta is its size),
    // cellAt gives the cell at local (x, y), and quarterTurns moves each cell clockwise to its slot as Rules.Tile does.
    // The card is inset so neighbours show a gap; the block itself stays whole so taps in the gap still reach the highlights drawn over it.
    public static void Paint(RectTransform block, string tileId, Func<int, int, Cell> cellAt, int quarterTurns)
    {
        CardPainter.Paint(block, Ui.Card, CardEdge, block.sizeDelta.x);

        // The cells fill the card's face, inside its gap and edge, so nothing a cell draws crosses the outline.
        var margin = CardPainter.InsetUnits(block.sizeDelta.x) + CardPainter.EdgeUnits(CardEdge, block.sizeDelta.x);
        var cells = Ui.Rect("Cells", block);
        Ui.Anchored(cells, Vector2.zero, Vector2.one, new Vector2(margin, margin), new Vector2(-margin, -margin));
        var cell = (block.sizeDelta.x - (2f * margin)) / 2f;
        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                var cellRect = Ui.Rect("C" + x + y, cells);
                var (slotX, slotY) = TurnClockwise(x, y, quarterTurns);
                Ui.PlaceCell(cellRect, slotX, slotY, 1, 1, 0, 0, cell, 0f, 0f);
                // Keyed by the slot after the quarter-turn, on purpose, so the turn preview shows the same angles that land on the board.
                CellPainter.Paint(cellRect, cellAt(x, y), CellPainter.SplatAngle(tileId + ":" + slotX + slotY));
            }
        }
    }

    // Same clockwise map as Rules.Tile: (x, y) goes to (y, 1 - x).
    private static (int X, int Y) TurnClockwise(int x, int y, int quarterTurns) =>
        quarterTurns switch
        {
            0 => (x, y),
            1 => (y, 1 - x),
            2 => (1 - x, 1 - y),
            3 => (1 - y, x),
            _ => throw new ArgumentOutOfRangeException(nameof(quarterTurns), quarterTurns, "Quarter-turns are 0, 1, 2, or 3."),
        };
}
}
