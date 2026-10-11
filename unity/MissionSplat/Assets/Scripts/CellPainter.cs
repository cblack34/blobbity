namespace MissionSplat.Player
{

using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

internal static class CellPainter
{
    // The splat is drawn turned by angleDegrees, so neighbouring cells do not look stamped.
    public static void Paint(RectTransform parent, Cell cell, float angleDegrees)
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
            Splat(parent, SplatPalette.Of(color), Vector2.zero, Vector2.one, angleDegrees);
            return;
        }

        if (cell.TryGetSymbol(out var symbol))
        {
            PaintSymbol(parent, symbol);
        }
    }

    private static void PaintSymbol(RectTransform parent, SymbolId symbol)
    {
        // A blank cell draws nothing: a pale dot reads as a hole on the cream card.
        if (symbol.Value == OrdinaryCatalog.Blank.Value)
        {
            return;
        }

        Dot(parent, SplatPalette.Gray);

        if (symbol.Value == OrdinaryCatalog.Rotate.Value)
        {
            // power-ups.svg draws an open clockwise arrow. The builtin font has no such glyph.
            var arrow = Ui.Image("Rotate", parent, Color.white, RotateArrow);
            arrow.preserveAspect = true;
            Ui.Anchored(arrow.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
            return;
        }

        var mark = Ui.Label("Mark", parent, 22, Color.white, TextAnchor.MiddleCenter);
        Ui.Stretch(mark.rectTransform);
        if (symbol.Value == OrdinaryCatalog.Stack.Value)
        {
            mark.text = "+";
        }
        else if (symbol.Value == OrdinaryCatalog.Bounce.Value)
        {
            mark.text = "-";
        }
        else
        {
            mark.text = symbol.Value.Substring(0, 1);
        }
    }

    private const float RotateRadius = 17.5f;

    private const float RotateHalfStroke = 2.8f;

    private const float RotateSweep = 220f * Mathf.Deg2Rad;

    private static readonly Sprite RotateArrow = BuildRotateArrow();

    private static Sprite BuildRotateArrow()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = (size - 1) / 2f;
        var (tip, baseLeft, baseRight) = RotateHead(center);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var ink = OnRotateStroke(x, y, center) || InTriangle(x, y, tip, baseLeft, baseRight);
                texture.SetPixel(x, y, ink ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static bool OnRotateStroke(int x, int y, float center)
    {
        var dx = x - center;
        var dy = y - center;
        var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
        if (Mathf.Abs(distance - RotateRadius) > RotateHalfStroke)
        {
            return false;
        }

        return ClockwiseFromTop(Mathf.Atan2(dy, dx)) <= RotateSweep;
    }

    private static float ClockwiseFromTop(float angle)
    {
        var span = (Mathf.PI / 2f) - angle;
        if (span < 0f)
        {
            span += Mathf.PI * 2f;
        }

        return span;
    }

    private static (Vector2 Tip, Vector2 BaseLeft, Vector2 BaseRight) RotateHead(float center)
    {
        var theta = (Mathf.PI / 2f) - RotateSweep;
        var forward = new Vector2(Mathf.Sin(theta), -Mathf.Cos(theta));
        var radial = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
        var origin = new Vector2(center, center) + (radial * RotateRadius);
        var back = origin - (forward * 6f);
        var side = new Vector2(-forward.y, forward.x) * 7.5f;
        return (origin + (forward * 10f), back + side, back - side);
    }

    private static bool InTriangle(int x, int y, Vector2 tip, Vector2 baseLeft, Vector2 baseRight)
    {
        var point = new Vector2(x, y);
        var ab = Cross(baseLeft - tip, point - tip);
        var bc = Cross(baseRight - baseLeft, point - baseLeft);
        var ca = Cross(tip - baseRight, point - baseRight);
        var positive = ab >= 0f && bc >= 0f && ca >= 0f;
        var negative = ab <= 0f && bc <= 0f && ca <= 0f;
        return positive || negative;
    }

    private static float Cross(Vector2 u, Vector2 v) => (u.x * v.y) - (u.y * v.x);

    private static void Dot(RectTransform parent, Color color)
    {
        var image = Ui.Image("Dot", parent, color, Ui.Circle);
        image.preserveAspect = true;
        Ui.Anchored(image.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
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
}
}
