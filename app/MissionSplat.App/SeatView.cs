namespace MissionSplat.App;

using MissionSplat.Rules;

public sealed class SeatView
{
    public SeatView(
        SeatId seat,
        IReadOnlyList<BlobbityCard> unclaimedBlobbities,
        IReadOnlyList<SeatClaims> claims,
        BoardView board,
        SeatId currentSeat,
        bool hasEnded,
        Tile? pendingMatchTile,
        IReadOnlyList<PowerCharge> remainingUses,
        bool awaitsReshuffle,
        IReadOnlyList<TileId> tilesToReshuffle)
    {
        Seat = seat;
        UnclaimedBlobbities = Copied.Items(unclaimedBlobbities, nameof(unclaimedBlobbities));
        Claims = Copied.Items(claims, nameof(claims));
        Board = board ?? throw new ArgumentNullException(nameof(board));
        CurrentSeat = currentSeat;
        HasEnded = hasEnded;
        PendingMatchTile = pendingMatchTile;
        RemainingUses = Copied.List(remainingUses, nameof(remainingUses));
        AwaitsReshuffle = awaitsReshuffle;
        TilesToReshuffle = Copied.List(tilesToReshuffle, nameof(tilesToReshuffle));
    }

    public SeatId Seat { get; }

    public IReadOnlyList<BlobbityCard> UnclaimedBlobbities { get; }

    public IReadOnlyList<SeatClaims> Claims { get; }

    public BoardView Board { get; }

    public SeatId CurrentSeat { get; }

    public bool HasEnded { get; }

    public Tile? PendingMatchTile { get; }

    // One entry per use power the setup lists, in setup order; zero once the drawn tile's cells of it are spent.
    public IReadOnlyList<PowerCharge> RemainingUses { get; }

    // True while the draw deck is empty and only a board reshuffle is accepted.
    public bool AwaitsReshuffle { get; }

    // Every board tile, buried ones included, a reshuffle order must hold once; empty unless awaiting.
    public IReadOnlyList<TileId> TilesToReshuffle { get; }
}

public sealed class SeatClaims
{
    public SeatClaims(SeatId seat, IReadOnlyList<BlobbityCard> blobbities)
    {
        Seat = seat;
        Blobbities = Copied.Items(blobbities, nameof(blobbities));
    }

    public SeatId Seat { get; }

    public IReadOnlyList<BlobbityCard> Blobbities { get; }
}

public sealed class BoardView
{
    public BoardView(IReadOnlyList<OccupiedTile> tiles, IReadOnlyList<OccupiedCell> cells)
    {
        Tiles = Copied.List(tiles, nameof(tiles));
        Cells = Copied.List(cells, nameof(cells));
    }

    public IReadOnlyList<OccupiedTile> Tiles { get; }

    public IReadOnlyList<OccupiedCell> Cells { get; }

    // The four cells showing at a tile position, empty when no tile is there.
    public OccupiedCell[] CellsOf(int tileX, int tileY)
    {
        var found = new List<OccupiedCell>(4);
        foreach (var cell in Cells)
        {
            if (cell.CellX >= tileX * 2 && cell.CellX < (tileX * 2) + 2 && cell.CellY >= tileY * 2 && cell.CellY < (tileY * 2) + 2)
            {
                found.Add(cell);
            }
        }

        return found.ToArray();
    }
}

public readonly record struct OccupiedTile(TileId Id, int TileX, int TileY);

public readonly record struct OccupiedCell(int CellX, int CellY, Cell Value);

public readonly record struct PowerCharge(SymbolId Power, int Remaining);
