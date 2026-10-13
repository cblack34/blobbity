namespace MissionSplat.Player
{

using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

internal static class CellPainter
{
    // A power's badge fills this much of the cell, centred.
    private const float BadgeMargin = 0.16f;

    // The splat is drawn turned by angleDegrees, so neighbouring cells do not look stamped. A power's badge draws gray once its use is spent.
    public static void Paint(RectTransform parent, Cell cell, float angleDegrees, bool spent = false)
    {
        if (cell.IsWild)
        {
            // Unturned: the four quarters are one splat, and turning each about its own centre would make a pinwheel.
            Quarter(parent, SplatPalette.Red, 0, 1);
            Quarter(parent, SplatPalette.Blue, 1, 1);
            Quarter(parent, SplatPalette.Green, 0, 0);
            Quarter(parent, SplatPalette.Purple, 1, 0);
            return;
        }

        if (cell.TryGetColor(out var color))
        {
            Splat(parent, SplatPalette.Of(color), Vector2.zero, Vector2.one, angleDegrees, 0.03f);
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

        var badge = Ui.Rect("Badge", parent);
        Ui.Anchored(badge, new Vector2(BadgeMargin, BadgeMargin), new Vector2(1f - BadgeMargin, 1f - BadgeMargin), Vector2.zero, Vector2.zero);
        PowerBadge.Paint(badge, symbol, spent);
    }

    // One splat shown through a mask over a quarter of the cell. Every quarter holds the same full-cell splat, so the four line up into one.
    private static void Quarter(RectTransform parent, Color color, int quarterX, int quarterY)
    {
        var window = Ui.Rect("Quarter", parent);
        Ui.Anchored(window, new Vector2(quarterX * 0.5f, quarterY * 0.5f), new Vector2((quarterX + 1) * 0.5f, (quarterY + 1) * 0.5f), Vector2.zero, Vector2.zero);
        window.gameObject.AddComponent<RectMask2D>();
        // Anchors are relative to the quarter, which is half the cell, so the cell's extent is -quarter..2-quarter.
        var shift = new Vector2(quarterX, quarterY);
        Splat(window, color, -shift, new Vector2(2f, 2f) - shift, 0f);
    }

    // Fills the area between min and max (anchors of the parent), less padding on each side as a fraction of it.
    // A negative padding overfills, for small splats whose transparent spikes would otherwise look tiny.
    internal static void Splat(RectTransform parent, Color color, Vector2 min, Vector2 max, float angleDegrees, float padding = 0.1f)
    {
        var image = Ui.Image("Splat", parent, color, Ui.Splat);
        image.preserveAspect = true;
        var span = max - min;
        Ui.Anchored(image.rectTransform, min + (span * padding), max - (span * padding), Vector2.zero, Vector2.zero);
        image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angleDegrees);
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
