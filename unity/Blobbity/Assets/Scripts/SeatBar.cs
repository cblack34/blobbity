namespace Blobbity.Player
{

using System;
using System.Collections.Generic;
using Blobbity.App;
using Blobbity.Rules;
using UnityEngine;
using UnityEngine.UI;

// One entry per seat across the top: critter, name, and a score pip per claim needed to win. One seat sits in a highlighted box: the current seat, or the winner once the game is won.
// Each entry is only as wide as its content and sits at the middle of an equal share of the bar, so two and three seats are spaced out evenly.
internal sealed class SeatBar
{
    private const float CritterUnits = 108f;
    private const float CritterMarginUnits = 12f;
    private const float PipUnits = 25f;
    private const float PipStepUnits = 30f;
    private const float EntryInsetUnits = 8f;
    private const float OutlineUnits = 7f;

    // The widest an entry gets: a critter, a name, and a row of four pips. More seats share the bar, so their entries are narrower.
    private const float MaxEntryUnits = 280f;

    // Where the name and pips start: past the critter and a gap.
    private const float TextLeftUnits = CritterMarginUnits + CritterUnits + 8f;

    private readonly RectTransform _bar;
    private readonly Dictionary<SeatId, List<(BlobbityId Id, RectTransform Pip)>> _pips = new Dictionary<SeatId, List<(BlobbityId, RectTransform)>>();

    public SeatBar(RectTransform bar)
    {
        _bar = bar;
    }

    // Raised with the seat's position in turn order and its claims when its entry is tapped.
    public event Action<int, SeatClaims> SeatTapped;

    // highlighted is null when no seat is, as when the game ended without a winner.
    public void Paint(SeatView view, SeatId? highlighted)
    {
        Ui.Clear(_bar);
        _pips.Clear();
        var seats = view.Claims.Count;
        // Before the first layout the bar has no width; the reference width stands in.
        var share = (_bar.rect.width > 1f ? _bar.rect.width : Ui.ReferenceWidthUnits) / seats;
        var entryWidth = Math.Min(MaxEntryUnits, share - (2f * EntryInsetUnits));
        for (var i = 0; i < seats; i++)
        {
            PaintEntry(i, seats, entryWidth, view.Claims[i], view.Claims[i].Seat.Equals(highlighted));
        }
    }

    // The pip a claimed blobbity fills in the seat's entry, as last painted; null when it is not drawn.
    public RectTransform PipOf(SeatId seat, BlobbityId id)
    {
        if (_pips.TryGetValue(seat, out var pips))
        {
            foreach (var (claimed, pip) in pips)
            {
                if (claimed.Equals(id) && pip != null)
                {
                    return pip;
                }
            }
        }

        return null;
    }

    private void PaintEntry(int index, int seats, float entryWidth, SeatClaims claims, bool isCurrent)
    {
        var box = Ui.Outlined("Seat" + claims.Seat.Value, _bar, isCurrent ? SplatPalette.Ink : Color.clear, isCurrent ? SplatPalette.Paper : Color.clear, OutlineUnits);
        var button = Ui.Tappable(box);
        var centre = (index + 0.5f) / seats;
        Ui.Anchored(box.rectTransform, new Vector2(centre, 0f), new Vector2(centre, 1f), new Vector2(-entryWidth / 2f, EntryInsetUnits), new Vector2(entryWidth / 2f, -EntryInsetUnits));
        button.onClick.AddListener(() => SeatTapped?.Invoke(index, claims));

        var critter = Ui.Rect("Critter", box.transform);
        critter.anchorMin = new Vector2(0f, 0.5f);
        critter.anchorMax = new Vector2(0f, 0.5f);
        critter.sizeDelta = new Vector2(CritterUnits, CritterUnits);
        critter.anchoredPosition = new Vector2(CritterMarginUnits + (CritterUnits / 2f), 0f);
        CritterPainter.Paint(critter, index);

        var name = Ui.Label("Name", box.transform, 38, SplatPalette.Ink, TextAnchor.MiddleLeft, FontWeight.SemiBold);
        Ui.Anchored(name.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(TextLeftUnits, 0f), Vector2.zero);
        name.text = Seats.NameOf(claims.Seat);

        // A win needs Seats.ClaimsToWin claims; a placement that crosses it can leave more, so the row grows to hold them.
        // The pips share what is left of the entry, so a longer row shrinks them instead of spilling out of the entry.
        var pips = Math.Max(Seats.ClaimsToWin, claims.Blobbities.Count);
        var step = Math.Min(PipStepUnits, Math.Max(1f, (entryWidth - TextLeftUnits - CritterMarginUnits) / pips));
        var filled = new List<(BlobbityId, RectTransform)>();
        _pips[claims.Seat] = filled;
        for (var p = 0; p < pips; p++)
        {
            var claimed = p < claims.Blobbities.Count;
            var pip = PaintPip(box.transform, TextLeftUnits + ((p + 0.5f) * step), Math.Min(PipUnits, step), claimed, index, p);
            if (claimed)
            {
                filled.Add((claims.Blobbities[p].Id, pip));
            }
        }
    }

    private static RectTransform PaintPip(Transform entry, float centreX, float size, bool claimed, int seatIndex, int position)
    {
        var pip = Ui.Rect(claimed ? "PipFilled" : "PipEmpty", entry);
        pip.anchorMin = new Vector2(0f, 0.3f);
        pip.anchorMax = new Vector2(0f, 0.3f);
        pip.sizeDelta = new Vector2(size, size);
        pip.anchoredPosition = new Vector2(centreX, 0f);
        if (claimed)
        {
            CellPainter.Splat(pip, SplatPalette.CritterOf(seatIndex), Vector2.zero, Vector2.one, CellPainter.SplatAngle("pip" + position), -0.1f);
            return pip;
        }

        var rim = Ui.Image("Rim", pip, SplatPalette.Ink, Ui.Circle);
        Ui.Stretch(rim.rectTransform);
        var hollow = Ui.Image("Hollow", rim.transform, SplatPalette.Paper, Ui.Circle);
        Ui.Anchored(hollow.rectTransform, new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f), Vector2.zero, Vector2.zero);
        return pip;
    }
}
}
