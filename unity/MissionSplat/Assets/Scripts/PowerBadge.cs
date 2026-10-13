namespace MissionSplat.Player
{

using MissionSplat.Rules;
using UnityEngine;

// A power's badge, on a tile cell and on a tray chip: a coloured disc with an ink rim and the power's mark in ink
// (rotate yellow with its arrow, stack blue with +, bounce pink with -).
internal static class PowerBadge
{
    // The rim's share of the badge's width on each side.
    private const float RimFraction = 0.07f;

    // The mark's margin inside the face, as a fraction of it.
    private const float MarkMargin = 0.12f;

    // A bar of the plus or minus: its length and thickness as fractions of the face.
    private const float BarLength = 0.56f;
    private const float BarThickness = 0.13f;

    // Draws the whole badge filling parent, which should be square.
    public static void Paint(RectTransform parent, SymbolId power)
    {
        var rim = Ui.Image("Rim", parent, SplatPalette.Ink, Ui.Circle);
        Ui.Stretch(rim.rectTransform);
        var face = Ui.Image("Face", rim.transform, SplatPalette.BadgeOf(power), Ui.Circle);
        Ui.Anchored(face.rectTransform, new Vector2(RimFraction, RimFraction), new Vector2(1f - RimFraction, 1f - RimFraction), Vector2.zero, Vector2.zero);
        PaintMark(face.rectTransform, power);
    }

    // The power's mark on its own, in ink, centred in face: the rotate arrow, or a plus or minus drawn as bars
    // (the glyphs of a font would sit off-centre in a disc). A chip draws it on the face of its own button.
    public static void PaintMark(RectTransform face, SymbolId power)
    {
        if (power.Equals(OrdinaryCatalog.Rotate))
        {
            var arrow = Ui.Image("Rotate", face, SplatPalette.Ink, RotateArrow);
            arrow.preserveAspect = true;
            Ui.Anchored(arrow.rectTransform, new Vector2(MarkMargin, MarkMargin), new Vector2(1f - MarkMargin, 1f - MarkMargin), Vector2.zero, Vector2.zero);
            return;
        }

        if (power.Equals(OrdinaryCatalog.Bounce) || power.Equals(OrdinaryCatalog.Stack))
        {
            PaintBar(face, "Bar", BarLength, BarThickness);
            if (power.Equals(OrdinaryCatalog.Stack))
            {
                PaintBar(face, "Bar", BarThickness, BarLength);
            }

            return;
        }

        // A symbol with no mark of its own shows its initial.
        var letter = Ui.Label("Mark", face, 40, SplatPalette.Ink, TextAnchor.MiddleCenter, FontWeight.ExtraBold);
        letter.resizeTextForBestFit = true;
        letter.resizeTextMinSize = 1;
        letter.resizeTextMaxSize = 200;
        Ui.Stretch(letter.rectTransform);
        letter.text = power.Value.Substring(0, 1).ToUpperInvariant();
    }

    // A bar centred in the face, width and height as fractions of it.
    private static void PaintBar(RectTransform face, string name, float width, float height)
    {
        var bar = Ui.Image(name, face, SplatPalette.Ink);
        Ui.Anchored(bar.rectTransform, new Vector2(0.5f - (width / 2f), 0.5f - (height / 2f)), new Vector2(0.5f + (width / 2f), 0.5f + (height / 2f)), Vector2.zero, Vector2.zero);
    }

    private const float RotateRadius = 17.5f;

    private const float RotateHalfStroke = 2.8f;

    private const float RotateSweep = 220f * Mathf.Deg2Rad;

    internal static readonly Sprite RotateArrow = BuildRotateArrow();

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
}
}
