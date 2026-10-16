namespace Blobbity.Player
{

using System.Collections.Generic;
using Blobbity.Rules;
using UnityEngine;
using UnityEngine.UI;

internal static class CellPainter
{
    // The name of a cell's power badge, for whatever looks for it.
    public const string BadgeName = "Badge";

    // The name of the mark on a colour splat, for whatever looks for it. A mark tells a colour without seeing it; it is not a rules symbol.
    public const string MarkName = "Mark";

    // The middle of the splat's body, as anchors of a square splat Image. The body's centre (the point farthest from its edge) was measured at
    // (71.5, 73.5) in art/splat.svg's 148 x 141 box; it is not the box's centre. The sprite is 256 x 244 and drawn with preserveAspect, so in a
    // square rect it fills the width and is letterboxed vertically to its height over width (244/256 today). x = 71.5 / 148 = 0.483. y, measured
    // up from the bottom: 0.5 + (0.5 - 73.5 / 141) * (244 / 256) = 0.480. A rect that is not square would move the body, so every caller hands
    // over a square. Recompute the measurement if art/splat.svg changes.
    private static readonly Vector2 BodyCentre =
        new Vector2(71.5f / 148f, 0.5f + ((0.5f - (73.5f / 141f)) * (Ui.Splat.rect.height / Ui.Splat.rect.width)));

    // The mark's art is a 64-unit box, which on the splat's 148-unit width reaches 32 / 148 of the splat either side of the centre.
    private const float MarkHalfExtent = 32f / 148f;

    // The wild's sparkle is centred where the four quarter windows meet, so every window holds the same share of it; it is drawn larger than a
    // colour's symbol, because its thin points are lost at board size otherwise.
    private static readonly Vector2 WildCentre = new Vector2(0.5f, 0.5f);
    private const float WildSparkleHalfExtent = 0.3f;

    // Each game colour's mark and the deep shade it is drawn in, keyed by colour id. Any other colour has no mark.
    private static readonly Dictionary<string, (Sprite Sprite, Color Deep)> Marks = new Dictionary<string, (Sprite, Color)>
    {
        [OrdinaryCatalog.Red.Value] = (Ui.MarkHeart, SplatPalette.DeepRed),
        [OrdinaryCatalog.Blue.Value] = (Ui.MarkTriangle, SplatPalette.DeepBlue),
        [OrdinaryCatalog.Green.Value] = (Ui.MarkSquare, SplatPalette.DeepGreen),
        [OrdinaryCatalog.Purple.Value] = (Ui.MarkStar, SplatPalette.DeepPurple),
    };

    // A power's badge fills this much of the cell, centred.
    private const float BadgeMargin = 0.16f;

    // The splat is drawn turned by angleDegrees, so neighbouring cells do not look stamped. A power's badge draws gray once its use is spent.
    public static void Paint(RectTransform parent, Cell cell, float angleDegrees, bool spent = false)
    {
        if (cell.IsWild)
        {
            // Unturned: the four quarters are one splat, and turning each about its own centre would make a pinwheel.
            Quarter(parent, OrdinaryCatalog.Red, 0, 1);
            Quarter(parent, OrdinaryCatalog.Blue, 1, 1);
            Quarter(parent, OrdinaryCatalog.Green, 0, 0);
            Quarter(parent, OrdinaryCatalog.Purple, 1, 0);
            return;
        }

        if (cell.TryGetColor(out var color))
        {
            ColourSplat(parent, color, Vector2.zero, Vector2.one, angleDegrees, 0.03f);
            return;
        }

        if (cell.TryGetSymbol(out var symbol))
        {
            PaintSymbol(parent, symbol, spent);
        }
    }

    private static void PaintSymbol(RectTransform parent, SymbolId symbol, bool spent)
    {
        // A blank cell draws nothing: a pale dot reads as a hole on the white card.
        if (symbol.Value == OrdinaryCatalog.Blank.Value)
        {
            return;
        }

        var badge = Ui.Rect(BadgeName, parent);
        Ui.Anchored(badge, new Vector2(BadgeMargin, BadgeMargin), new Vector2(1f - BadgeMargin, 1f - BadgeMargin), Vector2.zero, Vector2.zero);
        PowerBadge.Paint(badge, symbol, spent);
    }

    // One splat shown through a mask over a quarter of the cell. Every quarter holds the same full-cell splat, so the four line up into one.
    // Each also holds the same full-cell sparkle in its colour's deep shade, so the windows cut the sparkle along the splat's own lines.
    private static void Quarter(RectTransform parent, ColorId colour, int quarterX, int quarterY)
    {
        var window = Ui.Rect("Quarter", parent);
        Ui.Anchored(window, new Vector2(quarterX * 0.5f, quarterY * 0.5f), new Vector2((quarterX + 1) * 0.5f, (quarterY + 1) * 0.5f), Vector2.zero, Vector2.zero);
        window.gameObject.AddComponent<RectMask2D>();
        // Anchors are relative to the quarter, which is half the cell, so the cell's extent is -quarter..2-quarter.
        var shift = new Vector2(quarterX, quarterY);
        var splat = Splat(window, SplatPalette.Of(colour), -shift, new Vector2(2f, 2f) - shift, 0f);
        PaintMark(splat, Ui.MarkSparkle, Marks[colour.Value].Deep, WildCentre, WildSparkleHalfExtent, 0f);
    }

    // A splat for a game colour, marked so it can be told without seeing the colour. A colour with no mark gets none.
    // The area between min and max must be square, because the mark's place on the splat's body assumes a square (see BodyCentre).
    internal static void ColourSplat(RectTransform parent, ColorId colour, Vector2 min, Vector2 max, float angleDegrees, float padding = 0.1f)
    {
        var splat = Splat(parent, SplatPalette.Of(colour), min, max, angleDegrees, padding);
        if (Marks.TryGetValue(colour.Value, out var mark))
        {
            PaintMark(splat, mark.Sprite, mark.Deep, BodyCentre, MarkHalfExtent, angleDegrees);
        }
    }

    // The mark is a child of the splat, so it rides the splat's turn, and is turned back by the same angle so it stays upright.
    private static void PaintMark(Image splat, Sprite sprite, Color deep, Vector2 centre, float halfExtent, float splatAngleDegrees)
    {
        var mark = Ui.Image(MarkName, splat.transform, deep, sprite);
        mark.preserveAspect = true;
        var half = new Vector2(halfExtent, halfExtent);
        Ui.Anchored(mark.rectTransform, centre - half, centre + half, Vector2.zero, Vector2.zero);
        mark.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -splatAngleDegrees);
    }

    // Fills the area between min and max (anchors of the parent), less padding on each side as a fraction of it.
    // A negative padding overfills, for small splats whose transparent spikes would otherwise look tiny.
    internal static Image Splat(RectTransform parent, Color color, Vector2 min, Vector2 max, float angleDegrees, float padding = 0.1f)
    {
        var image = Ui.Image("Splat", parent, color, Ui.Splat);
        image.preserveAspect = true;
        var span = max - min;
        Ui.Anchored(image.rectTransform, min + (span * padding), max - (span * padding), Vector2.zero, Vector2.zero);
        image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angleDegrees);
        return image;
    }

    // A stable turn for a splat from a key of the caller's choosing: the same key turns the same way on every run
    // (string.GetHashCode is not stable between runs).
    internal static float SplatAngle(string key)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var c in key)
            {
                hash = (hash ^ c) * 16777619u;
            }

            hash ^= hash >> 15;
            return hash % 360u;
        }
    }
}
}
