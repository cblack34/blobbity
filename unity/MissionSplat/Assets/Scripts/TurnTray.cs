namespace MissionSplat.Player
{

using System;
using System.Linq;
using MissionSplat.App;
using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

// The bottom of the table: the drawn tile, Spin, the power chips, and the seat's secret missions, with the hand-off cover over the missions.
// When the game is over it shows the winner instead.
internal sealed class TurnTray
{
    public const float HeightUnits = 560f;

    private const float EdgeUnits = 6f;

    private readonly RectTransform _turn;
    private readonly RectTransform _powers;
    private readonly RectTransform _pending;
    private readonly RectTransform _spin;
    private readonly RectTransform _secrets;
    private readonly RectTransform _conceal;
    private readonly Text _concealLabel;
    private readonly RectTransform _winner;

    public TurnTray(RectTransform play)
    {
        var tray = Ui.Image("Tray", play, Color.white);
        Ui.Anchored(tray.rectTransform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, HeightUnits));
        var edge = Ui.Image("Edge", tray.transform, SplatPalette.Ink);
        Ui.Anchored(edge.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -EdgeUnits), Vector2.zero);
        _turn = Ui.Rect("Turn", tray.transform);
        Ui.Stretch(_turn);
        _powers = Area("Powers", _turn, 0.03f, 0.80f, 0.97f, 0.96f);
        _pending = Area("Pending", _turn, 0.03f, 0.04f, 0.37f, 0.76f);
        _spin = Area("Spin", _turn, 0.38f, 0.04f, 0.56f, 0.76f);
        _secrets = Area("Secrets", _turn, 0.58f, 0.04f, 0.97f, 0.76f);
        _winner = Area("Winner", tray.transform, 0.03f, 0.04f, 0.97f, 0.96f);

        // Over the secret missions only; the tile, Spin, and powers beside it stay in view.
        _conceal = Area("Conceal", _turn, 0.58f, 0.04f, 0.97f, 0.76f);
        var concealFill = Ui.Image("Fill", _conceal, SplatPalette.Overlay, Ui.Rounded);
        Ui.Stretch(concealFill.rectTransform);
        concealFill.raycastTarget = true;
        _concealLabel = Ui.Label("Message", _conceal, 30, Color.white, TextAnchor.MiddleCenter);
        _concealLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        Ui.Anchored(_concealLabel.rectTransform, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);
        var confirm = Ui.Button("Confirm", _conceal, "Confirm", SplatPalette.Green, Ui.Rounded);
        Ui.Anchored(confirm.GetComponent<RectTransform>(), new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.36f), Vector2.zero, Vector2.zero);
        confirm.onClick.AddListener(() => Confirmed?.Invoke());
        _conceal.gameObject.SetActive(false);
    }

    public event Action Confirmed;

    public event Action<int> QuarterTurnsSelected;

    public event Action<SymbolId?> PowerSelected;

    // heading names the winner when the game is over.
    public void Paint(TableSnapshot snapshot, string heading)
    {
        var ended = snapshot.Status.Kind is TableStatusKind.Won or TableStatusKind.Ended;
        _turn.gameObject.SetActive(!ended);
        _winner.gameObject.SetActive(ended);
        if (ended)
        {
            PaintWinner(snapshot, heading);
            return;
        }

        // Only rotate repurposes the quarter-turns as its amount, so only then does the pending tile not preview them.
        PaintPending(snapshot.View.PendingMatchTile, snapshot.SelectedPower is { } power && power.Equals(OrdinaryCatalog.Rotate) ? 0 : snapshot.QuarterTurns);
        PaintSpin(snapshot);
        PaintPowers(snapshot);
        PaintSecrets(snapshot);
        _conceal.gameObject.SetActive(snapshot.ConcealVisible);
        if (snapshot.ConcealVisible)
        {
            _concealLabel.text = SeatBar.NameOf(snapshot.View.CurrentSeat) + ", confirm to see your missions.";
        }
    }

    private static RectTransform Area(string name, Transform parent, float minX, float minY, float maxX, float maxY)
    {
        var area = Ui.Rect(name, parent);
        Ui.Anchored(area, new Vector2(minX, minY), new Vector2(maxX, maxY), Vector2.zero, Vector2.zero);
        return area;
    }


    private void PaintWinner(TableSnapshot snapshot, string heading)
    {
        Ui.Clear(_winner);
        var seat = snapshot.Status.Seat;
        if (seat is not { } winner)
        {
            var over = Ui.Label("Message", _winner, 64, SplatPalette.Ink, TextAnchor.MiddleCenter);
            Ui.Stretch(over.rectTransform);
            over.text = heading;
            return;
        }

        var claims = snapshot.View.Claims.Select((row, index) => (row, index)).First(entry => entry.row.Seat.Equals(winner));
        var critterSlot = Ui.Rect("CritterSlot", _winner);
        Ui.Anchored(critterSlot, new Vector2(0f, 0.05f), new Vector2(0.36f, 0.95f), Vector2.zero, Vector2.zero);
        var critter = Ui.Rect("Critter", critterSlot);
        Ui.Stretch(critter);
        var fit = critter.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1f;
        CritterPainter.Paint(critter, claims.index);
        var title = Ui.Label("Message", _winner, 72, SplatPalette.Ink, TextAnchor.LowerLeft);
        title.fontStyle = FontStyle.Bold;
        Ui.Anchored(title.rectTransform, new Vector2(0.40f, 0.5f), new Vector2(1f, 0.9f), Vector2.zero, Vector2.zero);
        title.text = heading;
        var count = Ui.Label("Claims", _winner, 40, SplatPalette.Muted, TextAnchor.UpperLeft);
        Ui.Anchored(count.rectTransform, new Vector2(0.40f, 0.2f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
        var total = claims.row.Missions.Count;
        count.text = total + (total == 1 ? " claim" : " claims");
    }

    private void PaintPending(Tile pending, int quarterTurns)
    {
        Ui.Clear(_pending);
        if (pending is null)
        {
            return;
        }

        PaintTile(_pending, pending, Vector2.zero, Vector2.one, quarterTurns);
    }

    // Each Spin tap turns the drawn tile (or sets rotate's amount) one more clockwise quarter-turn, then wraps from 3 back to 0.
    private void PaintSpin(TableSnapshot snapshot)
    {
        Ui.Clear(_spin);
        var slot = Ui.Rect("Slot", _spin);
        Ui.Anchored(slot, new Vector2(0f, 0.30f), Vector2.one, Vector2.zero, Vector2.zero);
        var button = Ui.Button("Spin", slot, "Spin", SplatPalette.SpinFill, Ui.Circle, 40);
        button.GetComponentInChildren<Text>().color = SplatPalette.Ink;
        var rect = button.GetComponent<RectTransform>();
        Ui.Stretch(rect);
        var fit = button.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1f;
        button.interactable = snapshot.CanAct;
        var next = (snapshot.QuarterTurns + 1) % 4;
        button.onClick.AddListener(() => QuarterTurnsSelected?.Invoke(next));

        var chip = Ui.Image("Turns", _spin, SplatPalette.SpinFill, Ui.Rounded);
        Ui.Anchored(chip.rectTransform, new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.24f), Vector2.zero, Vector2.zero);
        var count = Ui.Label("Count", chip.transform, 34, SplatPalette.Ink, TextAnchor.MiddleCenter);
        Ui.Stretch(count.rectTransform);
        count.text = "\u00d7" + snapshot.QuarterTurns;
    }

    private void PaintSecrets(TableSnapshot snapshot)
    {
        Ui.Clear(_secrets);
        // The hand-off cover sits over this area, and says what is hidden itself.
        if (snapshot.ConcealVisible)
        {
            return;
        }

        var title = Ui.Label("Title", _secrets, 30, SplatPalette.Ink, TextAnchor.MiddleCenter);
        Ui.Anchored(title.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.12f), Vector2.zero, Vector2.zero);
        title.text = snapshot.SecretsVisible ? "Your secret shapes" : "Missions hidden";
        if (!snapshot.SecretsVisible)
        {
            return;
        }

        var missions = snapshot.View.UnclaimedMissions;
        for (var i = 0; i < missions.Count; i++)
        {
            var slot = Ui.Rect("Mission" + i, _secrets);
            var min = new Vector2(0.01f + (i * 0.5f), 0.14f);
            var max = new Vector2(0.49f + (i * 0.5f), 1f);
            Ui.Anchored(slot, min, max, Vector2.zero, Vector2.zero);
            MissionCardPainter.Paint(slot, missions[i]);
        }
    }

    private void PaintPowers(TableSnapshot snapshot)
    {
        Ui.Clear(_powers);
        var charges = snapshot.View.RemainingUses;
        var slots = Math.Max(3, charges.Count + 1);
        for (var i = 0; i < charges.Count; i++)
        {
            var charge = charges[i];
            var selected = snapshot.SelectedPower is { } current && current.Equals(charge.Power);
            var usable = snapshot.CanAct && charge.Remaining > 0;
            var name = PowerName(charge.Power);
            var fill = selected ? SplatPalette.PowerSelected : usable ? SplatPalette.Muted : SplatPalette.Gray;
            var button = Ui.Button("Power" + name, _powers, name + " \u00b7 " + charge.Remaining + " left", fill, Ui.Rounded);
            AnchorSlot(button.GetComponent<RectTransform>(), i, slots);
            button.interactable = usable;
            var power = charge.Power;
            button.onClick.AddListener(() => PowerSelected?.Invoke(selected ? null : power));
        }

        if (snapshot.SelectedPower is not null)
        {
            var back = Ui.Button("PlaceInstead", _powers, "Place instead", SplatPalette.Green, Ui.Rounded);
            AnchorSlot(back.GetComponent<RectTransform>(), charges.Count, slots);
            back.onClick.AddListener(() => PowerSelected?.Invoke(null));
        }
    }

    private static void AnchorSlot(RectTransform rect, int index, int slots)
    {
        var x = index / (float)slots;
        Ui.Anchored(rect, new Vector2(x + 0.01f, 0f), new Vector2(x + (1f / slots) - 0.01f, 1f), Vector2.zero, Vector2.zero);
    }

    private static string PowerName(SymbolId power) => char.ToUpperInvariant(power.Value[0]) + power.Value.Substring(1);

    private static void PaintTile(RectTransform parent, Tile tile, Vector2 min, Vector2 max, int quarterTurns)
    {
        var slot = Ui.Rect("Slot", parent);
        Ui.Anchored(slot, min, max, Vector2.zero, Vector2.zero);
        Canvas.ForceUpdateCanvases();
        var side = Math.Min(slot.rect.width, slot.rect.height);
        if (side <= 1f)
        {
            side = 72f;
        }

        var block = Ui.Rect("Tile", slot);
        block.anchorMin = new Vector2(0.5f, 0.5f);
        block.anchorMax = new Vector2(0.5f, 0.5f);
        block.pivot = new Vector2(0.5f, 0.5f);
        block.sizeDelta = new Vector2(side, side);
        TilePainter.Paint(block, tile.Id.Value, tile.Local, quarterTurns);
    }
}
}
