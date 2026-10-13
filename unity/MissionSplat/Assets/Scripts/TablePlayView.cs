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
    // Canvas units, top to bottom: the seat bar, the heading, one instruction line, a notice strip, then the board, then the turn tray.
    private const float SeatBarUnits = 230f;
    private const float HeadingUnits = 100f;
    private const float StatusUnits = 110f;
    private const float NoticeUnits = 44f;

    // Below this a tile is too small to tap with a finger; a board that needs more room is clipped, not shrunk further.
    private const float MinimumCellUnits = 44f;

    private readonly Text _heading;
    private readonly Text _status;
    private readonly Text _notice;
    private readonly SeatBar _seatBar;
    private readonly ClaimsPanel _claimsPanel;
    private readonly RectTransform _board;
    private readonly TurnTray _tray;

    public event Action<int, int> Tapped;
    public event Action Confirmed;
    public event Action<int> QuarterTurnsSelected;
    public event Action<SymbolId?> PowerSelected;
    public event Action<int, int> TargetTapped;

    public TablePlayView(RectTransform play)
    {
        var seatBar = Ui.Rect("SeatBar", play);
        Band(seatBar, 0f, SeatBarUnits);
        _seatBar = new SeatBar(seatBar);

        _heading = Ui.Label("Heading", play, 76, SplatPalette.Ink, TextAnchor.MiddleCenter);
        _heading.fontStyle = FontStyle.Bold;
        Band(_heading.rectTransform, SeatBarUnits, HeadingUnits);
        _status = Ui.Label("Status", play, 34, SplatPalette.Ink, TextAnchor.UpperCenter);
        // Stop rulings are long sentences. Only this label wraps; captions and buttons stay on one line.
        _status.horizontalOverflow = HorizontalWrapMode.Wrap;
        Band(_status.rectTransform, SeatBarUnits + HeadingUnits, StatusUnits, 40f);
        // A one-line fact under the instruction; hidden while there is nothing to say.
        _notice = Ui.Label("Notice", play, 28, SplatPalette.Muted, TextAnchor.MiddleCenter);
        Band(_notice.rectTransform, SeatBarUnits + HeadingUnits + StatusUnits, NoticeUnits);
        _notice.gameObject.SetActive(false);

        _board = Ui.Rect("Board", play);
        Ui.Anchored(_board, Vector2.zero, Vector2.one, new Vector2(24f, TurnTray.HeightUnits + 12f), new Vector2(-24f, -(SeatBarUnits + HeadingUnits + StatusUnits + NoticeUnits)));
        _board.gameObject.AddComponent<RectMask2D>();
        var boardFill = Ui.Image("BoardFill", _board, Color.white, Ui.Rounded);
        Ui.Stretch(boardFill.rectTransform);
        boardFill.color = new Color(1f, 1f, 1f, 0.35f);

        _tray = new TurnTray(play);
        _tray.Confirmed += () => Confirmed?.Invoke();
        _tray.QuarterTurnsSelected += turns => QuarterTurnsSelected?.Invoke(turns);
        _tray.PowerSelected += power => PowerSelected?.Invoke(power);

        // Last, so it draws over everything else.
        _claimsPanel = new ClaimsPanel(play);
        _seatBar.SeatTapped += _claimsPanel.Show;
    }

    public void Render(TableSnapshot snapshot)
    {
        Canvas.ForceUpdateCanvases();
        _heading.text = HeadingText(snapshot);
        _status.text = InstructionText(snapshot);
        _notice.text = NoticeText(snapshot.Notice);
        _notice.gameObject.SetActive(snapshot.Notice != TableNotice.None);
        _seatBar.Paint(snapshot.View);
        // What the panel shows may be out of date after any action, so a render closes it.
        _claimsPanel.Hide();
        _tray.Paint(snapshot, _heading.text);
        PaintBoard(snapshot);
    }

    // A band across the screen: top units from the top edge, height units tall, inset on both sides.
    private static void Band(RectTransform rect, float top, float height, float inset = 0f)
    {
        Ui.Anchored(rect, new Vector2(0f, 1f), Vector2.one, new Vector2(inset, -(top + height)), new Vector2(-inset, -top));
    }

    private static string HeadingText(TableSnapshot snapshot)
    {
        var status = snapshot.Status;
        return status.Kind switch
        {
            TableStatusKind.ToConfirm or TableStatusKind.ToAct => SeatBar.NameOf(status.Seat.Value) + "\u2019s turn!",
            TableStatusKind.Won => SeatBar.NameOf(status.Seat.Value) + " wins!",
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
                return "Hand the device to " + SeatBar.NameOf(status.Seat.Value) + ", then confirm.";
            case TableStatusKind.ToAct:
                return ActInstruction(snapshot);
            case TableStatusKind.Won:
                return OrdinaryCatalog.ClaimsRequiredToWin + " claims wins the game.";
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
            return power.Equals(OrdinaryCatalog.Rotate) && snapshot.QuarterTurns == 0
                ? "Spin to choose the turns, then pick a tile to rotate."
                : "Pick a tile to " + power.Value + ".";
        }

        return snapshot.View.RemainingUses.Any(charge => charge.Remaining > 0)
            ? "Place your tile, or use a power first."
            : "Place your tile.";
    }

    private static string NoticeText(TableNotice notice) =>
        notice == TableNotice.DeckReshuffled ? "The draw deck ran out; the board was reshuffled." : string.Empty;

    private void PaintBoard(TableSnapshot snapshot)
    {
        // Child 0 is the permanent board fill.
        Ui.Clear(_board, 1);

        var view = snapshot.View;
        // An emptied board still offers the origin, so only return when nothing is drawn or tappable.
        if (view.Board.Cells.Count == 0 && snapshot.LegalPlacements.Count == 0)
        {
            return;
        }

        var minX = int.MaxValue;
        var maxX = int.MinValue;
        var minY = int.MaxValue;
        var maxY = int.MinValue;
        foreach (var occupiedCell in view.Board.Cells)
        {
            minX = Math.Min(minX, occupiedCell.CellX);
            maxX = Math.Max(maxX, occupiedCell.CellX);
            minY = Math.Min(minY, occupiedCell.CellY);
            maxY = Math.Max(maxY, occupiedCell.CellY);
        }

        foreach (var highlight in snapshot.LegalPlacements)
        {
            minX = Math.Min(minX, highlight.TileX * 2);
            maxX = Math.Max(maxX, (highlight.TileX * 2) + 1);
            minY = Math.Min(minY, highlight.TileY * 2);
            maxY = Math.Max(maxY, (highlight.TileY * 2) + 1);
        }

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        var area = _board.rect;
        var cellSize = Math.Max(MinimumCellUnits, Math.Min(area.width / (width + 1), area.height / (height + 1)));

        var lattice = Ui.Rect("Lattice", _board);
        lattice.anchorMin = new Vector2(0.5f, 0.5f);
        lattice.anchorMax = new Vector2(0.5f, 0.5f);
        lattice.pivot = new Vector2(0.5f, 0.5f);
        lattice.sizeDelta = new Vector2(width * cellSize, height * cellSize);

        var cells = new Dictionary<(int X, int Y), Cell>();
        foreach (var occupied in view.Board.Cells)
        {
            cells[(occupied.CellX, occupied.CellY)] = occupied.Value;
        }

        foreach (var tile in view.Board.Tiles)
        {
            var block = Ui.Rect("Tile" + tile.Id.Value, lattice);
            Ui.PlaceCell(block, tile.TileX * 2, tile.TileY * 2, 2, 2, minX, minY, cellSize, 0f, 0f);
            TilePainter.Paint(block, tile.Id.Value, (x, y) => cells[((tile.TileX * 2) + x, (tile.TileY * 2) + y)], 0);
        }

        if (snapshot.ConcealVisible)
        {
            return;
        }

        // While a power is selected the snapshot offers no placements, so only its targets are drawn.
        // A rotate target waits for a 1–3 amount; Rules would refuse zero, so the tap is not offered until the row is set.
        var targetsLive = !(snapshot.SelectedPower is { } selected && selected.Equals(OrdinaryCatalog.Rotate) && snapshot.QuarterTurns == 0);
        foreach (var target in snapshot.SelectedTargets)
        {
            var button = AddTileButton(lattice, "Target" + target.TileX + "_" + target.TileY, target.TileX, target.TileY, SplatPalette.TargetHighlight, minX, minY, cellSize, () => TargetTapped?.Invoke(target.TileX, target.TileY));
            button.interactable = targetsLive;
        }

        foreach (var highlight in snapshot.LegalPlacements)
        {
            var tint = highlight.Kind == PlacementKind.OnTop ? SplatPalette.StackHighlight : SplatPalette.Highlight;
            AddTileButton(lattice, "Highlight" + highlight.TileX + "_" + highlight.TileY, highlight.TileX, highlight.TileY, tint, minX, minY, cellSize, () => Tapped?.Invoke(highlight.TileX, highlight.TileY));
        }
    }

    private static Button AddTileButton(RectTransform lattice, string name, int tileX, int tileY, Color tint, int minX, int minY, float cellSize, Action onTap)
    {
        var button = Ui.Button(name, lattice, string.Empty, tint);
        Ui.PlaceCell(button.GetComponent<RectTransform>(), tileX * 2, tileY * 2, 2, 2, minX, minY, cellSize, 0f, 0f);
        button.onClick.AddListener(() => onTap());
        return button;
    }
}
}
