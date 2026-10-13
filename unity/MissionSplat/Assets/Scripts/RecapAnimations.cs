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
    // Below the card, because the card flies up and to a pip and would cross a label above it.
    private const float ClaimLabelHeightUnits = -450f;
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
            RecapClaimed claimed => Claimed(claimed, snapshot, index),
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
            return Idle(QuickSeconds);
        }

        RecapStage.PaintTile(block, item.Tile.Value, item.Cells, item.TileX, item.TileY);
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
            return Idle(QuickSeconds);
        }

        RecapStage.PaintTile(block, TileIdAt(snapshot, item.TileX, item.TileY), item.Cells, item.TileX, item.TileY);
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
                RecapStage.PaintTile(revealed, string.Empty, item.RevealedCells, item.TileX, item.TileY);
            }
        }

        var block = _stage.TileBlock("RecapBounced", item.TileX, item.TileY);
        if (block == null)
        {
            return Idle(QuickSeconds);
        }

        RecapStage.PaintTile(block, string.Empty, item.RemovedCells, item.TileX, item.TileY);
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
    private RecapStep Claimed(RecapClaimed item, TableSnapshot snapshot, int index)
    {
        var screen = _stage.ScreenLayer;
        var dim = Ui.Image("RecapDim", screen, SplatPalette.Overlay);
        Ui.Stretch(dim.rectTransform);
        var card = CenteredBox("RecapClaim", screen, new Vector2(ClaimCardWidthUnits, ClaimCardHeightUnits));
        var cardGroup = card.gameObject.AddComponent<CanvasGroup>();
        MissionCardPainter.Paint(card, item.Mission);
        var label = Ui.Label("RecapClaimLabel", screen, 84, SplatPalette.Paper, TextAnchor.MiddleCenter, FontWeight.ExtraBold);
        label.rectTransform.anchoredPosition = new Vector2(0f, ClaimLabelHeightUnits);
        label.text = Seats.NameOf(item.Seat) + " claims!";
        // White on the dimmed table reads weakly, so the label gets the ink outline the rest of the art has.
        var outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = SplatPalette.Ink;
        outline.effectDistance = new Vector2(5f, -5f);
        var labelGroup = label.gameObject.AddComponent<CanvasGroup>();

        var rest = new Vector2(0f, ClaimRestHeightUnits);
        var target = _stage.PipPosition(item.Seat, PipOrdinal(item, snapshot, index)) ?? rest;
        return new RecapStep(BeatSeconds, progress =>
        {
            var pop = Mathf.Clamp01(progress / PopShare);
            var fly = Smooth(Mathf.Clamp01((progress - HoldShare) / (1f - HoldShare)));
            card.anchoredPosition = Vector2.Lerp(rest, target, fly);
            card.localScale = Vector3.one * Mathf.Lerp(Mathf.Lerp(0.4f, 1f, OutBack(pop)), 0.06f, fly);
            cardGroup.alpha = Mathf.Clamp01(pop * 4f);
            labelGroup.alpha = Mathf.Clamp01(pop * 4f) * (1f - Mathf.Clamp01(fly * 1.3f));
            dim.color = WithAlpha(dim.color, DimAlpha * pop * (1f - fly));
        });
    }

    // A banner pops in over the middle of the table and fades out at the end.
    private RecapStep Banner(string text, float seconds)
    {
        var box = Ui.Toy("RecapBanner", _stage.ScreenLayer, SplatPalette.Sunny);
        var rect = box.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(BannerWidthUnits, BannerHeightUnits);
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

    private static RecapStep Idle(float seconds) => new RecapStep(seconds, _ => { });

    private static RectTransform CenteredBox(string name, RectTransform parent, Vector2 size)
    {
        var box = Ui.Rect(name, parent);
        box.anchorMin = new Vector2(0.5f, 0.5f);
        box.anchorMax = new Vector2(0.5f, 0.5f);
        box.sizeDelta = size;
        return box;
    }

    private static Image PaintRing(RectTransform block, Color color)
    {
        var ring = Ui.Image("RecapPulse", block, WithAlpha(color, 0f), Ui.TileRing);
        Ui.Stretch(ring.rectTransform);
        return ring;
    }

    // Which of the seat's pips this claim fills: the snapshot holds every claim made, so count back past the seat's later claims in this recap.
    private static int PipOrdinal(RecapClaimed item, TableSnapshot snapshot, int index)
    {
        var later = 0;
        for (var i = index + 1; i < snapshot.Recap.Count; i++)
        {
            if (snapshot.Recap[i] is RecapClaimed other && other.Seat.Equals(item.Seat))
            {
                later++;
            }
        }

        return Math.Max(0, snapshot.View.Claims[Seats.IndexOf(snapshot.View, item.Seat)].Missions.Count - 1 - later);
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
