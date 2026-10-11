namespace MissionSplat.Player
{

using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

internal static class MissionCardPainter
{
    // Playing-card proportions, width over height.
    private const float Ratio = 5f / 7f;

    // The splats sit on a 4 x 3 lattice, so every pattern uses the same splat size.
    private const int LatticeWidth = 4;
    private const int LatticeHeight = 3;

    // Draws the mission as a portrait card centred in slot, whatever shape slot is, and sized by it.
    public static void Paint(RectTransform slot, Mission mission)
    {
        var card = Ui.Rect("Card", slot);
        Ui.Stretch(card);
        var fit = card.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = Ratio;
        TilePainter.PaintCard(card, Ui.MissionCard, 0.03f, 0.015f, Ratio);

        // The lattice fits its own ratio inside the upper part of the face, so the splats stay round at any card size.
        var area = Ui.Rect("Pattern", card);
        Ui.Anchored(area, new Vector2(0.06f, 0.33f), new Vector2(0.94f, 0.93f), Vector2.zero, Vector2.zero);
        var lattice = Ui.Rect("Lattice", area);
        Ui.Stretch(lattice);
        var latticeFit = lattice.gameObject.AddComponent<AspectRatioFitter>();
        latticeFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        latticeFit.aspectRatio = LatticeWidth / (float)LatticeHeight;
        var color = SplatPalette.Of(mission.Color);
        var cells = Cells(mission.Pattern);
        for (var i = 0; i < cells.Length; i++)
        {
            var splat = Ui.Rect("Splat" + i, lattice);
            var (x, y) = cells[i];
            Ui.Anchored(splat, new Vector2(x / LatticeWidth, y / LatticeHeight), new Vector2((x + 1f) / LatticeWidth, (y + 1f) / LatticeHeight), Vector2.zero, Vector2.zero);
            // One turn per splat, keyed by the mission, so the card looks the same wherever it is drawn.
            CellPainter.Splat(splat, color, Vector2.zero, Vector2.one, TilePainter.SplatAngle(mission.Id.Value, i, 0), -0.1f);
        }

        var caption = Ui.Label("Caption", card, 16, SplatPalette.Ink, TextAnchor.MiddleCenter);
        Ui.Anchored(caption.rectTransform, new Vector2(0.12f, 0.05f), new Vector2(0.88f, 0.29f), Vector2.zero, Vector2.zero);
        // Best fit needs the text to wrap and truncate, so it scales to the card instead of overflowing it.
        caption.horizontalOverflow = HorizontalWrapMode.Wrap;
        caption.verticalOverflow = VerticalWrapMode.Truncate;
        caption.resizeTextForBestFit = true;
        caption.resizeTextMinSize = 6;
        caption.resizeTextMaxSize = 60;
        caption.text = PatternName(mission.Pattern);
    }

    // Lattice cells (x right, y up) of each pattern, centred on the 4 x 3 lattice.
    private static (float X, float Y)[] Cells(MissionPattern pattern) =>
        pattern switch
        {
            MissionPattern.Row => new[] { (0f, 1f), (1f, 1f), (2f, 1f), (3f, 1f) },
            MissionPattern.Square => new[] { (1f, 0.5f), (2f, 0.5f), (1f, 1.5f), (2f, 1.5f) },
            _ => new[] { (1f, 2f), (1f, 1f), (1f, 0f), (2f, 0f) },
        };

    private static string PatternName(MissionPattern pattern) =>
        pattern switch
        {
            MissionPattern.Row => "row",
            MissionPattern.Square => "square",
            _ => "L",
        };
}
}
