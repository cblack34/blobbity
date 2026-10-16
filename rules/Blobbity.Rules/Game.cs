namespace Blobbity.Rules;

public sealed class Game
{
    private readonly SeatSnapshot[] _seats;
    private readonly int _currentIndex;
    private readonly BlobbityCard[] _blobbityDeck;
    private readonly Tile[] _matchDeck;
    private readonly Grid _grid;
    private readonly Ruleset _ruleset;
    private readonly SpentUses _spent;
    private readonly bool _hasEnded;

    private Game(
        SeatSnapshot[] seats,
        int currentIndex,
        BlobbityCard[] blobbityDeck,
        Tile[] matchDeck,
        Grid grid,
        Ruleset ruleset,
        int claimsRequiredToWin,
        SpentUses spent,
        bool hasEnded)
    {
        _seats = seats;
        _currentIndex = currentIndex;
        _blobbityDeck = blobbityDeck;
        _matchDeck = matchDeck;
        _grid = grid;
        _ruleset = ruleset;
        ClaimsRequiredToWin = claimsRequiredToWin;
        _spent = spent;
        _hasEnded = hasEnded;
    }

    public SeatId CurrentSeat => _seats[_currentIndex].Id;

    public bool HasEnded => _hasEnded;

    public int ClaimsRequiredToWin { get; }

    public int TileCount => _grid.TileCount;

    public int BlobbityDeckRemaining => _blobbityDeck.Length;

    public int MatchDeckRemaining => _matchDeck.Length;

    // The drawn tile: the one Apply spends powers from and the next Place consumes. Null when the game has
    // ended or the match awaits a reshuffle.
    public Tile? PendingMatchTile => _hasEnded || AwaitsReshuffle ? null : _matchDeck[0];

    // True once a placement empties the draw deck without winning, or a setup supplies no tile beyond the start tile:
    // only ReshuffleBoard is accepted until it is played.
    public bool AwaitsReshuffle => !_hasEnded && _matchDeck.Length == 0;

    // Every board tile, visible and buried, that a ReshuffleBoard order must contain exactly once.
    public IReadOnlyList<TileId> TilesToReshuffle => AwaitsReshuffle ? IdsOf(_grid.AllTiles()) : [];

    public IReadOnlyList<SeatId> SeatsInTurnOrder
    {
        get
        {
            var seats = new SeatId[_seats.Length];
            for (var i = 0; i < _seats.Length; i++)
            {
                seats[i] = _seats[i].Id;
            }

            return seats;
        }
    }

    // The powers this game's setup lists, so a GUI can refuse a game it cannot present.
    public IReadOnlyList<SymbolId> PowersInPlay => _ruleset.PowersInPlay;

    public static CommandResult Start(GameSetup setup)
    {
        if (setup is null)
        {
            throw new ArgumentNullException(nameof(setup));
        }

        var rejection = SetupCheck.Find(setup);
        if (rejection is not null)
        {
            return CommandResult.Reject(null, rejection);
        }

        return CommandResult.Accept(Deal(setup), []);
    }

    public CommandResult Apply(SeatId seat, GameAction action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (_hasEnded)
        {
            return Reject(RejectionReason.GameOver, "The game has already been won.");
        }

        if (!seat.Equals(CurrentSeat))
        {
            return Reject(RejectionReason.NotYourTurn, "It is not that seat's turn.");
        }

        if (AwaitsReshuffle != (action is ReshuffleBoard))
        {
            return AwaitsReshuffle
                ? Reject(RejectionReason.AwaitingReshuffle, "The draw deck is empty; the board must be reshuffled.")
                : Reject(RejectionReason.NotAwaitingReshuffle, "The board is reshuffled only when the draw deck is empty.");
        }

        if (PendingMatchTile is { } drawn)
        {
            RejectOpenRulings(drawn);
        }

        return action switch
        {
            ReshuffleBoard reshuffle => ApplyReshuffle(reshuffle),
            UseRotate rotate => ApplyRotate(rotate),
            UseBounce bounce => ApplyBounce(bounce),
            Place place => ApplyPlace(place),
            _ => throw new ArgumentException("That action is not part of the closed action set.", nameof(action)),
        };
    }

    // Read-only and total: no drawn tile, an ended game, quarter-turns outside 0..3, or a tile showing two different
    // powers (an open ruling) yield nothing, never an exception.
    public IReadOnlyList<LegalPlacement> LegalPlacements(int quarterTurnsClockwise)
    {
        if (QueryTile is not { } tile || !IsOrientation(quarterTurnsClockwise))
        {
            return [];
        }

        return _ruleset.LegalPlacements(_grid, tile);
    }

