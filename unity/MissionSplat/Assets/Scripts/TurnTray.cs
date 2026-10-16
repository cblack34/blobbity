namespace MissionSplat.Player
{

using System;
using System.Collections.Generic;
using System.Linq;
using MissionSplat.App;
using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

// The bottom of the table: the drawn tile (its power badges grayed as their uses are spent), Spin, and the seat's secret blobbities, with the hand-off cover over the blobbities.
// When the game is over it shows the winner instead.
internal sealed class TurnTray
{
    public const float HeightUnits = 450f;

    // The wavy top edge rises above the tray's white body, so the board stops this far above the tray to keep clear of it.
    public const float WaveUnits = 56f;

    public const float ReachUnits = HeightUnits + WaveUnits;

    // The secret blobbities lie a little askew, one each way, like cards dealt on a table.
    private const float BlobbityTiltDegrees = 4f;

    // The drawn tile's side before the first layout has given its slot a size.
    private const float FallbackTileUnits = 72f;

    // The turn tray's rows, as fractions of its height: the drawn tile, Spin, and the secret blobbities fill the tray from the bottom margin to the top.
    private const float ContentBottom = 0.03f;
    private const float ContentTop = 0.97f;

    private const float SecretsMinX = 0.58f;
    private const float SecretsMaxX = 0.97f;

    // The game-end tray's rows, as fractions of the winner area (about 415 units tall): each button is at least the
    // 120-unit tap size (see BoardPanel), and the result sits above them.
    private const float NewSetupTop = 0.30f;
    private const float PlayAgainBottom = 0.32f;
    private const float PlayAgainTop = 0.62f;
    private const float ResultBottom = 0.64f;
    private const float ResultSplit = 0.78f;

    private readonly RectTransform _turn;
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
        _pending = Area("Pending", _turn, 0.03f, ContentBottom, 0.37f, ContentTop);
        _spin = Area("Spin", _turn, 0.38f, ContentBottom, 0.56f, ContentTop);
        _secrets = SecretsArea("Secrets", _turn);
        _winner = Area("Winner", tray.transform, 0.03f, 0.04f, 0.97f, 0.96f);

        // Over the secret blobbities only; the tile and Spin beside it stay in view.
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

    public event Action PlayAgainRequested;

    public event Action NewSetupRequested;

    // heading names the winner when the game is over.
    public void Paint(TableSnapshot snapshot, string heading)
    {
        // The result waits for the recap that led to it.
        var ended = snapshot.Status.Kind is TableStatusKind.Won or TableStatusKind.Ended && snapshot.Recap.Count == 0;
        _turn.gameObject.SetActive(!ended);
        _winner.gameObject.SetActive(ended);
        if (ended)
        {
            PaintWinner(snapshot, heading);
            return;
        }

        PaintPending(snapshot);
        PaintSpin(snapshot);
        PaintSecrets(snapshot);
        var covered = CoverUp(snapshot);
        _conceal.gameObject.SetActive(covered);
        if (covered)
        {
            _concealLabel.text = Seats.NameOf(snapshot.View.CurrentSeat) + ", confirm to see your blobbities.";
        }
    }

    // The hand-off cover waits for the outgoing player's recap: until they have seen their own turn, the device is still theirs.
    private static bool CoverUp(TableSnapshot snapshot) => snapshot.ConcealVisible && snapshot.Recap.Count == 0;

    private static RectTransform Area(string name, Transform parent, float minX, float minY, float maxX, float maxY)
    {
        var area = Ui.Rect(name, parent);
        Ui.Anchored(area, new Vector2(minX, minY), new Vector2(maxX, maxY), Vector2.zero, Vector2.zero);
        return area;
    }

    // The secret blobbities' area, which the hand-off cover sits exactly over.
    private static RectTransform SecretsArea(string name, Transform parent) =>
        Area(name, parent, SecretsMinX, ContentBottom, SecretsMaxX, ContentTop);

    private void PaintWinner(TableSnapshot snapshot, string heading)
    {
        Ui.Clear(_winner);
        PaintResult(snapshot, heading);
        var again = Ui.Button("PlayAgain", _winner, "Play again", SplatPalette.Go, Ui.Rounded, 60);
        Ui.Anchored(again.GetComponent<RectTransform>(), new Vector2(0f, PlayAgainBottom), new Vector2(1f, PlayAgainTop), Vector2.zero, Vector2.zero);
        again.onClick.AddListener(() => PlayAgainRequested?.Invoke());
        var setup = Ui.Button("NewSetup", _winner, "New setup", SplatPalette.Paper, Ui.Rounded, 32);
        Ui.Anchored(setup.GetComponent<RectTransform>(), new Vector2(0.25f, 0f), new Vector2(0.75f, NewSetupTop), Vector2.zero, Vector2.zero);
        setup.onClick.AddListener(() => NewSetupRequested?.Invoke());
    }

    // The top of the game-end tray: who won and with how many claims, above the two buttons.
    private void PaintResult(TableSnapshot snapshot, string heading)
    {
        if (snapshot.Status.Seat is not { } winner)
        {
            var over = Ui.Label("Message", _winner, 64, SplatPalette.Ink, TextAnchor.MiddleCenter, FontWeight.ExtraBold);
            Ui.Anchored(over.rectTransform, new Vector2(0f, ResultBottom), Vector2.one, Vector2.zero, Vector2.zero);
            over.text = heading;
            return;
        }

        var index = Seats.IndexOf(snapshot.View, winner);
        var critterSlot = Ui.Rect("CritterSlot", _winner);
        Ui.Anchored(critterSlot, new Vector2(0f, ResultBottom), new Vector2(0.30f, 1f), Vector2.zero, Vector2.zero);
        CritterPainter.PaintFitted(critterSlot, index);
        var title = Ui.Label("Message", _winner, 72, SplatPalette.Ink, TextAnchor.LowerLeft, FontWeight.ExtraBold);
        Ui.Anchored(title.rectTransform, new Vector2(0.34f, ResultSplit), Vector2.one, Vector2.zero, Vector2.zero);
        title.text = heading;
        var count = Ui.Label("Claims", _winner, 40, SplatPalette.Muted, TextAnchor.UpperLeft);
        Ui.Anchored(count.rectTransform, new Vector2(0.34f, ResultBottom), new Vector2(1f, ResultSplit), Vector2.zero, Vector2.zero);
        var total = snapshot.View.Claims[index].Blobbities.Count;
        count.text = total + (total == 1 ? " claim" : " claims");
    }

    private void PaintPending(TableSnapshot snapshot)
    {
        Ui.Clear(_pending);
        if (snapshot.View.PendingMatchTile is not { } pending)
        {
            return;
        }

        var spent = SpentBadges(snapshot.View, pending);
        PaintTile(_pending, pending, Vector2.zero, Vector2.one, snapshot.QuarterTurns, (x, y) => spent.Contains((x, y)));
    }

    // Where each kind of power on the drawn tile has its badge, one per kind, in cell order.
    public IReadOnlyList<PowerSpot> PowerSpots(SeatView view)
    {
        var spots = new List<PowerSpot>();
        if (view.PendingMatchTile is not { } tile)
        {
            return spots;
        }

        Canvas.ForceUpdateCanvases();
        var corners = new Vector3[4];
        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                // A symbol that has a badge look is taken to be a power. A follow-up (#76) has the App say which powers are in play.
                if (!tile.Local(x, y).TryGetSymbol(out var power) || !PowerBadge.IsPower(power) || spots.Exists(spot => spot.Power.Equals(power)))
                {
                    continue;
                }

                var cell = LiveChild(_pending, TilePainter.CellName(x, y));
                if (cell != null && LiveChild(cell, CellPainter.BadgeName) is { } badge)
                {
                    badge.GetWorldCorners(corners);
                    spots.Add(new PowerSpot(power, corners[0], corners[2]));
                }
                else
                {
                    Debug.LogWarning("The drawn tile's " + power.Value + " at cell " + x + "," + y + " has no badge on the tray to splash from.");
                }
            }
        }

        return spots;
    }

    // The still-showing descendant named name; a re-render leaves the replaced ones inactive until the frame ends.
    private static RectTransform LiveChild(RectTransform parent, string name)
    {
        foreach (var rect in parent.GetComponentsInChildren<RectTransform>(false))
        {
            if (rect.name == name)
            {
                return rect;
            }
        }

        return null;
    }

    // The local cells of the drawn tile whose power badge is used up. A power's uses are a count, not tied to a cell,
    // so as many of its badges as are spent, in cell order, show gray.
    private static HashSet<(int X, int Y)> SpentBadges(SeatView view, Tile tile)
    {
        var spent = new HashSet<(int X, int Y)>();
        foreach (var charge in view.RemainingUses)
        {
            var badges = new List<(int X, int Y)>();
            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 2; x++)
                {
                    if (tile.Local(x, y).TryGetSymbol(out var symbol) && symbol.Equals(charge.Power))
                    {
                        badges.Add((x, y));
                    }
                }
            }

            spent.UnionWith(badges.Take(Math.Max(0, badges.Count - charge.Remaining)));
        }

        return spent;
    }

    // Each Spin tap turns the drawn tile one more clockwise quarter-turn, then wraps from 3 back to 0.
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
    }

    private void PaintSecrets(TableSnapshot snapshot)
    {
        Ui.Clear(_secrets);
        // The hand-off cover sits over this area, and says what is hidden itself.
        if (CoverUp(snapshot))
        {
            return;
        }

        var title = Ui.Label("Title", _secrets, 30, SplatPalette.Ink, TextAnchor.MiddleCenter);
        Ui.Anchored(title.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.12f), Vector2.zero, Vector2.zero);
        title.text = snapshot.SecretsVisible ? "Your secret shapes" : "Blobbities hidden";
        if (!snapshot.SecretsVisible)
        {
            return;
        }

        var blobbities = snapshot.View.UnclaimedBlobbities;
        for (var i = 0; i < blobbities.Count; i++)
        {
            var slot = Ui.Rect("Blobbity" + i, _secrets);
            var min = new Vector2(0.01f + (i * 0.5f), 0.14f);
            var max = new Vector2(0.49f + (i * 0.5f), 1f);
            Ui.Anchored(slot, min, max, Vector2.zero, Vector2.zero);
            slot.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? BlobbityTiltDegrees : -BlobbityTiltDegrees);
            BlobbityCardPainter.Paint(slot, blobbities[i]);
        }
    }

    private static void PaintTile(RectTransform parent, Tile tile, Vector2 min, Vector2 max, int quarterTurns, Func<int, int, bool> spentAt)
    {
        var slot = Ui.Rect("Slot", parent);
        Ui.Anchored(slot, min, max, Vector2.zero, Vector2.zero);
        Canvas.ForceUpdateCanvases();
        var side = Math.Min(slot.rect.width, slot.rect.height);
        if (side <= 1f)
        {
            side = FallbackTileUnits;
        }

        var block = Ui.Centered("Tile", slot, new Vector2(side, side));
        TilePainter.Paint(block, tile.Id.Value, tile.Local, quarterTurns, spentAt);
    }
}
}
