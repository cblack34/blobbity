namespace MissionSplat.Player
{

using System;
using System.Collections.Generic;
using System.Linq;
using Blobbity.App;
using Blobbity.Rules;
using UnityEngine;
using UnityEngine.UI;

// The board: tiles on a lattice inside a scroll view. A board that fits is centred and stays put; one that does not
// scrolls, and keeps the cell at the middle of the view across renders so a placement does not throw the player elsewhere.
internal sealed class BoardPanel
{
    // A tile is two cells wide and is what a finger taps, so its minimum is the comfortable tap size: 120 units
    // on the canvas's 1080-unit reference width is about 44 pt on a phone.
    private const float MinimumTileUnits = 120f;

    // The previewed or selected tile is drawn 1.1 times as big as the spot it is tried at, and raised 0.18 of a cell.
    private const float PreviewScale = 1.1f;
    private const float PreviewLift = 0.18f;

    private readonly RectTransform _viewport;
    private readonly ScrollRect _scroll;

    // _focus is the board cell at the middle of the view, kept until the player scrolls: a render that briefly shrinks
    // the board (the hand-off hides the placements) clamps the position, and must not lose where the player was looking.
    // _placedAt is where the last render left the content, so a different position means the player scrolled.
    private Vector2? _focus;
    private Vector2 _placedAt;

    // The content as last drawn: the lattice's origin cell, its size, and its cell size, to turn a scroll position into a cell.
    private RectTransform _content;
    private RectTransform _lattice;
    private Vector2Int _origin;
    private Vector2 _latticeSize;
    private float _cellSize;

    public BoardPanel(RectTransform viewport)
    {
        _viewport = viewport;
        // The viewport clips what scrolls out of it.
        viewport.gameObject.AddComponent<RectMask2D>();
        // The fill is clear, so the background shows through, but it takes raycasts so a drag that starts on empty board scrolls it.
        var fill = Ui.Image("BoardFill", viewport, Color.clear, Ui.Rounded);
        Ui.Stretch(fill.rectTransform);
        fill.raycastTarget = true;
        _scroll = viewport.gameObject.AddComponent<ScrollRect>();
        _scroll.viewport = viewport;
        _scroll.horizontal = true;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.inertia = true;
    }

    // A tap on a legal spot or on a power target; the owner decides what it starts.
    public event Action<int, int> TileTapped;

    // A new game starts centred, not where the last one was scrolled to.
    public void ForgetScroll()
    {
        _content = null;
        _lattice = null;
        _focus = null;
        _placedAt = Vector2.zero;
    }

    // The caller runs Canvas.ForceUpdateCanvases() first: the viewport's size is read here.
    // preview is what is being tried, if anything: the drawn tile at a spot, or a board tile selected for a power. It is drawn there, lifted, with a ring,
    // instead of that tile's or spot's tap target.
    // bottomInset is the height of what covers the board's bottom edge: the content gets that much slack below (and above, to stay centred),
    // so the player can scroll the bottom row clear of it.
    public void Paint(TableSnapshot snapshot, TilePreview preview = null, float bottomInset = 0f)
    {
        if (_content != null && _content.anchoredPosition != _placedAt)
        {
            _focus = CellAtViewCentre();
        }

        // Child 0 is the permanent board fill.
        Ui.Clear(_viewport, 1);
        _content = null;
        _lattice = null;

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

        void Include(int tileX, int tileY)
        {
            minX = Math.Min(minX, tileX * 2);
            maxX = Math.Max(maxX, (tileX * 2) + 1);
            minY = Math.Min(minY, tileY * 2);
            maxY = Math.Max(maxY, (tileY * 2) + 1);
        }

        foreach (var highlight in snapshot.LegalPlacements)
        {
            Include(highlight.TileX, highlight.TileY);
        }

        // A pending recap plays on this board as it now is, but its items sit where the tiles were: a bounce or a reshuffle can leave
        // one outside what is drawn. The lattice reaches them too, so the board can scroll to each and its animation is seen.
        foreach (var item in snapshot.Recap)
        {
            switch (item)
            {
                case RecapPlaced placed: Include(placed.TileX, placed.TileY); break;
                case RecapRotated rotated: Include(rotated.TileX, rotated.TileY); break;
                case RecapBounced bounced: Include(bounced.TileX, bounced.TileY); break;
            }
        }

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        var area = _viewport.rect;
        var cellSize = Math.Max(MinimumTileUnits / 2f, Math.Min(area.width / (width + 1), area.height / (height + 1)));
        var latticeSize = new Vector2(width * cellSize, height * cellSize);

        // The content is the lattice plus half a cell all round, and never smaller than the view, so a board that fits sits in the middle.
        var content = Ui.Rect("Content", _viewport);
        content.sizeDelta = new Vector2(Math.Max(latticeSize.x + cellSize, area.width), Math.Max(latticeSize.y + cellSize, area.height) + (2f * bottomInset));
        var lattice = Ui.Rect("Lattice", content);
        lattice.sizeDelta = latticeSize;

        var cells = new Dictionary<(int X, int Y), Cell>();
        foreach (var occupied in view.Board.Cells)
        {
            cells[(occupied.CellX, occupied.CellY)] = occupied.Value;
        }

        // A tile selected for a power is drawn lifted, by PaintPreview, instead.
        foreach (var tile in view.Board.Tiles)
        {
            if (preview?.SelectsBoardTile(tile.TileX, tile.TileY) == true)
            {
                continue;
            }

            var block = Ui.Rect("Tile" + tile.Id.Value, lattice);
            Ui.PlaceCell(block, tile.TileX * 2, tile.TileY * 2, 2, 2, minX, minY, cellSize, 0f, 0f);
            TilePainter.Paint(block, tile.Id.Value, CellsOf(cells, tile.TileX, tile.TileY), 0);
        }
        _content = content;
        _lattice = lattice;
        _origin = new Vector2Int(minX, minY);
        _latticeSize = latticeSize;
        _cellSize = cellSize;
        _scroll.content = content;
        _scroll.velocity = Vector2.zero;
        content.anchoredPosition = ScrolledTo(_focus, content.sizeDelta, area.size);
        _placedAt = content.anchoredPosition;

        if (!snapshot.ConcealVisible)
        {
            PaintTapTargets(lattice, snapshot, preview, minX, minY, cellSize);
        }

        // Last, so no neighbour's tap target or spot draws over the lifted tile.
        PaintPreview(lattice, snapshot, preview, cells, minX, minY, cellSize);
    }

    // The cell at local (x, y) of the tile at (tileX, tileY), as the board holds it.
    private static Func<int, int, Cell> CellsOf(Dictionary<(int X, int Y), Cell> cells, int tileX, int tileY) =>
        (x, y) => cells[((tileX * 2) + x, (tileY * 2) + y)];

    // A ring and a tap on each tile the drawn tile's power can act on, so the player can see what is worth a tap, and a tap target on each legal spot.
    // What is being previewed shows the tile itself instead.
    private void PaintTapTargets(RectTransform lattice, TableSnapshot snapshot, TilePreview preview, int minX, int minY, float cellSize)
    {
        var targets = TilePreview.PowerTargets(snapshot).ToHashSet();
        foreach (var target in targets.Where(target => preview?.IsAt(target.TileX, target.TileY) != true))
        {
            var button = AddTileButton(lattice, "Target" + target.TileX + "_" + target.TileY, target.TileX, target.TileY, minX, minY, cellSize, () => TileTapped?.Invoke(target.TileX, target.TileY));
            PaintRing(button.transform, SplatPalette.TargetRing, cellSize);
        }

        // A power target is already tappable above; a tap there selects the power.
        foreach (var highlight in snapshot.LegalPlacements.Where(spot => preview?.IsAt(spot.TileX, spot.TileY) != true && !targets.Contains(new BoardPosition(spot.TileX, spot.TileY))))
        {
            var button = AddTileButton(lattice, "Highlight" + highlight.TileX + "_" + highlight.TileY, highlight.TileX, highlight.TileY, minX, minY, cellSize, () => TileTapped?.Invoke(highlight.TileX, highlight.TileY));
            // A stack target is a tile to cover, so it gets a ring around the tile; any other placement is an empty spot to fill.
            if (highlight.Kind == PlacementKind.OnTop)
            {
                PaintRing(button.transform, SplatPalette.StackRing, cellSize);
            }
            else
            {
                // The sprite carries its own colours, so it is drawn untinted.
                var spot = Ui.Image("Spot", button.transform, Color.white, Ui.LegalSpot);
                Ui.Stretch(spot.rectTransform);
            }
        }
    }

    // The previewed tile, lifted: the drawn tile at its spot as Spin has it, or the selected board tile as the player has turned it.
    private static void PaintPreview(RectTransform lattice, TableSnapshot snapshot, TilePreview preview, Dictionary<(int X, int Y), Cell> cells, int minX, int minY, float cellSize)
    {
        switch (preview?.Kind)
        {
            case null:
                return;
            case PreviewKind.Placement:
                if (snapshot.View.PendingMatchTile is { } drawn)
                {
                    PaintLifted(lattice, drawn.Id.Value, drawn.Local, snapshot.QuarterTurns, preview, minX, minY, cellSize);
                }

                return;
            case PreviewKind.Rotate:
            case PreviewKind.Bounce:
                foreach (var tile in snapshot.View.Board.Tiles.Where(tile => preview.SelectsBoardTile(tile.TileX, tile.TileY)))
                {
                    PaintLifted(lattice, tile.Id.Value, CellsOf(cells, tile.TileX, tile.TileY), preview.Turns, preview, minX, minY, cellSize);
                }

                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(preview), preview.Kind, "Unknown kind of preview.");
        }
    }

    // Where the board last drew the tile at (tileX, tileY): its centre in world space and its side in canvas units. The position need not hold a tile,
    // and may be scrolled out of view. Null when nothing is drawn.
    public (Vector3 Centre, float Side)? TileSlot(int tileX, int tileY)
    {
        if (_lattice == null)
        {
            return null;
        }

        var local = _lattice.rect.min + new Vector2(((tileX * 2) - _origin.x + 1) * _cellSize, ((tileY * 2) - _origin.y + 1) * _cellSize);
        return (_lattice.TransformPoint(local), 2f * _cellSize);
    }

    // Scrolls the board so the tile at (tileX, tileY) is wholly in view, if it is not. The scroll stays where it ends: the next render keeps
    // whatever is then at the middle of the view, as for any scroll by the player. bottomInset is the height of what covers the view's bottom
    // edge: the tile must clear it too, and is centred in the view above it.
    public void RevealTile(int tileX, int tileY, float bottomInset = 0f)
    {
        if (TileSlot(tileX, tileY) is not { } slot)
        {
            return;
        }

        var local = (Vector2)_viewport.InverseTransformPoint(slot.Centre);
        var reach = (_viewport.rect.size / 2f) - new Vector2(slot.Side / 2f, slot.Side / 2f);
        if (Mathf.Abs(local.x) <= reach.x && local.y <= reach.y && local.y >= -reach.y + bottomInset)
        {
            return;
        }

        _content.anchoredPosition = ScrolledTo(new Vector2((tileX * 2) + 1, (tileY * 2) + 1), _content.sizeDelta, _viewport.rect.size, bottomInset / 2f);
        _scroll.velocity = Vector2.zero;
    }

    // The board cell at the middle of the view as last drawn and scrolled.
    private Vector2 CellAtViewCentre()
    {
        var offset = _content.anchoredPosition;
        return new Vector2(
            _origin.x + ((-offset.x + (_latticeSize.x / 2f)) / _cellSize),
            _origin.y + ((-offset.y + (_latticeSize.y / 2f)) / _cellSize));
    }

    // The content position that puts the focus cell at the middle of the view (lift above it), kept inside the content; centred with no focus.
    private Vector2 ScrolledTo(Vector2? focus, Vector2 contentSize, Vector2 viewSize, float lift = 0f)
    {
        if (focus is not { } cell)
        {
            return Vector2.zero;
        }

        var wanted = new Vector2(
            (_latticeSize.x / 2f) - ((cell.x - _origin.x) * _cellSize),
            (_latticeSize.y / 2f) - ((cell.y - _origin.y) * _cellSize) + lift);
        var slack = (contentSize - viewSize) / 2f;
        return new Vector2(Mathf.Clamp(wanted.x, -slack.x, slack.x), Mathf.Clamp(wanted.y, -slack.y, slack.y));
    }

    // A tile at the spot it is being tried at or selected at, turned as given: bigger and raised, with a soft green ring, and over a tile it would stack on.
    // It takes no taps; the strip decides.
    private static void PaintLifted(RectTransform lattice, string tileId, Func<int, int, Cell> cellAt, int quarterTurns, TilePreview spot, int minX, int minY, float cellSize)
    {
        var block = Ui.Rect("Preview", lattice);
        Ui.PlaceCell(block, spot.TileX * 2, spot.TileY * 2, 2, 2, minX, minY, cellSize, 0f, 0f);
        block.pivot = new Vector2(0.5f, 0.5f);
        block.anchoredPosition += new Vector2(cellSize, cellSize + (PreviewLift * cellSize));
        block.localScale = new Vector3(PreviewScale, PreviewScale, 1f);
        TilePainter.Paint(block, tileId, cellAt, quarterTurns);
        // After the tile, so its hard shadow does not hide the ring's lower and right sides.
        PaintRing(block, SplatPalette.Go, cellSize);
    }

    // A tap target over a whole tile's 2 x 2 block, which is where the card's gap lets a tap through too; the caller draws into it.
    private static Button AddTileButton(RectTransform lattice, string name, int tileX, int tileY, int minX, int minY, float cellSize, Action onTap)
    {
        var button = Ui.Tappable(Ui.Image(name, lattice, Color.clear));
        Ui.PlaceCell(button.GetComponent<RectTransform>(), tileX * 2, tileY * 2, 2, 2, minX, minY, cellSize, 0f, 0f);
        button.onClick.AddListener(() => onTap());
        return button;
    }

    // A ring of fixed thickness just outside the tile's card, inside the block, so rings of neighbouring tiles stay apart.
    private static void PaintRing(Transform tile, Color tint, float cellSize)
    {
        var margin = Mathf.Max(0f, CardPainter.InsetUnits(2f * cellSize) - Ui.RingUnits);
        var ring = Ui.Image("Ring", tile, tint, Ui.TileRing);
        Ui.Anchored(ring.rectTransform, Vector2.zero, Vector2.one, new Vector2(margin, margin), new Vector2(-margin, -margin));
    }
}
}
