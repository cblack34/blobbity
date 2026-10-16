namespace MissionSplat.Player
{

using System;
using System.Collections.Generic;
using MissionSplat.App;
using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

// The picture and the pace of each kind of recap item. The board under the stage already shows where the table ended up,
// so every animation is drawn over it and ends looking like it.
internal sealed class RecapAnimations
{
    private const float QuickSeconds = 0.5f;
    private const float BeatSeconds = 1f;

    // A placement spends this much of its time dropping; the rest is the pulse.
    private const float DropShare = 0.7f;

    // The claim card pops up, holds, then flies; these are the fractions of its second where each part ends.
    private const float PopShare = 0.25f;
    private const float HoldShare = 0.65f;

    private const float ClaimCardWidthUnits = 460f;
    private const float ClaimCardHeightUnits = ClaimCardWidthUnits * 7f / 5f;
    private const float ClaimRestHeightUnits = 60f;
    private const float DimAlpha = 0.6f;
    private const float BannerWidthUnits = 880f;
    private const float BannerHeightUnits = 200f;

    private readonly RecapStage _stage;

    public RecapAnimations(RecapStage stage)
    {
        _stage = stage;
    }

    // The picture for snapshot.Recap[index], which the stage must already be open for.
    public RecapStep Begin(TableSnapshot snapshot, int index)
    {
        var item = snapshot.Recap[index];
        return item switch
        {
            RecapPlaced placed => Placed(placed, snapshot),
            RecapRotated rotated => Rotated(rotated, snapshot),
            RecapBounced bounced => Bounced(bounced),
            RecapClaimed claimed => Claimed(claimed),
            RecapWon won => Banner(Seats.NameOf(won.Seat) + " wins!", BeatSeconds),
            RecapReshuffled => Banner("Board reshuffled", QuickSeconds),
            _ => throw new ArgumentException("Unknown recap item " + item.GetType().Name + ".", nameof(snapshot)),
        };
    }

    // The tile drops in from a little above, bigger and fading in, then a ring in the seat's colour pulses out from it.
    private RecapStep Placed(RecapPlaced item, TableSnapshot snapshot)
    {
        var block = _stage.TileBlock("RecapPlaced", item.TileX, item.TileY);
        if (block == null)
        {
            return RecapStep.Idle(QuickSeconds);
        }

        PaintTile(block, item.Tile.Value, item.Cells, item.TileX, item.TileY);
        var group = block.gameObject.AddComponent<CanvasGroup>();
        var ring = PaintRing(block, SplatPalette.CritterOf(Seats.IndexOf(snapshot.View, item.Seat)));
        var rest = block.anchoredPosition;
        var side = block.sizeDelta.x;
        return new RecapStep(QuickSeconds, progress =>
        {
            var fall = Mathf.Clamp01(progress / DropShare);
            var landing = fall * fall;
            block.anchoredPosition = rest + new Vector2(0f, (1f - landing) * side * 0.8f);
            block.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, landing);
            group.alpha = Mathf.Clamp01(fall * 3f);
            var pulse = Mathf.Clamp01((progress - DropShare) / (1f - DropShare));
            ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.4f, pulse);
            ring.color = WithAlpha(ring.color, progress < DropShare || progress >= 1f ? 0f : 1f - pulse);
        });
    }

    // The tile, drawn as it ends up, turns clockwise by the item's quarter-turns to rest.
    private RecapStep Rotated(RecapRotated item, TableSnapshot snapshot)
    {
        var block = _stage.TileBlock("RecapRotated", item.TileX, item.TileY);
        if (block == null)
        {
            return RecapStep.Idle(QuickSeconds);
        }

        PaintTile(block, TileIdAt(snapshot, item.TileX, item.TileY), item.Cells, item.TileX, item.TileY);
        var degrees = item.QuarterTurnsClockwise * 90f;
        return new RecapStep(QuickSeconds, progress =>
        {
            block.localRotation = Quaternion.Euler(0f, 0f, degrees * (1f - Smooth(progress)));
            block.localScale = Vector3.one * (1f + (0.12f * Mathf.Sin(Mathf.PI * progress)));
        });
    }

    // What the bounce uncovered shows beneath while the lifted tile rises and fades.
    private RecapStep Bounced(RecapBounced item)
    {
        if (item.RevealedCells.Count > 0)
        {
            var revealed = _stage.TileBlock("RecapRevealed", item.TileX, item.TileY);
            if (revealed != null)
            {
                PaintTile(revealed, string.Empty, item.RevealedCells, item.TileX, item.TileY);
            }
        }

        var block = _stage.TileBlock("RecapBounced", item.TileX, item.TileY);
        if (block == null)
        {
            return RecapStep.Idle(QuickSeconds);
        }

        PaintTile(block, string.Empty, item.RemovedCells, item.TileX, item.TileY);
        var group = block.gameObject.AddComponent<CanvasGroup>();
        var rest = block.anchoredPosition;
        var side = block.sizeDelta.x;
        return new RecapStep(QuickSeconds, progress =>
        {
            var lift = 1f - ((1f - progress) * (1f - progress));
            block.anchoredPosition = rest + new Vector2(0f, lift * side * 0.9f);
            block.localScale = Vector3.one * Mathf.Lerp(1f, 1.3f, lift);
            group.alpha = 1f - lift;
        });
    }

    // The mission pops up large at the middle of the table, then shrinks and flies into the claiming seat's score pip.
    private RecapStep Claimed(RecapClaimed item)
    {
        var screen = _stage.ScreenLayer;
        var dim = Ui.Image("RecapDim", screen, SplatPalette.Overlay);
        Ui.Stretch(dim.rectTransform);
        var card = Ui.Centered("RecapClaim", screen, new Vector2(ClaimCardWidthUnits, ClaimCardHeightUnits));
        var cardGroup = card.gameObject.AddComponent<CanvasGroup>();
        BlobbityCardPainter.Paint(card, item.Card);

        var rest = new Vector2(0f, ClaimRestHeightUnits);
        var target = _stage.PipPosition(item.Seat, item.Card.Id) ?? rest;
        return new RecapStep(BeatSeconds, progress =>
        {
            var pop = Mathf.Clamp01(progress / PopShare);
            var fly = Smooth(Mathf.Clamp01((progress - HoldShare) / (1f - HoldShare)));
            card.anchoredPosition = Vector2.Lerp(rest, target, fly);
            card.localScale = Vector3.one * Mathf.Lerp(Mathf.Lerp(0.4f, 1f, OutBack(pop)), 0.06f, fly);
            cardGroup.alpha = Mathf.Clamp01(pop * 4f);
            dim.color = WithAlpha(dim.color, DimAlpha * pop * (1f - fly));
        });
    }

    // A banner pops in over the middle of the table and fades out at the end.
    private RecapStep Banner(string text, float seconds)
    {
        var box = Ui.Toy("RecapBanner", _stage.ScreenLayer, SplatPalette.Sunny);
        var rect = box.rectTransform;
        Ui.Center(rect, new Vector2(BannerWidthUnits, BannerHeightUnits));
        var group = box.gameObject.AddComponent<CanvasGroup>();
        var label = Ui.Label("Message", Ui.ToyFace(box), 72, SplatPalette.Ink, TextAnchor.MiddleCenter, FontWeight.ExtraBold);
        Ui.Stretch(label.rectTransform);
        label.text = text;
        return new RecapStep(seconds, progress =>
        {
            rect.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, OutBack(Mathf.Clamp01(progress * seconds / 0.3f)));
            group.alpha = Mathf.Clamp01((1f - progress) * seconds / 0.2f);
        });
    }

    // The tile the cells make, drawn as the board draws it. Rotate and bounce carry no tile id, so the caller's stand-in only seeds the splat angles.
    private static void PaintTile(RectTransform block, string tileId, IReadOnlyList<OccupiedCell> cells, int tileX, int tileY)
    {
        TilePainter.Paint(block, tileId, (x, y) => CellAt(cells, (tileX * 2) + x, (tileY * 2) + y), 0);
    }

    private static Cell CellAt(IReadOnlyList<OccupiedCell> cells, int cellX, int cellY)
    {
        foreach (var cell in cells)
        {
            if (cell.CellX == cellX && cell.CellY == cellY)
            {
                return cell.Value;
            }
        }

        return Cell.Symbol(OrdinaryCatalog.Blank);
    }

    private static Image PaintRing(RectTransform block, Color color)
    {
        var ring = Ui.Image("RecapPulse", block, WithAlpha(color, 0f), Ui.TileRing);
        Ui.Stretch(ring.rectTransform);
        return ring;
    }

    // A rotate item has no tile id, so the board's own tile there (if still there) lends its id, which only seeds the splat angles.
    private static string TileIdAt(TableSnapshot snapshot, int tileX, int tileY)
    {
        foreach (var tile in snapshot.View.Board.Tiles)
        {
            if (tile.TileX == tileX && tile.TileY == tileY)
            {
                return tile.Id.Value;
            }
        }

        return string.Empty;
    }

    private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

    private static float Smooth(float t) => t * t * (3f - (2f * t));

    // Overshoots a little before settling at 1.
    private static float OutBack(float t)
    {
        const float overshoot = 1.70158f;
        var u = t - 1f;
        return 1f + (u * u * (((overshoot + 1f) * u) + overshoot));
    }
}
}
