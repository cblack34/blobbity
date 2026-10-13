namespace MissionSplat.Player
{

using System;
using System.Collections.Generic;
using MissionSplat.App;
using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

// The board: tiles on a lattice inside a scroll view. A board that fits is centred and stays put; one that does not
// scrolls, and keeps the cell at the middle of the view across renders so a placement does not throw the player elsewhere.
internal sealed class BoardPanel
{
    // A tile is two cells wide and is what a finger taps, so its minimum is the comfortable tap size: 120 units
    // on the 1080-wide canvas is about 44 pt on a phone.
    private const float MinimumTileUnits = 120f;

    private readonly RectTransform _viewport;
    private readonly ScrollRect _scroll;

    // Where the content was drawn last, to find which cell sits at the middle of the view.
    // _focus is that cell, kept until the player scrolls: a render that briefly shrinks the board (the hand-off hides the
    // placements) clamps the position, and must not lose where the player was looking.
    private RectTransform _content;
    private Vector2? _focus;
    private Vector2 _placedAt;
    private Vector2Int _origin;
    private Vector2 _latticeSize;
    private float _cellSize;

    public BoardPanel(RectTransform viewport)
    {
        _viewport = viewport;
        // The viewport clips what scrolls out of it.
        viewport.gameObject.AddComponent<RectMask2D>();
        // The fill takes raycasts so a drag that starts on empty board scrolls it.
        var fill = Ui.Image("BoardFill", viewport, new Color(1f, 1f, 1f, 0.35f), Ui.Rounded);
        Ui.Stretch(fill.rectTransform);
        fill.raycastTarget = true;
        _scroll = viewport.gameObject.AddComponent<ScrollRect>();
        _scroll.viewport = viewport;
        _scroll.horizontal = true;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.inertia = true;
    }

    public event Action<int, int> Tapped;

    public event Action<int, int> TargetTapped;

    public void Paint(TableSnapshot snapshot)
    {
        if (_content != null && _content.anchoredPosition != _placedAt)
        {
            _focus = CellAtViewCentre();
        }

        // Child 0 is the permanent board fill.
        Ui.Clear(_viewport, 1);
        _content = null;

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
        var area = _viewport.rect;
        var cellSize = Math.Max(MinimumTileUnits / 2f, Math.Min(area.width / (width + 1), area.height / (height + 1)));
        var latticeSize = new Vector2(width * cellSize, height * cellSize);

        // The content is the lattice plus half a cell all round, and never smaller than the view, so a board that fits sits in the middle.
        var content = Ui.Rect("Content", _viewport);
        content.sizeDelta = new Vector2(Math.Max(latticeSize.x + cellSize, area.width), Math.Max(latticeSize.y + cellSize, area.height));
        var lattice = Ui.Rect("Lattice", content);
        lattice.sizeDelta = latticeSize;

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

        _content = content;
        _origin = new Vector2Int(minX, minY);
        _latticeSize = latticeSize;
        _cellSize = cellSize;
        _scroll.content = content;
        _scroll.velocity = Vector2.zero;
        content.anchoredPosition = ScrolledTo(_focus, content.sizeDelta, area.size);
        _placedAt = content.anchoredPosition;

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

    // The board cell at the middle of the view as last drawn and scrolled.
    private Vector2 CellAtViewCentre()
    {
        var offset = _content.anchoredPosition;
        return new Vector2(
            _origin.x + ((-offset.x + (_latticeSize.x / 2f)) / _cellSize),
            _origin.y + ((-offset.y + (_latticeSize.y / 2f)) / _cellSize));
    }

    // The content position that puts the focus cell at the middle of the view, kept inside the content; centred with no focus.
    private Vector2 ScrolledTo(Vector2? focus, Vector2 contentSize, Vector2 viewSize)
    {
        if (focus is not { } cell)
        {
            return Vector2.zero;
        }

        var wanted = new Vector2(
            (_latticeSize.x / 2f) - ((cell.x - _origin.x) * _cellSize),
            (_latticeSize.y / 2f) - ((cell.y - _origin.y) * _cellSize));
        var slack = (contentSize - viewSize) / 2f;
        return new Vector2(Mathf.Clamp(wanted.x, -slack.x, slack.x), Mathf.Clamp(wanted.y, -slack.y, slack.y));
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