    public IReadOnlyList<BoardPosition> LegalTargets(SymbolId power)
    {
        if (QueryTile is not { } tile || !OrdinaryCatalog.IsUsePower(power))
        {
            return [];
        }

        return _ruleset.LegalTargets(_grid, tile, power, _spent);
    }

    // Stack is not counted: it is no separate use, only the on-top option inside a placement.
    public int RemainingUses(SymbolId power) =>
        QueryTile is { } tile && OrdinaryCatalog.IsUsePower(power) ? _ruleset.Remaining(tile, power, _spent) : 0;

    // The top tile at each occupied position, ordered by X then Y; a covered tile is not listed.
    public IReadOnlyList<VisibleTile> Tiles
    {
        get
        {
            var positions = BoardOrder.SortedPositions(_grid.TilePositions);
            var tiles = new List<VisibleTile>(positions.Count);
            foreach (var position in positions)
            {
                var tile = _grid.TileAt(position.TileX, position.TileY)
                    ?? throw new InvalidOperationException("A listed board position holds no tile.");
                tiles.Add(new VisibleTile(tile.Id, position.TileX, position.TileY));
            }

            return tiles;
        }
    }

    public IReadOnlyList<BlobbityCard> Hand(SeatId seat) => Copy(Find(seat).Hand);

    public IReadOnlyList<BlobbityCard> Claims(SeatId seat) => Copy(Find(seat).Claims);

    public Cell? CellAt(int cellX, int cellY) => _grid.At(cellX, cellY);

    public bool HasTileAt(int tileX, int tileY) => _grid.HasTile(tileX, tileY);

    public IReadOnlyList<TileId> CoveredTileIds(int tileX, int tileY) => _grid.CoveredTileIds(tileX, tileY);

    private CommandResult ResolvePlacement(
        Tile tile,
        Place place,
        Grid nextGrid,
        IReadOnlyList<(int X, int Y, Cell Value)> located,
        TileId? covered)
    {
        var resolution = ResolveClaims(nextGrid, located);
        var events = new List<GameEvent>
        {
            new TilePlaced(
                CurrentSeat,
                tile.Id,
                place.TileX,
                place.TileY,
                place.QuarterTurnsClockwise,
                covered),
        };
        AddClaimEvents(events, resolution);

        var next = Next(
            resolution.ActingSeat,
            (_currentIndex + 1) % _seats.Length,
            resolution.BlobbityDeck,
            _matchDeck[1..],
            nextGrid,
            default,
            resolution.Won);
        return CommandResult.Accept(next, events);
    }

    // The one claim pass, shared by a placement and a rotate: the acting seat claims each blobbity its hand completes
    // through the cells just written, draws a replacement per claim, and wins on reaching the claim count. It changes
    // nothing itself; the caller applies the resolution and appends its events.
    private ClaimResolution ResolveClaims(Grid grid, IReadOnlyList<(int X, int Y, Cell Value)> written)
    {
        var acting = _seats[_currentIndex];
        var completed = _ruleset.CompletedBlobbities(grid, written, acting.Hand);
        if (completed.Count > _blobbityDeck.Length)
        {
            throw new UnresolvedRulingException(
                "The blobbity deck cannot replace every blobbity this command completed. Exhausting the blobbity deck is an open ruling, so this command was not applied.");
        }

        var hand = new List<BlobbityCard>(acting.Hand);
        var claims = new List<BlobbityCard>(acting.Claims);
        var blobbityDeck = new List<BlobbityCard>(_blobbityDeck);
        foreach (var blobbity in completed)
        {
            hand.Remove(blobbity);
            var replacement = blobbityDeck[0];
            blobbityDeck.RemoveAt(0);
            hand.Add(replacement);
            claims.Add(blobbity);
        }

        return new ClaimResolution(
            completed,
            new SeatSnapshot(acting.Id, hand.ToArray(), claims.ToArray()),
            blobbityDeck.ToArray(),
            claims.Count >= ClaimsRequiredToWin);
    }

    private static void AddClaimEvents(List<GameEvent> events, ClaimResolution resolution)
    {
        foreach (var blobbity in resolution.Claimed)
        {
            events.Add(new BlobbityClaimed(resolution.ActingSeat.Id, blobbity.Id));
        }

        if (resolution.Won)
        {
            events.Add(new GameWon(resolution.ActingSeat.Id, resolution.ActingSeat.Claims.Length));
        }
    }

