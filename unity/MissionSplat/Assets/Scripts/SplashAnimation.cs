namespace MissionSplat.Player
{

using MissionSplat.Rules;
using UnityEngine;

// The picture and the pace of a power splash: the badge leaves its cell on the drawn tile, spins out to the middle of the table,
// then shrinks back into the cell, over a soft dim.
internal sealed class SplashAnimation
{
    private const float Seconds = 1f;

    // How much bigger than its cell the badge grows, and how far it turns on the way (a whole number of turns, so it ends upright).
    private const float PeakScale = 3.5f;
    private const float SpinDegrees = 360f;
    private const float DimAlpha = 0.35f;

    // The badge is out for the first half and back for the second.
    private const float OutShare = 0.5f;

    private readonly RecapStage _stage;

    public SplashAnimation(RecapStage stage)
    {
        _stage = stage;
    }

    // What the heading says while it plays.
    public static string Caption(SymbolId power) => "You got a " + power.Value + "!";

    // The picture for one power, which the stage must already be open for.
    public RecapStep Begin(PowerSpot spot)
    {
        var screen = _stage.ScreenLayer;
        var dim = Ui.Image("SplashDim", screen, SplatPalette.Overlay);
        Ui.Stretch(dim.rectTransform);
        var from = _stage.ScreenSpan(spot.Min, spot.Max);
        var badge = Ui.Centered("SplashBadge", screen, new Vector2(from.Size, from.Size));
        PowerBadge.Paint(badge, spot.Power);
        var centre = Vector2.zero;
        return new RecapStep(Seconds, progress =>
        {
            var away = progress < OutShare ? progress / OutShare : (1f - progress) / (1f - OutShare);
            var eased = Mathf.SmoothStep(0f, 1f, away);
            badge.anchoredPosition = Vector2.Lerp(from.Centre, centre, eased);
            badge.localScale = Vector3.one * Mathf.Lerp(1f, PeakScale, eased);
            badge.localRotation = Quaternion.Euler(0f, 0f, -SpinDegrees * Mathf.SmoothStep(0f, 1f, progress));
            dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, DimAlpha * eased);
        });
    }
}
}
