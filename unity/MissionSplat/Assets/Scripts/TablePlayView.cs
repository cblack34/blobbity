namespace MissionSplat.Player
{

using System;
using System.Collections.Generic;
using System.Linq;
using MissionSplat.App;
using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

internal sealed class TablePlayView
{
    // Canvas units, top to bottom: the seat bar, the heading, one instruction line, a notice strip (the recap summary, or the reshuffle notice), then the board, then the turn tray.
    private const float SeatBarUnits = 230f;
    private const float HeadingUnits = 100f;
    private const float StatusUnits = 110f;
    private const float NoticeUnits = 60f;
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
    private readonly RecapPlayer _recap;
    private readonly PlacementStrip _strip;

    // What was last rendered, to redraw the board when the preview changes without a new snapshot.
    private TableSnapshot _snapshot;

    // The spot the drawn tile is being tried at, and which tile that is, so a preview never outlives its turn. View state only: the table
    // learns of a placement when Place is pressed, and says which spots are legal.
    private (int TileX, int TileY, string Tile)? _preview;

    // The player pressed Place on the previewed spot.
    public event Action<int, int> PlaceRequested;
    public event Action Confirmed;
    public event Action<int> QuarterTurnsSelected;
    public event Action<SymbolId?> PowerSelected;
    public event Action<int, int> TargetTapped;
    public event Action PlayAgainRequested;
    public event Action NewSetupRequested;

    // Raised with the items a finished recap showed.
    public event Action<IReadOnlyList<RecapItem>> RecapFinished;

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
        // A short fact under the instruction; hidden while there is nothing to say. A long recap summary wraps to two lines, so it shrinks to fit.
        _notice = Ui.Label("Notice", play, 28, SplatPalette.Muted, TextAnchor.MiddleCenter);
        _notice.horizontalOverflow = HorizontalWrapMode.Wrap;
        _notice.resizeTextForBestFit = true;
        _notice.resizeTextMinSize = 18;
        _notice.resizeTextMaxSize = 28;
        Band(_notice.rectTransform, NoticeTopUnits, NoticeUnits);
        _notice.gameObject.SetActive(false);

        var board = Ui.Rect("Board", play);
        Ui.Anchored(board, Vector2.zero, Vector2.one, new Vector2(24f, TurnTray.ReachUnits), new Vector2(-24f, -BoardTopUnits));
        _board = new BoardPanel(board);
        _board.PlacementTapped += Preview;
        _board.TargetTapped += (tileX, tileY) => TargetTapped?.Invoke(tileX, tileY);

        _tray = new TurnTray(play);
        _tray.Confirmed += () => Confirmed?.Invoke();
        _tray.QuarterTurnsSelected += turns => QuarterTurnsSelected?.Invoke(turns);
        _tray.PowerSelected += power => PowerSelected?.Invoke(power);
        _tray.PlayAgainRequested += () => PlayAgainRequested?.Invoke();
        _tray.NewSetupRequested += () => NewSetupRequested?.Invoke();

        _strip = new PlacementStrip(play);
        _strip.Placed += Place;
        _strip.Cancelled += ClearPreview;

        // Last, so it draws over everything else.
        _claimsPanel = new ClaimsPanel(play);
        _seatBar.SeatTapped += _claimsPanel.Show;

        // After the claims panel, so it draws over everything and takes every tap while a recap plays.
        _recap = new RecapPlayer(new RecapStage(play, board, _board, _seatBar));
        _recap.Finished += shown => RecapFinished?.Invoke(shown);
        // The heading says what is playing until the recap ends.
        _recap.ItemStarted += item => _heading.text = RecapSummary.Caption(item);
    }

    internal RecapPlayer Recap => _recap;

    // Starts the snapshot's recap, if it has one and none is playing; the caller has rendered the snapshot first.
    public void PlayRecap(TableSnapshot snapshot) => _recap.Play(snapshot);

    public void Tick(float unscaledSeconds) => _recap.Tick(unscaledSeconds);

    public void CancelRecap() => _recap.Cancel();

    // A new game starts with its board centred and nothing previewed.
    public void NewGame()
    {
        _preview = null;
        _board.ForgetScroll();
    }

    // summary is the line a finished recap left, or null; it takes the notice strip until the next action.
    public void Render(TableSnapshot snapshot, string summary = null)
    {
        Canvas.ForceUpdateCanvases();
        _snapshot = snapshot;
        if (!StillPreviewable(snapshot))
        {
            _preview = null;
        }

        // What happens next waits for the recap that led to it: the heading names the item playing, and the instruction is blank.
        var recapPending = snapshot.Recap.Count > 0;
        var heading = recapPending ? string.Empty : HeadingText(snapshot);
        _heading.text = _recap.IsPlaying ? RecapSummary.Caption(_recap.Current) : heading;
        _status.text = recapPending ? string.Empty : InstructionText(snapshot);
        _notice.text = summary ?? NoticeText(snapshot.Notice);
        _notice.gameObject.SetActive(_notice.text.Length > 0);
        _seatBar.Paint(snapshot.View, HighlightedSeat(snapshot));
        // What the panel shows may be out of date after any action, so a render closes it.
        _claimsPanel.Hide();
        _tray.Paint(snapshot, heading);
        PaintBoard();
    }

    // The preview holds only while its spot is still offered for the same drawn tile at the current Spin turn, with no recap in the way.
    private bool StillPreviewable(TableSnapshot snapshot) =>
        _preview is { } preview
        && snapshot.Recap.Count == 0
        && snapshot.View.PendingMatchTile?.Id.Value == preview.Tile
        && snapshot.LegalPlacements.Any(spot => spot.TileX == preview.TileX && spot.TileY == preview.TileY);

    private void PaintBoard()
    {
        // The strip covers the board's bottom edge while it shows, so the board leaves room to scroll clear of it.
        _board.Paint(_snapshot, _preview is { } preview ? (preview.TileX, preview.TileY) : null, _preview != null ? PlacementStrip.HeightUnits : 0f);
        _strip.Show(_preview != null);
    }

    // A tap on a legal spot only tries the tile there.
    private void Preview(int tileX, int tileY)
    {
        if (_recap.IsPlaying || _snapshot?.View.PendingMatchTile is not { } drawn)
        {
            return;
        }

        _preview = (tileX, tileY, drawn.Id.Value);
        Canvas.ForceUpdateCanvases();
        PaintBoard();
        // The new spot may sit behind the strip, or off the view.
        _board.RevealTile(tileX, tileY, PlacementStrip.HeightUnits);
    }

    private void ClearPreview()
    {
        _preview = null;
        Canvas.ForceUpdateCanvases();
        PaintBoard();
    }

    private void Place()
    {
        if (_preview is not { } preview)
        {
            return;
        }

        // Redrawn first, so nothing lingers if the session drops the action; a render that follows draws the new state.
        ClearPreview();
        PlaceRequested?.Invoke(preview.TileX, preview.TileY);
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