    // The next match: the acting seat replaced, every other seat shared, the board and decks as given.
    private Game Next(
        SeatSnapshot actingSeat,
        int currentIndex,
        BlobbityCard[] blobbityDeck,
        Tile[] matchDeck,
        Grid grid,
        SpentUses spent,
        bool hasEnded)
    {
        var seats = (SeatSnapshot[])_seats.Clone();
        seats[_currentIndex] = actingSeat;
        return new Game(
            seats,
            currentIndex,
            blobbityDeck,
            matchDeck,
            grid,
            _ruleset,
            ClaimsRequiredToWin,
            spent,
            hasEnded);
    }

    private CommandResult ApplyRotate(UseRotate rotate)
    {
        if (rotate.QuarterTurnsClockwise is < 1 or > 3)
        {
            return Reject(RejectionReason.InvalidRotateQuarterTurns, "A rotate use is 1, 2, or 3 quarter-turns.");
        }

        var drawn = DrawnTile();
        var refusal = _ruleset.UseRefusal(
            _grid,
            drawn,
            OrdinaryCatalog.Rotate,
            _spent,
            rotate.TileX,
            rotate.TileY);
        if (refusal is not null)
        {
            return CommandResult.Reject(this, refusal);
        }

        var grid = _ruleset.Rotate(_grid, rotate);

        // A rotate writes its tile's four cells, so they can claim at once. It does not end the turn: the seat and
        // the drawn tile stay, and a win ends the game before that tile is placed.
        var resolution = ResolveClaims(grid, grid.CellsOfTile(rotate.TileX, rotate.TileY));
        var events = new List<GameEvent>
        {
            new TileRotated(CurrentSeat, rotate.TileX, rotate.TileY, rotate.QuarterTurnsClockwise),
        };
        AddClaimEvents(events, resolution);

        var next = Next(
            resolution.ActingSeat,
            _currentIndex,
            resolution.BlobbityDeck,
            _matchDeck,
            grid,
            _spent.WithUse(OrdinaryCatalog.Rotate),
            resolution.Won);
        return CommandResult.Accept(next, events);
    }

    private CommandResult ApplyBounce(UseBounce bounce)
    {
        var drawn = DrawnTile();
        var refusal = _ruleset.UseRefusal(
            _grid,
            drawn,
            OrdinaryCatalog.Bounce,
            _spent,
            bounce.TileX,
            bounce.TileY);
        if (refusal is not null)
        {
            return CommandResult.Reject(this, refusal);
        }

        var (grid, removed, revealed) = _ruleset.Bounce(_grid, bounce);

        var matchDeck = new Tile[_matchDeck.Length + 1];
        Array.Copy(_matchDeck, matchDeck, _matchDeck.Length);
        matchDeck[^1] = removed;

        var next = Next(_seats[_currentIndex], _currentIndex, _blobbityDeck, matchDeck, grid, _spent.WithUse(OrdinaryCatalog.Bounce), _hasEnded);
        return CommandResult.Accept(
            next,
            [new TileBounced(CurrentSeat, bounce.TileX, bounce.TileY, removed.Id, revealed)]);
    }

    private CommandResult ApplyPlace(Place place)
    {
        if (!IsOrientation(place.QuarterTurnsClockwise))
        {
            return Reject(RejectionReason.InvalidQuarterTurns, "Quarter-turns are 0, 1, 2, or 3.");
        }

        var tile = DrawnTile();
        var refusal = _ruleset.PlacementRefusal(_grid, tile, place.TileX, place.TileY);
        if (refusal is not null)
        {
            return CommandResult.Reject(this, refusal);
        }

        var (grid, located, covered) = _ruleset.Place(_grid, tile, place);

        return ResolvePlacement(tile, place, grid, located, covered);
    }

    private static bool IsOrientation(int quarterTurns) => quarterTurns is >= 0 and <= 3;

    // The drawn tile as the queries see it: none when a tile shows two different powers, an open ruling Apply refuses.
    private Tile? QueryTile => PendingMatchTile is { } tile && !_ruleset.ShowsMixedPowers(tile) ? tile : null;

    // docs/rules.md leaves a tile showing two different powers open, so no command is applied while one is drawn.
    private void RejectOpenRulings(Tile tile)
    {
        if (_ruleset.ShowsMixedPowers(tile))
        {
            throw new UnresolvedRulingException(
                "The drawn tile shows more than one power. A tile showing two different powers is an open ruling, so this command was not applied.");
        }
    }

    // Apply routes an empty deck to the reshuffle, so every action that reaches a tile finds one.
    private Tile DrawnTile() =>
        PendingMatchTile ?? throw new InvalidOperationException("No tile is drawn while the match awaits a reshuffle.");

