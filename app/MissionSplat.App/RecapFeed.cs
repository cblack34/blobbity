namespace MissionSplat.App;

using MissionSplat.Rules;

// The table's ordered log of public events and what each human seat has already seen of it.
internal sealed class RecapFeed(GameSetup setup)
{
    private readonly GameSetup _setup = setup ?? throw new ArgumentNullException(nameof(setup));
    private readonly List<RecapItem> _log = [];
    private readonly Dictionary<SeatId, int> _seen = [];

    public void Reset()
    {
        _log.Clear();
        _seen.Clear();
    }

    // before and after are the board either side of the result, so a lifted tile's cells can still be reported.
    public void Record(IReadOnlyList<GameEvent> events, BoardView before, BoardView after)
    {
        foreach (var gameEvent in events)
        {
            _log.Add(ItemFor(gameEvent, before, after));
        }
    }

    public RecapItem[] PendingFor(SeatId? seat)
    {
        if (seat is not { } viewer)
        {
            return [];
        }

        var from = SeenBy(viewer);
        var pending = new RecapItem[_log.Count - from];
        _log.CopyTo(from, pending, 0, pending.Length);
        return pending;
    }

    // True when the seat had something pending.
    public bool Acknowledge(SeatId seat)
    {
        if (SeenBy(seat) == _log.Count)
        {
            return false;
        }

        _seen[seat] = _log.Count;
        return true;
    }

    private int SeenBy(SeatId seat) => _seen.TryGetValue(seat, out var count) ? count : 0;

    private RecapItem ItemFor(GameEvent gameEvent, BoardView before, BoardView after) => gameEvent switch
    {
        TilePlaced placed => new RecapPlaced(
            placed.Seat,
            placed.Tile,
            placed.TileX,
            placed.TileY,
            placed.QuarterTurnsClockwise,
            after.CellsOf(placed.TileX, placed.TileY),
            placed.Covered is not null),
        TileRotated rotated => new RecapRotated(
            rotated.Seat,
            rotated.TileX,
            rotated.TileY,
            rotated.QuarterTurnsClockwise,
            after.CellsOf(rotated.TileX, rotated.TileY)),
        TileBounced bounced => new RecapBounced(
            bounced.Seat,
            bounced.TileX,
            bounced.TileY,
            before.CellsOf(bounced.TileX, bounced.TileY),
            bounced.Revealed is null ? [] : after.CellsOf(bounced.TileX, bounced.TileY)),
        BlobbityClaimed claimed => new RecapClaimed(claimed.Seat, MissionOf(claimed.Mission)),
        GameWon won => new RecapWon(won.Seat, won.ClaimCount),
        BoardReshuffled reshuffled => new RecapReshuffled(reshuffled.Seat),
        _ => throw new InvalidOperationException("The recap has no item for " + gameEvent.GetType().Name + "."),
    };

    private Mission MissionOf(MissionId id)
    {
        foreach (var mission in _setup.MissionDeck)
        {
            if (mission.Id.Equals(id))
            {
                return mission;
            }
        }

        throw new InvalidOperationException("A claimed mission is not in the setup's deck.");
    }
}
