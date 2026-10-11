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
            Quarter(parent, SplatPalette.Red, 0, 1, angleDegrees);
            Quarter(parent, SplatPalette.Blue, 1, 1, angleDegrees);
            Quarter(parent, SplatPalette.Green, 0, 0, angleDegrees);
            Quarter(parent, SplatPalette.Purple, 1, 0, angleDegrees);
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

    public static void PaintMission(RectTransform parent, Mission mission)
    {
        var fill = Ui.Image("Fill", parent, Color.white);
        Ui.Stretch(fill.rectTransform);
        var color = SplatPalette.Of(mission.Color);
        var lattice = Ui.Rect("Lattice", parent);
        lattice.anchorMin = new Vector2(0.5f, 0.58f);
        lattice.anchorMax = new Vector2(0.5f, 0.58f);
        lattice.pivot = new Vector2(0.5f, 0.5f);
        var side = Mathf.Min(parent.rect.width, parent.rect.height * 0.72f);
        if (side <= 1f)
        {
            side = 88f;
        }

        lattice.sizeDelta = new Vector2(side, side);
        switch (mission.Pattern)
        {
            case MissionPattern.Row:
                SquareDots(lattice, color, new[] { (0, 1), (1, 1), (2, 1), (3, 1) }, 4, 3);
                break;
            case MissionPattern.Square:
                SquareDots(lattice, color, new[] { (0, 1), (1, 1), (0, 0), (1, 0) }, 2, 2);
                break;
            default:
                SquareDots(lattice, color, new[] { (0, 2), (0, 1), (0, 0), (1, 0) }, 2, 3);
                break;
        }

        var caption = Ui.Label("Caption", parent, 16, SplatPalette.Ink, TextAnchor.LowerCenter);
        Ui.Anchored(caption.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.22f), Vector2.zero, Vector2.zero);
        caption.text = PatternName(mission.Pattern);
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
    private static void Quarter(RectTransform parent, Color color, int quarterX, int quarterY, float angleDegrees)
    {
        var window = Ui.Rect("Quarter", parent);
        Ui.Anchored(window, new Vector2(quarterX * 0.5f, quarterY * 0.5f), new Vector2((quarterX + 1) * 0.5f, (quarterY + 1) * 0.5f), Vector2.zero, Vector2.zero);
        window.gameObject.AddComponent<RectMask2D>();
        // Anchors are relative to the quarter, which is half the cell, so the cell's extent is -quarter..2-quarter.
        var shift = new Vector2(quarterX, quarterY);
        Splat(window, color, Vector2.zero - shift, new Vector2(2f, 2f) - shift, angleDegrees);
    }

    // Fills most of the area between min and max, which are anchors of the parent.
    private static void Splat(RectTransform parent, Color color, Vector2 min, Vector2 max, float angleDegrees)
    {
        var image = Ui.Image("Splat", parent, color, Ui.Splat);
        image.preserveAspect = true;
        var span = max - min;
        Ui.Anchored(image.rectTransform, min + (span * 0.1f), max - (span * 0.1f), Vector2.zero, Vector2.zero);
        image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angleDegrees);
    }

    private static void SquareDots(RectTransform lattice, Color color, (int x, int y)[] cells, int gridW, int gridH)
    {
        var span = Mathf.Max(gridW, gridH);
        var cell = lattice.sizeDelta.x / span;
        var originX = (lattice.sizeDelta.x - (gridW * cell)) * 0.5f;
        var originY = (lattice.sizeDelta.y - (gridH * cell)) * 0.5f;
        var pad = cell * 0.12f;
        for (var i = 0; i < cells.Length; i++)
        {
            var image = Ui.Image("Dot" + i, lattice, color, Ui.Circle);
            image.preserveAspect = true;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(cell - pad, cell - pad);
            rect.anchoredPosition = new Vector2(
                originX + (cells[i].x * cell) + (pad * 0.5f),
                originY + (cells[i].y * cell) + (pad * 0.5f));
        }
    }

    private static string PatternName(MissionPattern pattern) =>
        pattern switch
        {
            MissionPattern.Row => "row",
            MissionPattern.Square => "square",
            _ => "L",
        };
}
}
