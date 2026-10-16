namespace MissionSplat.App;

using MissionSplat.Rules;

// One public fact a human missed since they last held the device. The set is closed: only this assembly can derive,
// so a GUI can switch over every kind. Nothing here names a hidden mission, a buried tile, or the draw order.
public abstract record RecapItem
{
    private protected RecapItem(SeatId seat)
    {
        Seat = seat;
    }

    public SeatId Seat { get; }
}

// A tile put on the board. Cells are its four board cells as placed, so a GUI draws it without turning it again.
public sealed record RecapPlaced : RecapItem
{
    public RecapPlaced(
        SeatId seat,
        TileId tile,
        int tileX,
        int tileY,
        int quarterTurnsClockwise,
        IReadOnlyList<OccupiedCell> cells,
        bool covered)
        : base(seat)
    {
        Tile = tile;
        TileX = tileX;
        TileY = tileY;
        QuarterTurnsClockwise = quarterTurnsClockwise;
        Cells = Copied.List(cells, nameof(cells));
        Covered = covered;
    }

    public TileId Tile { get; }

    public int TileX { get; }

    public int TileY { get; }

    public int QuarterTurnsClockwise { get; }

    public IReadOnlyList<OccupiedCell> Cells { get; }

    public bool Covered { get; }
}

// A tile turned in place. Cells are its four board cells after the turn.
public sealed record RecapRotated : RecapItem
{
    public RecapRotated(SeatId seat, int tileX, int tileY, int quarterTurnsClockwise, IReadOnlyList<OccupiedCell> cells)
        : base(seat)
    {
        TileX = tileX;
        TileY = tileY;
        QuarterTurnsClockwise = quarterTurnsClockwise;
        Cells = Copied.List(cells, nameof(cells));
    }

    public int TileX { get; }

    public int TileY { get; }

    public int QuarterTurnsClockwise { get; }

    public IReadOnlyList<OccupiedCell> Cells { get; }
}

// A tile lifted off. RemovedCells are the lifted tile's cells before the bounce; RevealedCells are the tile
// the bounce uncovered at that position, empty when none. Neither names a tile.
public sealed record RecapBounced : RecapItem
{
    public RecapBounced(
        SeatId seat,
        int tileX,
        int tileY,
        IReadOnlyList<OccupiedCell> removedCells,
        IReadOnlyList<OccupiedCell> revealedCells)
        : base(seat)
    {
        TileX = tileX;
        TileY = tileY;
        RemovedCells = Copied.List(removedCells, nameof(removedCells));
        RevealedCells = Copied.List(revealedCells, nameof(revealedCells));
    }

    public int TileX { get; }

    public int TileY { get; }

    public IReadOnlyList<OccupiedCell> RemovedCells { get; }

    public IReadOnlyList<OccupiedCell> RevealedCells { get; }
}

// A claimed mission is public, so the card itself rides along.
public sealed record RecapClaimed(SeatId Seat, BlobbityCard Mission) : RecapItem(Seat);

public sealed record RecapWon(SeatId Seat, int ClaimCount) : RecapItem(Seat);

// The board was reshuffled; the new order is deliberately absent.
public sealed record RecapReshuffled(SeatId Seat) : RecapItem(Seat);
