namespace MissionSplat.Player
{

using System;
using Blobbity.Rules;
using UnityEngine;
using UnityEngine.UI;

internal static class BlobbityCardPainter
{
    // The edge line, as a fraction of the card's width.
    private const float CardEdge = 0.03f;

    // The splats sit on a 2 x 4 portrait lattice, so every pattern uses the same splat size.
    private const int LatticeWidth = 2;
    private const int LatticeHeight = 4;

    // Splats overfill their cell by a twentieth on each side, because their transparent spikes make them look small otherwise.
    private const float SplatOverfill = -0.05f;

    // Each pattern's lattice cells (x right, y up), centred on the lattice; a row runs diagonally, bottom-left to top-right, which gives it the most room on the portrait card.
    private static (float X, float Y)[] Pattern(BlobbityPattern pattern) =>
        pattern switch
        {
            BlobbityPattern.Row => new[] { (0f, 0f), (1f / 3f, 1f), (2f / 3f, 2f), (1f, 3f) },
            BlobbityPattern.Square => new[] { (0f, 1f), (1f, 1f), (0f, 2f), (1f, 2f) },
            BlobbityPattern.L => new[] { (0f, 2.5f), (0f, 1.5f), (0f, 0.5f), (1f, 0.5f) },
            _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown blobbity pattern."),
        };

    // Draws the blobbity as a portrait card centred in slot, whatever shape slot is, and sized by it.
    public static void Paint(RectTransform slot, BlobbityCard blobbity)
    {
        var card = Ui.Rect("Card", slot);
        // The fitter resizes the card at layout time; stretching first gives it a sane rect before then.
        Ui.Stretch(card);
        var fit = card.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = Ui.BlobbityCard.rect.width / Ui.BlobbityCard.rect.height;
        CardPainter.Paint(card, Ui.BlobbityCard, CardEdge, FittedWidth(slot));
        PaintPattern(card, blobbity);
    }

    // The width the fitter will give the card inside slot, which sets how thick its edge line must be to stay visible.
    // The slot was made a moment ago, so it is laid out first to give it its size; its parents must be active for that.
    private static float FittedWidth(RectTransform slot)
    {
        Canvas.ForceUpdateCanvases();
        var ratio = Ui.BlobbityCard.rect.width / Ui.BlobbityCard.rect.height;
        var width = Mathf.Min(slot.rect.width, slot.rect.height * ratio);
        return width > 1f ? width : 100f;
    }

    private static void PaintPattern(RectTransform card, BlobbityCard blobbity)
    {
        // The lattice fits its own ratio inside the card less a margin, so the splats stay round at any card size and clear of the edge.
        var area = Ui.Rect("Pattern", card);
        Ui.Anchored(area, new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.9f), Vector2.zero, Vector2.zero);
        var lattice = Ui.Rect("Lattice", area);
        Ui.Stretch(lattice);
        var latticeFit = lattice.gameObject.AddComponent<AspectRatioFitter>();
        latticeFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        latticeFit.aspectRatio = LatticeWidth / (float)LatticeHeight;
        var cells = Pattern(blobbity.Pattern);
        for (var i = 0; i < cells.Length; i++)
        {
            var splat = Ui.Rect("Splat" + i, lattice);
            var (x, y) = cells[i];
            Ui.Anchored(splat, new Vector2(x / LatticeWidth, y / LatticeHeight), new Vector2((x + 1f) / LatticeWidth, (y + 1f) / LatticeHeight), Vector2.zero, Vector2.zero);
            // One turn per splat, keyed by the blobbity, so the card looks the same wherever it is drawn.
            CellPainter.ColourSplat(splat, blobbity.Color, Vector2.zero, Vector2.one, CellPainter.SplatAngle(blobbity.Id.Value + ":" + i), SplatOverfill);
        }
    }
}
}
