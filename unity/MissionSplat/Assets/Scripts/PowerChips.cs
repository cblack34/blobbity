namespace MissionSplat.Player
{

using System;
using System.Linq;
using MissionSplat.App;
using MissionSplat.Rules;
using UnityEngine;

// The row of power chips in the turn tray: a badge for each power the drawn tile still has a use of, with "Place instead" while one is selected.
internal sealed class PowerChips
{
    private const float ChipUnits = 100f;
    private const float ChipGapUnits = 28f;
    private const float SelectedRingUnits = 9f;
    private const float PlaceInsteadUnits = 300f;

    private readonly RectTransform _row;

    public PowerChips(RectTransform row)
    {
        _row = row;
    }

    // Raised with the power to select, or null to go back to placing.
    public event Action<SymbolId?> PowerSelected;

    // A chip for each power the drawn tile still has a use of, left to right; one used up leaves no chip.
    public void Paint(TableSnapshot snapshot)
    {
        Ui.Clear(_row);
        var x = 0f;
        foreach (var charge in snapshot.View.RemainingUses)
        {
            if (charge.Remaining <= 0)
            {
                continue;
            }

            var selected = snapshot.SelectedPower is { } current && current.Equals(charge.Power);
            // A power with nothing to act on cannot be picked, but a selected one can always be put back.
            var usable = snapshot.CanAct && (selected || HasTargets(snapshot, charge.Power));
            var chip = Ui.Button("Power" + PowerName(charge.Power), _row, string.Empty, PowerBadge.FillOf(charge.Power), Ui.Circle);
            PlaceFromLeft(chip.GetComponent<RectTransform>(), x, ChipUnits, ChipUnits);
            PowerBadge.PaintMark(Ui.ToyFace(chip), charge.Power);
            if (selected)
            {
                PaintSelectedRing(chip.transform);
            }

            if (charge.Remaining > 1)
            {
                PaintUseCount(chip.transform, charge.Remaining);
            }

            Ui.SetEnabled(chip, usable);
            var power = charge.Power;
            chip.onClick.AddListener(() => PowerSelected?.Invoke(selected ? null : power));
            x += ChipUnits + ChipGapUnits;
        }

        if (snapshot.SelectedPower is not null)
        {
            var back = Ui.Button("PlaceInstead", _row, "Place instead", SplatPalette.Paper, Ui.Rounded);
            PlaceFromLeft(back.GetComponent<RectTransform>(), x, PlaceInsteadUnits, ChipUnits - 12f);
            back.onClick.AddListener(() => PowerSelected?.Invoke(null));
        }
    }

    // A thick ring behind the chip marks the power in use; it is the chip's first child, so the chip draws over it.
    private static void PaintSelectedRing(Transform chip)
    {
        var ring = Ui.Image("Selected", chip, SplatPalette.PowerSelected, Ui.Circle);
        Ui.Anchored(ring.rectTransform, Vector2.zero, Vector2.one, new Vector2(-SelectedRingUnits, -SelectedRingUnits), new Vector2(SelectedRingUnits, SelectedRingUnits));
        ring.transform.SetAsFirstSibling();
    }

    private static void PaintUseCount(Transform chip, int remaining)
    {
        var badge = Ui.Outlined("Count", chip, SplatPalette.Ink, SplatPalette.Paper, 3f);
        badge.rectTransform.anchorMin = Vector2.one;
        badge.rectTransform.anchorMax = Vector2.one;
        badge.rectTransform.sizeDelta = new Vector2(56f, 42f);
        badge.rectTransform.anchoredPosition = new Vector2(-4f, -2f);
        var label = Ui.Label("Label", badge.transform, 28, SplatPalette.Ink, TextAnchor.MiddleCenter, FontWeight.ExtraBold);
        Ui.Stretch(label.rectTransform);
        label.text = "×" + remaining;
    }

    // Places rect at x from the left edge of the row, centred vertically.
    private static void PlaceFromLeft(RectTransform rect, float x, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x + (width / 2f), 0f);
    }

    private static bool HasTargets(TableSnapshot snapshot, SymbolId power) =>
        snapshot.LegalTargets.Any(entry => entry.Power.Equals(power) && entry.Targets.Count > 0);

    // The chip's object name ("PowerRotate"), which the play-mode tests look chips up by.
    private static string PowerName(SymbolId power) => char.ToUpperInvariant(power.Value[0]) + power.Value.Substring(1);
}
}
