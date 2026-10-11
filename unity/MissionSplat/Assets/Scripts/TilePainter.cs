namespace MissionSplat.Player
{

using System;
using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

internal static class TilePainter
{
    // The card and its cells, drawn the same way on the board and for the drawn tile. block is the tile's 2×2 cells (sizeDelta is its size),
    // cellAt gives the cell at local (x, y), and quarterTurns moves each cell clockwise to its slot as Rules.Tile does.
    // The card is inset so neighbours show a gap; the block itself stays whole so taps in the gap still reach the highlights drawn over it.
    public static void Paint(RectTransform block, string tileId, Func<int, int, Cell> cellAt, int quarterTurns)
    {
        PaintCard(block, Ui.Card, 0.025f, 0.015f, 1f);

        var cell = block.sizeDelta.x / 2f;
        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                var cellRect = Ui.Rect("C" + x + y, block);
                var (slotX, slotY) = TurnClockwise(x, y, quarterTurns);
                Ui.PlaceCell(cellRect, slotX, slotY, 1, 1, 0, 0, cell, 0f, 0f);
                CellPainter.Paint(cellRect, cellAt(x, y), SplatAngle(tileId, slotX, slotY));
            }
        }
    }

    // The shadow, edge and face of a card, used for tiles and for mission cards. inset and edge are fractions of the block's width;
    // widthPerHeight makes the same fraction of its height as many pixels, so a portrait card keeps an even border.
    internal static void PaintCard(RectTransform block, Sprite sprite, float inset, float edge, float widthPerHeight)
    {
        PaintCardLayer(block, "Shadow", SplatPalette.CardShadow, sprite, inset, widthPerHeight, -0.02f);
        PaintCardLayer(block, "Edge", SplatPalette.CardEdge, sprite, inset, widthPerHeight, 0f);
        PaintCardLayer(block, "Face", SplatPalette.CardFace, sprite, inset + edge, widthPerHeight, 0f);
    }

    // One layer of the card: the block inset on every side, then moved up by liftFraction of the block's height.
    private static void PaintCardLayer(RectTransform block, string name, Color color, Sprite sprite, float inset, float widthPerHeight, float liftFraction)
    {
        var image = Ui.Image(name, block, color, sprite);
        var insetY = inset * widthPerHeight;
        Ui.Anchored(image.rectTransform, new Vector2(inset, insetY + liftFraction), new Vector2(1f - inset, 1f - insetY + liftFraction), Vector2.zero, Vector2.zero);
    }

    // A stable turn for each splat: the same tile and slot are turned the same way on every run (string.GetHashCode is not stable).
    // The key is the tile id plus the slot after the quarter-turn, on purpose, so the turn preview shows the same angles that land on the board.
    internal static float SplatAngle(string tileId, int slotX, int slotY)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var c in tileId + ":" + slotX + slotY)
            {
                hash = (hash ^ c) * 16777619u;
            }

            hash ^= hash >> 15;
            return hash % 360u;
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
