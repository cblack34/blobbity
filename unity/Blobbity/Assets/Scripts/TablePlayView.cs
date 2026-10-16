namespace Blobbity.Player
{

using System;
using System.Collections.Generic;
using System.Linq;
using Blobbity.App;
using Blobbity.Rules;
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
    private readonly PreviewStrip _strip;

    // What was last rendered, to redraw the board when the preview changes without a new snapshot.
    private TableSnapshot _snapshot;

    // What the player is trying on the board: the drawn tile at a spot, or a board tile selected for a rotate or bounce. View state only: the table
    // learns of it when the strip's Place, Confirm, or Bounce is pressed, and says which spots and targets are legal.
    private TilePreview _preview;

    // The drawn tile the holder was last looking at, when they could act, power or not: a power tile is announced when it differs from this, so
    // a re-render, a Spin, or a preview does not announce it again, but one that comes back after another tile does. It is marked when the splash
    // is queued, not when it ends, so a cancelled, failed, or instant splash never loops.
    private (SeatId Seat, TileId Tile)? _lastSeen;

    // The player pressed Place on the previewed spot.
    public event Action<int, int> PlaceRequested;

    // The player confirmed a rotate or a bounce of the selected tile.
    public event Action<GameAction> PowerRequested;
    public event Action Confirmed;
    public event Action<int> QuarterTurnsSelected;
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
        _board.TileTapped += OnTileTapped;

        _tray = new TurnTray(play);
        _tray.Confirmed += () => Confirmed?.Invoke();
        _tray.QuarterTurnsSelected += turns => QuarterTurnsSelected?.Invoke(turns);
        _tray.PlayAgainRequested += () => PlayAgainRequested?.Invoke();
        _tray.NewSetupRequested += () => NewSetupRequested?.Invoke();

        _strip = new PreviewStrip(play);
        _strip.Placed += Place;
        _strip.Turned += Turn;
        _strip.PowerConfirmed += UsePower;
        _strip.Cancelled += ClearPreview;

        // Last, so it draws over everything else.
        _claimsPanel = new ClaimsPanel(play);
        _seatBar.SeatTapped += _claimsPanel.Show;

        // After the claims panel, so it draws over everything and takes every tap while a recap plays.
        _recap = new RecapPlayer(new RecapStage(play, board, _board, _seatBar));
        _recap.Finished += shown => RecapFinished?.Invoke(shown);
        // The heading says what is playing until the recap ends.
        _recap.ItemStarted += caption => _heading.text = caption;
        // The heading goes back to the turn once the splashes are over; a recap's end renders instead.
        _recap.SplashesFinished += () =>
        {
            _heading.text = HeadingText(_snapshot);
            PaintStatus();
        };
    }

    internal RecapPlayer Recap => _recap;

    // Starts what the snapshot has pending, if nothing is playing: its recap, or else the splashes for the drawn tile the holder has not yet been
    // told about. The caller has rendered the snapshot first.
    public void PlayPending(TableSnapshot snapshot)
    {
        if (snapshot.Recap.Count > 0)
        {
            _recap.Play(snapshot);
            return;
        }

        AnnouncePowers(snapshot);
    }

    // Only the human who can act on it hears of a drawn tile's powers, once, and never behind the hand-off cover.
    private void AnnouncePowers(TableSnapshot snapshot)
    {
        if (_recap.IsPlaying || !snapshot.CanAct || snapshot.ConcealVisible || snapshot.View.PendingMatchTile is not { } tile)
        {
            return;
        }

        var drawn = (snapshot.View.Seat, tile.Id);
        if (_lastSeen == drawn)
        {
            return;
        }

        _lastSeen = drawn;
        _recap.PlaySplashes(_tray.PowerSpots(snapshot.View));
        PaintStatus();
    }

    public void Tick(float unscaledSeconds) => _recap.Tick(unscaledSeconds);

    public void CancelRecap() => _recap.Cancel();

    // A new game starts with its board centred and nothing previewed.
    public void NewGame()
    {
        _preview = null;
        _lastSeen = null;
        _board.ForgetScroll();
    }

    // summary is the line a finished recap left, or null; it takes the notice strip until the next action.
    public void Render(TableSnapshot snapshot, string summary = null)
    {
        Canvas.ForceUpdateCanvases();
        _snapshot = snapshot;
        if (_preview?.Holds(snapshot) == false)
        {
            _preview = null;
        }

        // What happens next waits for the recap that led to it: the heading names the item playing, and the instruction is blank.
        var recapPending = snapshot.Recap.Count > 0;
        var heading = recapPending ? string.Empty : HeadingText(snapshot);
        _heading.text = _recap.IsPlaying ? _recap.Caption : heading;
        PaintStatus();
        _notice.text = summary ?? NoticeText(snapshot.Notice);
        _notice.gameObject.SetActive(_notice.text.Length > 0);
        _seatBar.Paint(snapshot.View, HighlightedSeat(snapshot));
        // What the panel shows may be out of date after any action, so a render closes it.
        _claimsPanel.Hide();
        _tray.Paint(snapshot, heading);
        PaintBoard();
    }

    // The instruction waits for the recap or splash that leads to it, and follows what is being tried.
    private void PaintStatus() => _status.text = _snapshot.Recap.Count > 0 || _recap.IsPlaying ? string.Empty : InstructionText(_snapshot, _preview);

    private void PaintBoard()
    {
        // The strip covers the board's bottom edge while it shows, so the board leaves room to scroll clear of it.
        _board.Paint(_snapshot, _preview, _preview != null ? PreviewStrip.HeightUnits : 0f);
        _strip.Show(_preview);
        // Every paint can move the tile: a tap, a Spin, or a new viewport size. It may sit behind the strip, or off the view.
        if (_preview != null)
        {
            _board.RevealTile(_preview.TileX, _preview.TileY, PreviewStrip.HeightUnits);
        }
    }

    // What the player tried changed without a new snapshot: repaint the instruction and the board to match.
    private void Repaint()
    {
        PaintStatus();
        Canvas.ForceUpdateCanvases();
        PaintBoard();
    }

    // A tap on a legal spot only tries the drawn tile there; a tap on a power target only selects that tile. Nothing starts while a recap is pending.
    private void OnTileTapped(int tileX, int tileY)
    {
        if (_snapshot == null || TilePreview.Tapped(_snapshot, tileX, tileY) is not { } tried)
        {
            return;
        }

        _preview = tried;
        Repaint();
    }

    private void Turn()
    {
        if (_preview is { Kind: PreviewKind.Rotate } rotate)
        {
            _preview = rotate.Turned();
            Repaint();
        }
    }

    private void ClearPreview()
    {
        _preview = null;
        Repaint();
    }

    private void Place()
    {
        if (_preview is not { Kind: PreviewKind.Placement } preview)
        {
            return;
        }

        // Redrawn first, so nothing lingers if the session drops the action; a render that follows draws the new state.
        ClearPreview();
        PlaceRequested?.Invoke(preview.TileX, preview.TileY);
    }

    // Confirm on a rotate, or Bounce, on the selected tile.
    private void UsePower()
    {
        if (_preview?.ToAction() is not { } action)
        {
            return;
        }

        ClearPreview();
        PowerRequested?.Invoke(action);
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

    private static string InstructionText(TableSnapshot snapshot, TilePreview preview)
    {
        var status = snapshot.Status;
        switch (status.Kind)
        {
            case TableStatusKind.ToConfirm:
                return "Hand the device to " + Seats.NameOf(status.Seat.Value) + ", then confirm.";
            case TableStatusKind.ToAct:
                return ActInstruction(snapshot, preview);
            case TableStatusKind.Won:
                return Seats.ClaimsToWin + " claims wins the game.";
            case TableStatusKind.Ended:
                return "The game has ended.";
            default:
                return status.Message;
        }
    }

    private static string ActInstruction(TableSnapshot snapshot, TilePreview preview)
    {
        switch (preview?.Kind)
        {
            case PreviewKind.Rotate:
                return "Turn the tile, then confirm.";
            case PreviewKind.Bounce:
                return "Bounce this tile?";
            case null:
            case PreviewKind.Placement:
                return TilePreview.PowerTargets(snapshot).Any()
                    ? "Place your tile, or tap a ringed tile to use a power."
                    : "Place your tile.";
            default:
                throw new ArgumentOutOfRangeException(nameof(preview), preview.Kind, "Unknown kind of preview.");
        }
    }

    private static string NoticeText(TableNotice notice) =>
        notice == TableNotice.DeckReshuffled ? "The draw deck ran out; the board was reshuffled." : string.Empty;
}
}
