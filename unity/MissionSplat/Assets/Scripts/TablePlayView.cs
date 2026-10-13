namespace MissionSplat.Player
{

using System;
using System.Linq;
using MissionSplat.App;
using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

internal sealed class TablePlayView
{
    // Canvas units, top to bottom: the seat bar, the heading, one instruction line, a notice strip, then the board, then the turn tray.
    private const float SeatBarUnits = 230f;
    private const float HeadingUnits = 100f;
    private const float StatusUnits = 110f;
    private const float NoticeUnits = 44f;
    private const float HeadingTopUnits = SeatBarUnits;
    private const float StatusTopUnits = HeadingTopUnits + HeadingUnits;
    private const float NoticeTopUnits = StatusTopUnits + StatusUnits;
    private const float BoardTopUnits = NoticeTopUnits + NoticeUnits;

    private readonly Text _heading;
    private readonly Text _status;
    private readonly Text _notice;
    private readonly SeatBar _seatBar;
    private readonly ClaimsPanel _claimsPanel;
    private readonly BoardPanel _board;
    private readonly TurnTray _tray;

    public event Action<int, int> Tapped;
    public event Action Confirmed;
    public event Action<int> QuarterTurnsSelected;
    public event Action<SymbolId?> PowerSelected;
    public event Action<int, int> TargetTapped;
    public event Action PlayAgainRequested;
    public event Action NewSetupRequested;

    public TablePlayView(RectTransform play)
    {
        var seatBar = Ui.Rect("SeatBar", play);
        Band(seatBar, 0f, SeatBarUnits);
        _seatBar = new SeatBar(seatBar);

        _heading = Ui.Label("Heading", play, 76, SplatPalette.Ink, TextAnchor.MiddleCenter, FontWeight.ExtraBold);
        Band(_heading.rectTransform, HeadingTopUnits, HeadingUnits);
        _status = Ui.Label("Status", play, 34, SplatPalette.Ink, TextAnchor.UpperCenter);
        // Stop rulings are long sentences. Only this label wraps; captions and buttons stay on one line.
        _status.horizontalOverflow = HorizontalWrapMode.Wrap;
        Band(_status.rectTransform, StatusTopUnits, StatusUnits, 40f);
        // A one-line fact under the instruction; hidden while there is nothing to say.
        _notice = Ui.Label("Notice", play, 28, SplatPalette.Muted, TextAnchor.MiddleCenter);
        Band(_notice.rectTransform, NoticeTopUnits, NoticeUnits);
        _notice.gameObject.SetActive(false);

        var board = Ui.Rect("Board", play);
        Ui.Anchored(board, Vector2.zero, Vector2.one, new Vector2(24f, TurnTray.ReachUnits), new Vector2(-24f, -BoardTopUnits));
        _board = new BoardPanel(board);
        _board.PlacementTapped += (tileX, tileY) => Tapped?.Invoke(tileX, tileY);
        _board.TargetTapped += (tileX, tileY) => TargetTapped?.Invoke(tileX, tileY);

        _tray = new TurnTray(play);
        _tray.Confirmed += () => Confirmed?.Invoke();
        _tray.QuarterTurnsSelected += turns => QuarterTurnsSelected?.Invoke(turns);
        _tray.PowerSelected += power => PowerSelected?.Invoke(power);
        _tray.PlayAgainRequested += () => PlayAgainRequested?.Invoke();
        _tray.NewSetupRequested += () => NewSetupRequested?.Invoke();

        // Last, so it draws over everything else.
        _claimsPanel = new ClaimsPanel(play);
        _seatBar.SeatTapped += _claimsPanel.Show;
    }

    // A new game starts with its board centred.
    public void ForgetScroll() => _board.ForgetScroll();

    public void Render(TableSnapshot snapshot)
    {
        Canvas.ForceUpdateCanvases();
        var heading = HeadingText(snapshot);
        _heading.text = heading;
        _status.text = InstructionText(snapshot);
        _notice.text = NoticeText(snapshot.Notice);
        _notice.gameObject.SetActive(snapshot.Notice != TableNotice.None);
        _seatBar.Paint(snapshot.View, HighlightedSeat(snapshot));
        // What the panel shows may be out of date after any action, so a render closes it.
        _claimsPanel.Hide();
        _tray.Paint(snapshot, heading);
        _board.Paint(snapshot);
    }

    // A band across the screen: top units from the top edge, height units tall, inset on both sides.
    private static void Band(RectTransform rect, float top, float height, float inset = 0f)
    {
        Ui.Anchored(rect, new Vector2(0f, 1f), Vector2.one, new Vector2(inset, -(top + height)), new Vector2(-inset, -top));
    }

    // The seat to play, or the winner once the game is won; nobody when it ended without one.
    private static SeatId? HighlightedSeat(TableSnapshot snapshot) => snapshot.Status.Kind switch
    {
        TableStatusKind.Won => snapshot.Status.Seat,
        TableStatusKind.Ended => null,
        _ => snapshot.View.CurrentSeat,
    };

    private static string HeadingText(TableSnapshot snapshot)
    {
        var status = snapshot.Status;
        return status.Kind switch
        {
            TableStatusKind.ToConfirm or TableStatusKind.ToAct => Seats.NameOf(status.Seat.Value) + "\u2019s turn!",
            TableStatusKind.Won => Seats.NameOf(status.Seat.Value) + " wins!",
            TableStatusKind.Ended => "Game over",
            _ => "Play has stopped",
        };
    }

    private static string InstructionText(TableSnapshot snapshot)
    {
        var status = snapshot.Status;
        switch (status.Kind)
        {
            case TableStatusKind.ToConfirm:
                return "Hand the device to " + Seats.NameOf(status.Seat.Value) + ", then confirm.";
            case TableStatusKind.ToAct:
                return ActInstruction(snapshot);
            case TableStatusKind.Won:
                return Seats.ClaimsToWin + " claims wins the game.";
            case TableStatusKind.Ended:
                return "The game has ended.";
            default:
                return status.Message;
        }
    }

    private static string ActInstruction(TableSnapshot snapshot)
    {
        if (snapshot.SelectedPower is { } power)
        {
            return RotateAmount.IsPending(snapshot)
                ? "Spin to choose the turns, then pick a tile to rotate."
                : "Pick a tile to " + power.Value + ".";
        }

        return snapshot.View.RemainingUses.Any(charge => charge.Remaining > 0)
            ? "Place your tile, or use a power first."
            : "Place your tile.";
    }

    private static string NoticeText(TableNotice notice) =>
        notice == TableNotice.DeckReshuffled ? "The draw deck ran out; the board was reshuffled." : string.Empty;
}
}
