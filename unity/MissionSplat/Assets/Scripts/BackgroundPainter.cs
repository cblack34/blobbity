namespace MissionSplat.Player
{

using UnityEngine;

// The page behind everything: big faint splats spilling off the screen edges, and three paint drips from the top.
// It is only decoration, so nothing in it takes a tap. Splats are placed by fractions of the screen and sized by the canvas width,
// so they suit any portrait phone. The layout is the design reference's, in docs/design.
internal static class BackgroundPainter
{
    private const float SplatOpacity = 0.16f;
    private const float DripOpacity = 0.35f;
    private const float DripWidthUnits = 50f;

    // A drip's rounded top is drawn this far above the screen, out of sight.
    private const float DripOverhangUnits = 60f;

    // Where a splat's centre sits as fractions of the screen (x from the left, y from the top), its width as a fraction of the canvas width, and its turn.
    private static readonly (float X, float Y, float Width, float Angle, Color Colour)[] Splats =
    {
        (-0.051f, 0.201f, 0.667f, 20f, SplatPalette.Blue),
        (1.026f, 0.107f, 0.590f, 110f, SplatPalette.Red),
        (1.051f, 0.664f, 0.718f, 200f, SplatPalette.Green),
        (-0.103f, 0.900f, 0.769f, 300f, SplatPalette.Purple),
        (0.513f, 1.019f, 0.513f, 40f, SplatPalette.Red),
    };

    // Drips hang from the top edge: where the centre sits as a fraction of the width, and how long it is in canvas units, rounded end included.
    private static readonly (float X, float Length, Color Colour)[] Drips =
    {
        (0.154f, 136f, SplatPalette.Purple),
        (0.385f, 202f, SplatPalette.Blue),
        (0.769f, 108f, SplatPalette.Green),
    };

    // Paints into parent, which should be the whole screen.
    public static void Paint(RectTransform parent)
    {
        foreach (var splat in Splats)
        {
            var image = Ui.Image("Splat", parent, Faint(splat.Colour, SplatOpacity), Ui.Splat);
            var width = splat.Width * Ui.ReferenceWidthUnits;
            Place(image.rectTransform, splat.X, 1f - splat.Y, new Vector2(width, width * Ui.Splat.rect.height / Ui.Splat.rect.width));
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, splat.Angle);
        }

        foreach (var drip in Drips)
        {
            var image = Ui.Image("Drip", parent, Faint(drip.Colour, DripOpacity), Ui.Rounded);
            Place(image.rectTransform, drip.X, 1f, new Vector2(DripWidthUnits, drip.Length + DripOverhangUnits));
            image.rectTransform.pivot = new Vector2(0.5f, 1f);
            image.rectTransform.anchoredPosition = new Vector2(0f, DripOverhangUnits);
        }
    }

    // Centres rect on a point given as fractions of its parent.
    private static void Place(RectTransform rect, float x, float y, Vector2 size)
    {
        rect.anchorMin = new Vector2(x, y);
        rect.anchorMax = new Vector2(x, y);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
    }

    private static Color Faint(Color colour, float opacity) => new Color(colour.r, colour.g, colour.b, opacity);
}
}
