namespace Blobbity.Player
{

using UnityEngine;
using UnityEngine.UI;

// A seat's critter: the project's own splat tinted by seat order, with two googly eyes. slot should be square.
internal static class CritterPainter
{
    private const float EyeCentreY = 0.56f;
    private const float EyeRadius = 0.14f;

    // Fits the critter, as large as it can be and square, inside a slot of any shape.
    public static void PaintFitted(RectTransform slot, int seatIndex)
    {
        var critter = Ui.Rect("Critter", slot);
        Ui.Stretch(critter);
        var fit = critter.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1f;
        Paint(critter, seatIndex);
    }

    public static void Paint(RectTransform slot, int seatIndex)
    {
        CellPainter.Splat(slot, SplatPalette.CritterOf(seatIndex), Vector2.zero, Vector2.one, 0f, 0f);
        PaintEye(slot, 0.37f);
        PaintEye(slot, 0.63f);
    }

    // A dark rim, a white ball, and a pupil looking down and to the right.
    private static void PaintEye(RectTransform slot, float centreX)
    {
        var rim = Ui.Image("EyeRim", slot, SplatPalette.Ink, Ui.Circle);
        Ui.Anchored(rim.rectTransform, new Vector2(centreX - EyeRadius, EyeCentreY - EyeRadius), new Vector2(centreX + EyeRadius, EyeCentreY + EyeRadius), Vector2.zero, Vector2.zero);
        var ball = Ui.Image("Eye", rim.transform, Color.white, Ui.Circle);
        Ui.Anchored(ball.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
        var pupil = Ui.Image("Pupil", ball.transform, SplatPalette.Ink, Ui.Circle);
        Ui.Anchored(pupil.rectTransform, new Vector2(0.38f, 0.12f), new Vector2(0.88f, 0.62f), Vector2.zero, Vector2.zero);
    }
}
}
