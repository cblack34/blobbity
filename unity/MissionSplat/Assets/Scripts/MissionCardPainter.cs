namespace MissionSplat.Player
{

using System;
using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

internal static class MissionCardPainter
{
    // Fractions of the card's width: the border around the face, and the edge line.
    private const float CardInset = 0.03f;
    private const float CardEdge = 0.015f;

    // The splats sit on a 4 x 3 lattice, so every pattern uses the same splat size.
    private const int LatticeWidth = 4;
    private const int LatticeHeight = 3;

    // Splats overfill their cell by a tenth on each side, because their transparent spikes make them look small otherwise.
    private const float SplatOverfill = -0.1f;

    // Each pattern's caption and its lattice cells (x right, y up), centred on the 4 x 3 lattice.
    private static (string Caption, (float X, float Y)[] Cells) Pattern(MissionPattern pattern) =>
        pattern switch
        {
            MissionPattern.Row => ("Row", new[] { (0f, 1f), (1f, 1f), (2f, 1f), (3f, 1f) }),
            MissionPattern.Square => ("Square", new[] { (1f, 0.5f), (2f, 0.5f), (1f, 1.5f), (2f, 1.5f) }),
            MissionPattern.L => ("L", new[] { (1f, 2f), (1f, 1f), (1f, 0f), (2f, 0f) }),
            _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown mission pattern."),
        };

    // Draws the mission as a portrait card centred in slot, whatever shape slot is, and sized by it.
    public static void Paint(RectTransform slot, Mission mission)
    {
        var card = Ui.Rect("Card", slot);
        // The fitter resizes the card at layout time; stretching first gives it a sane rect before then.
        Ui.Stretch(card);
        var fit = card.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = Ui.MissionCard.rect.width / Ui.MissionCard.rect.height;
        CardPainter.Paint(card, Ui.MissionCard, CardInset, CardEdge);
        PaintPattern(card, mission);
        PaintCaption(card, mission);
    }

    private static void PaintPattern(RectTransform card, Mission mission)
    {
        // The lattice fits its own ratio inside the upper part of the face, so the splats stay round at any card size.
        var area = Ui.Rect("Pattern", card);
        Ui.Anchored(area, new Vector2(0.06f, 0.33f), new Vector2(0.94f, 0.93f), Vector2.zero, Vector2.zero);
        var lattice = Ui.Rect("Lattice", area);
        Ui.Stretch(lattice);
        var latticeFit = lattice.gameObject.AddComponent<AspectRatioFitter>();
        latticeFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        latticeFit.aspectRatio = LatticeWidth / (float)LatticeHeight;
        var color = SplatPalette.Of(mission.Color);
        var cells = Pattern(mission.Pattern).Cells;
        for (var i = 0; i < cells.Length; i++)
        {
            var splat = Ui.Rect("Splat" + i, lattice);
            var (x, y) = cells[i];
            Ui.Anchored(splat, new Vector2(x / LatticeWidth, y / LatticeHeight), new Vector2((x + 1f) / LatticeWidth, (y + 1f) / LatticeHeight), Vector2.zero, Vector2.zero);
            // One turn per splat, keyed by the mission, so the card looks the same wherever it is drawn.
            CellPainter.Splat(splat, color, Vector2.zero, Vector2.one, CellPainter.SplatAngle(mission.Id.Value + ":" + i), SplatOverfill);
        }
    }

    private static void PaintCaption(RectTransform card, Mission mission)
    {
        var caption = Ui.Label("Caption", card, 16, SplatPalette.Ink, TextAnchor.MiddleCenter);
        Ui.Anchored(caption.rectTransform, new Vector2(0.12f, 0.05f), new Vector2(0.88f, 0.29f), Vector2.zero, Vector2.zero);
        // Best fit needs the text to wrap and truncate, so it scales to the card instead of overflowing it.
        caption.horizontalOverflow = HorizontalWrapMode.Wrap;
        caption.verticalOverflow = VerticalWrapMode.Truncate;
        caption.resizeTextForBestFit = true;
        caption.resizeTextMinSize = 6;
        caption.resizeTextMaxSize = 60;
        caption.text = Pattern(mission.Pattern).Caption;
    }
}
}
