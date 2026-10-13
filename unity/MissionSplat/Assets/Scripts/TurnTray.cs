namespace MissionSplat.Player
{

using System;
using MissionSplat.App;
using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

// The bottom of the table: the drawn tile, Spin, the power chips, and the seat's secret missions, with the hand-off cover over the missions.
// When the game is over it shows the winner instead.
internal sealed class TurnTray
{
    public const float HeightUnits = 560f;

    // The wavy top edge rises above the tray's white body, so the board stops this far above the tray to keep clear of it.
    public const float WaveUnits = 56f;

    public const float ReachUnits = HeightUnits + WaveUnits;

    // The secret missions lie a little askew, one each way, like cards dealt on a table.
    private const float MissionTiltDegrees = 4f;

    // The drawn tile's side before the first layout has given its slot a size.
    private const float FallbackTileUnits = 72f;
    private const float TurnArrowUnits = 70f;

    private const float SecretsMinX = 0.58f;
    private const float SecretsMinY = 0.03f;
    private const float SecretsMaxX = 0.97f;
    private const float SecretsMaxY = 0.74f;

    private readonly RectTransform _turn;
    private readonly PowerChips _chips;
    private readonly RectTransform _pending;
    private readonly RectTransform _spin;
    private readonly RectTransform _secrets;
    private readonly RectTransform _conceal;
    private readonly Text _concealLabel;
    private readonly RectTransform _winner;

    public TurnTray(RectTransform play)
    {
        var tray = Ui.Image("Tray", play, SplatPalette.Paper);
        Ui.Anchored(tray.rectTransform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, HeightUnits));
        // The wave sprite is baked in white and ink, so it is drawn untinted; it sits on the tray's top edge, reaching up from it.
        var wave = Ui.Image("Wave", tray.transform, Color.white, Ui.TrayWave);
        Ui.Anchored(wave.rectTransform, new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, WaveUnits));
        _turn = Ui.Rect("Turn", tray.transform);
        Ui.Stretch(_turn);
        _chips = new PowerChips(Area("Powers", _turn, 0.03f, 0.76f, 0.97f, 0.97f));
        _chips.PowerSelected += power => PowerSelected?.Invoke(power);
        _pending = Area("Pending", _turn, 0.03f, 0.03f, 0.37f, 0.74f);
        _spin = Area("Spin", _turn, 0.38f, 0.03f, 0.56f, 0.74f);
        _secrets = SecretsArea("Secrets", _turn);
        _winner = Area("Winner", tray.transform, 0.03f, 0.04f, 0.97f, 0.96f);

        // Over the secret missions only; the tile, Spin, and powers beside it stay in view.
        _conceal = SecretsArea("Conceal", _turn);
        var cover = Ui.Toy("Cover", _conceal, SplatPalette.Paper);
        Ui.Stretch(cover.rectTransform);
        var coverFace = Ui.ToyFace(cover);
        coverFace.GetComponent<Image>().raycastTarget = true;
        _concealLabel = Ui.Label("Message", coverFace, 30, SplatPalette.Ink, TextAnchor.MiddleCenter);
        _concealLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        Ui.Anchored(_concealLabel.rectTransform, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);
        var confirm = Ui.Button("Confirm", coverFace, "Confirm", SplatPalette.Sunny, Ui.Rounded);
        Ui.Anchored(confirm.GetComponent<RectTransform>(), new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.36f), Vector2.zero, Vector2.zero);
        confirm.onClick.AddListener(() => Confirmed?.Invoke());
        _conceal.gameObject.SetActive(false);
    }

    public event Action Confirmed;

    public event Action<int> QuarterTurnsSelected;

    public event Action<SymbolId?> PowerSelected;

    public event Action PlayAgainRequested;

    public event Action NewSetupRequested;

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
        PaintPending(snapshot.View.PendingMatchTile, RotateAmount.IsSelected(snapshot) ? 0 : snapshot.QuarterTurns);
        PaintSpin(snapshot);
        _chips.Paint(snapshot);
        PaintSecrets(snapshot);
        _conceal.gameObject.SetActive(snapshot.ConcealVisible);
        if (snapshot.ConcealVisible)
        {
            _concealLabel.text = Seats.NameOf(snapshot.View.CurrentSeat) + ", confirm to see your missions.";
        }
    }

    private static RectTransform Area(string name, Transform parent, float minX, float minY, float maxX, float maxY)
    {
        var area = Ui.Rect(name, parent);
        Ui.Anchored(area, new Vector2(minX, minY), new Vector2(maxX, maxY), Vector2.zero, Vector2.zero);
        return area;
    }

    // The secret missions' area, which the hand-off cover sits exactly over.
    private static RectTransform SecretsArea(string name, Transform parent) =>
        Area(name, parent, SecretsMinX, SecretsMinY, SecretsMaxX, SecretsMaxY);

    private void PaintWinner(TableSnapshot snapshot, string heading)
    {
        Ui.Clear(_winner);
        PaintResult(snapshot, heading);
        var again = Ui.Button("PlayAgain", _winner, "Play again", SplatPalette.Go, Ui.Rounded, 60);
        Ui.Anchored(again.GetComponent<RectTransform>(), new Vector2(0f, 0.17f), new Vector2(1f, 0.43f), Vector2.zero, Vector2.zero);
        again.onClick.AddListener(() => PlayAgainRequested?.Invoke());
        var setup = Ui.Button("NewSetup", _winner, "New setup", SplatPalette.Paper, Ui.Rounded, 32);
        Ui.Anchored(setup.GetComponent<RectTransform>(), new Vector2(0.25f, 0f), new Vector2(0.75f, 0.13f), Vector2.zero, Vector2.zero);
        setup.onClick.AddListener(() => NewSetupRequested?.Invoke());
    }

    // The top of the game-end tray: who won and with how many claims, above the two buttons.
    private void PaintResult(TableSnapshot snapshot, string heading)
    {
        if (snapshot.Status.Seat is not { } winner)
        {
            var over = Ui.Label("Message", _winner, 64, SplatPalette.Ink, TextAnchor.MiddleCenter, FontWeight.ExtraBold);
            Ui.Anchored(over.rectTransform, new Vector2(0f, 0.47f), Vector2.one, Vector2.zero, Vector2.zero);
            over.text = heading;
            return;
        }

        var index = Seats.IndexOf(snapshot.View, winner);
        var critterSlot = Ui.Rect("CritterSlot", _winner);
        Ui.Anchored(critterSlot, new Vector2(0f, 0.47f), new Vector2(0.30f, 1f), Vector2.zero, Vector2.zero);
        CritterPainter.PaintFitted(critterSlot, index);
        var title = Ui.Label("Message", _winner, 72, SplatPalette.Ink, TextAnchor.LowerLeft, FontWeight.ExtraBold);
        Ui.Anchored(title.rectTransform, new Vector2(0.34f, 0.74f), Vector2.one, Vector2.zero, Vector2.zero);
        title.text = heading;
        var count = Ui.Label("Claims", _winner, 40, SplatPalette.Muted, TextAnchor.UpperLeft);
        Ui.Anchored(count.rectTransform, new Vector2(0.34f, 0.47f), new Vector2(1f, 0.74f), Vector2.zero, Vector2.zero);
        var total = snapshot.View.Claims[index].Missions.Count;
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
        var button = Ui.Button("Spin", slot, "Spin", SplatPalette.Sunny, Ui.Circle, 40);
        var rect = button.GetComponent<RectTransform>();
        Ui.Stretch(rect);
        var fit = button.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1f;
        Ui.SetEnabled(button, snapshot.CanAct);
        var next = (snapshot.QuarterTurns + 1) % 4;
        button.onClick.AddListener(() => QuarterTurnsSelected?.Invoke(next));

        // While rotate is selected the drawn tile does not preview the turns, so one arrow per quarter-turn shows the amount.
        if (RotateAmount.IsSelected(snapshot) && snapshot.QuarterTurns > 0)
        {
            PaintTurnHint(snapshot.QuarterTurns);
        }
    }

    private void PaintTurnHint(int quarterTurns)
    {
        var hint = Ui.Rect("TurnHint", _spin);
        hint.anchorMin = new Vector2(0.5f, 0.04f);
        hint.anchorMax = new Vector2(0.5f, 0.24f);
        hint.sizeDelta = new Vector2(quarterTurns * TurnArrowUnits, 0f);
        hint.anchoredPosition = Vector2.zero;
        for (var i = 0; i < quarterTurns; i++)
        {
            var arrow = Ui.Image("Quarter" + i, hint, SplatPalette.Ink, Ui.RotateArrow);
            arrow.preserveAspect = true;
            Ui.Anchored(arrow.rectTransform, new Vector2(i / (float)quarterTurns, 0f), new Vector2((i + 1) / (float)quarterTurns, 1f), Vector2.zero, Vector2.zero);
        }
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
            slot.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? MissionTiltDegrees : -MissionTiltDegrees);
            MissionCardPainter.Paint(slot, missions[i]);
        }
    }

    private static void PaintTile(RectTransform parent, Tile tile, Vector2 min, Vector2 max, int quarterTurns)
    {
        var slot = Ui.Rect("Slot", parent);
        Ui.Anchored(slot, min, max, Vector2.zero, Vector2.zero);
        Canvas.ForceUpdateCanvases();
        var side = Math.Min(slot.rect.width, slot.rect.height);
        if (side <= 1f)
        {
            side = FallbackTileUnits;
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