    // The order must be exactly the board's tiles; the first restarts the board at the origin, the rest become the deck.
    private CommandResult ApplyReshuffle(ReshuffleBoard reshuffle)
    {
        if (!TryOrder(reshuffle.Order, out var order))
        {
            return Reject(
                RejectionReason.InvalidReshuffleOrder,
                "The reshuffle order must list every tile on the board, buried ones included, exactly once.");
        }

        var next = new Game(
            _seats,
            _currentIndex,
            _blobbityDeck,
            order[1..],
            Grid.FromStart(order[0]),
            _ruleset,
            ClaimsRequiredToWin,
            default,
            hasEnded: false);
        return CommandResult.Accept(next, [new BoardReshuffled(CurrentSeat, reshuffle.Order)]);
    }

    private bool TryOrder(IReadOnlyList<TileId> ids, out Tile[] order)
    {
        var onBoard = new Dictionary<TileId, Tile>();
        foreach (var tile in _grid.AllTiles())
        {
            onBoard.Add(tile.Id, tile);
        }

        order = new Tile[ids.Count];
        for (var i = 0; i < order.Length; i++)
        {
            if (!onBoard.Remove(ids[i], out order[i]!))
            {
                return false;
            }
        }

        return onBoard.Count == 0;
    }

    private static TileId[] IdsOf(IReadOnlyList<Tile> tiles)
    {
        var ids = new TileId[tiles.Count];
        for (var i = 0; i < ids.Length; i++)
        {
            ids[i] = tiles[i].Id;
        }

        return ids;
    }

    private CommandResult Reject(RejectionReason reason, string message) =>
        CommandResult.Reject(this, new Rejection(reason, message));

    private static BlobbityCard[] Copy(BlobbityCard[] blobbities)
    {
        var copy = new BlobbityCard[blobbities.Length];
        Array.Copy(blobbities, copy, blobbities.Length);
        return copy;
    }

    private SeatSnapshot Find(SeatId seat)
    {
        foreach (var snapshot in _seats)
        {
            if (snapshot.Id.Equals(seat))
            {
                return snapshot;
            }
        }

        throw new ArgumentException($"Seat '{seat.Value}' is not at the table.", nameof(seat));
    }

    private static Game Deal(GameSetup setup)
    {
        var seats = new SeatSnapshot[setup.SeatsInTurnOrder.Count];
        var deckIndex = 0;
        for (var i = 0; i < seats.Length; i++)
        {
            var hand = new[]
            {
                setup.BlobbityDeck[deckIndex],
                setup.BlobbityDeck[deckIndex + 1],
            };
            deckIndex += 2;
            seats[i] = new SeatSnapshot(setup.SeatsInTurnOrder[i], hand, []);
        }

        var blobbitiesLeft = setup.BlobbityDeck.Count - deckIndex;
        var blobbityDeck = new BlobbityCard[blobbitiesLeft];
        for (var i = 0; i < blobbitiesLeft; i++)
        {
            blobbityDeck[i] = setup.BlobbityDeck[deckIndex + i];
        }

        var matchLeft = setup.MatchDeck.Count - 1;
        var matchDeck = new Tile[matchLeft];
        for (var i = 0; i < matchLeft; i++)
        {
            matchDeck[i] = setup.MatchDeck[i + 1];
        }

        return new Game(
            seats,
            IndexOf(seats, setup.FirstSeat),
            blobbityDeck,
            matchDeck,
            Grid.FromStart(setup.MatchDeck[0]),
            new Ruleset(setup.NonScoringSymbols, setup.ActivePatterns),
            setup.ClaimsRequiredToWin,
            default,
            hasEnded: false);
    }

    private static int IndexOf(SeatSnapshot[] seats, SeatId seat)
    {
        for (var i = 0; i < seats.Length; i++)
        {
            if (seats[i].Id.Equals(seat))
            {
                return i;
            }
        }

        throw new InvalidOperationException("The first seat was validated and then not found.");
    }

    private sealed record ClaimResolution(
        IReadOnlyList<BlobbityCard> Claimed,
        SeatSnapshot ActingSeat,
        BlobbityCard[] BlobbityDeck,
        bool Won);

    private sealed class SeatSnapshot
    {
        public SeatSnapshot(SeatId id, BlobbityCard[] hand, BlobbityCard[] claims)
        {
            Id = id;
            Hand = hand;
            Claims = claims;
        }

        public SeatId Id { get; }

        public BlobbityCard[] Hand { get; }

        public BlobbityCard[] Claims { get; }
    }
}
