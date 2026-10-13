namespace MissionSplat.Player
{

using System;
using MissionSplat.App;
using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

// One entry per seat across the top: critter, name, and a score pip per claim needed to win. The current seat sits in a highlighted box.
internal sealed class SeatBar
{
    private const float CritterUnits = 108f;
    private const float PipUnits = 25f;
    private const float PipStepUnits = 30f;
    private const float EntryInsetUnits = 8f;
    private const float OutlineUnits = 6f;

    // Where the name and pips start: past the critter and a gap.
    private const float TextLeftUnits = 12f + CritterUnits + 8f;

    private readonly RectTransform _bar;

    public SeatBar(RectTransform bar)
    {
        _bar = bar;
    }

    // Raised with the seat's position in turn order and its claims when its entry is tapped.
    public event Action<int, SeatClaims> SeatTapped;

    public void Paint(SeatView view)
    {
        Ui.Clear(_bar);
        var seats = view.Claims.Count;
        for (var i = 0; i < seats; i++)
        {
            PaintEntry(i, seats, view.Claims[i], view.Claims[i].Seat.Equals(view.CurrentSeat));
        }
    }

    public static string NameOf(SeatId seat) => "Seat " + seat.Value;

    private void PaintEntry(int index, int seats, SeatClaims claims, bool isCurrent)
    {
        var box = Ui.Button("Seat" + claims.Seat.Value, _bar, string.Empty, isCurrent ? SplatPalette.Ink : Color.clear, Ui.Rounded);
        Ui.Anchored(box.GetComponent<RectTransform>(), new Vector2(index / (float)seats, 0f), new Vector2((index + 1) / (float)seats, 1f), new Vector2(EntryInsetUnits, EntryInsetUnits), new Vector2(-EntryInsetUnits, -EntryInsetUnits));
        box.onClick.AddListener(() => SeatTapped?.Invoke(index, claims));
        if (isCurrent)
        {
            var fill = Ui.Image("Fill", box.transform, Color.white, Ui.Rounded);
            Ui.Anchored(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(OutlineUnits, OutlineUnits), new Vector2(-OutlineUnits, -OutlineUnits));
        }

        var critter = Ui.Rect("Critter", box.transform);
        critter.anchorMin = new Vector2(0f, 0.5f);
        critter.anchorMax = new Vector2(0f, 0.5f);
        critter.sizeDelta = new Vector2(CritterUnits, CritterUnits);
        critter.anchoredPosition = new Vector2(12f + (CritterUnits / 2f), 0f);
        CritterPainter.Paint(critter, index);

        var name = Ui.Label("Name", box.transform, 38, SplatPalette.Ink, TextAnchor.MiddleLeft);
        name.fontStyle = FontStyle.Bold;
        Ui.Anchored(name.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(TextLeftUnits, 0f), Vector2.zero);
        name.text = NameOf(claims.Seat);

        // A win needs OrdinaryCatalog.ClaimsRequiredToWin claims; a placement that crosses it can leave one more, so the row grows to hold them.
        var pips = Math.Max(OrdinaryCatalog.ClaimsRequiredToWin, claims.Missions.Count);
        for (var p = 0; p < pips; p++)
        {
            PaintPip(box.transform, p, p < claims.Missions.Count, index);
        }
    }

    private static void PaintPip(Transform entry, int position, bool claimed, int seatIndex)
    {
        var pip = Ui.Rect(claimed ? "PipFilled" : "PipEmpty", entry);
        pip.anchorMin = new Vector2(0f, 0.3f);
        pip.anchorMax = new Vector2(0f, 0.3f);
        pip.sizeDelta = new Vector2(PipUnits, PipUnits);
        pip.anchoredPosition = new Vector2(TextLeftUnits + (PipUnits / 2f) + (position * PipStepUnits), 0f);
        if (claimed)
        {
            CellPainter.Splat(pip, SplatPalette.CritterOf(seatIndex), Vector2.zero, Vector2.one, CellPainter.SplatAngle("pip" + position), -0.3f);
            return;
        }

        var rim = Ui.Image("Rim", pip, SplatPalette.Ink, Ui.Circle);
        Ui.Stretch(rim.rectTransform);
        var hollow = Ui.Image("Hollow", rim.transform, Color.white, Ui.Circle);
        Ui.Anchored(hollow.rectTransform, new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f), Vector2.zero, Vector2.zero);
    }
}
}
