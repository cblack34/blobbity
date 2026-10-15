namespace MissionSplat.App.Tests;

using MissionSplat.App;
using MissionSplat.Rules;

public class RecapTests
{
    private static readonly SeatId First = new("1");

    private static readonly SeatId Second = new("2");

    private static readonly SeatId Third = new("3");

    [Test]
    public void ATableJustStarted_HasNothingToRecap()
    {
        var table = Started(TableStart.HumanThenAi());

        Assert.That(table.Snapshot.Recap, Is.Empty);
        table.AcknowledgeRecap();
        table.Confirm();
        Assert.That(table.Snapshot.Recap, Is.Empty);
        table.AcknowledgeRecap();
        Assert.That(table.Snapshot.Recap, Is.Empty);
    }

    [Test]
    public void AnAiFirstSeat_IsRecappedToTheHumanOnceTheyConfirm()
    {
        var table = Started(new TableStart(2, [true, false], 0, false));

        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        Assert.That(table.Snapshot.Status.Seat, Is.EqualTo(Second));
        table.Confirm();

        Assert.That(table.Snapshot.RecapSeat, Is.EqualTo(Second));
        Assert.That(table.Snapshot.Recap.Select(item => item.Seat), Is.EqualTo(new[] { First }));
    }

    [Test]
    public void AnAiFirstSeatBeforeTwoHumans_IsRecappedToEachOnceTheyConfirm()
    {
        var table = Started(new TableStart(3, [true, false, false], 0, false));
        Assert.That(table.Snapshot.ConcealVisible, Is.True, "the first human is asked to confirm, so they become the holder");
        Assert.That(table.Snapshot.Recap, Is.Empty, "nothing behind the cover");
        table.Confirm();
        Assert.That(table.Snapshot.Recap.Select(item => item.Seat), Is.EqualTo(new[] { First }));

        table.Submit(new Place(1, 0, 0));
        table.AcknowledgeRecap();
        table.Confirm();

        Assert.That(table.Snapshot.RecapSeat, Is.EqualTo(Third));
        Assert.That(table.Snapshot.Recap.Select(item => item.Seat), Is.EqualTo(new[] { First, Second }), "the AI's opening move, then the other human's turn");
    }

    [Test]
    public void AHumanAgainstAnAi_SeesTheirOwnPlacementThenTheAisBeforeActingAgain()
    {
        var table = Started(TableStart.HumanThenAi());
        table.Confirm();

        table.Submit(new Place(1, 0, 0));

        var recap = table.Snapshot.Recap;
        Assert.That(recap.Select(item => item.Seat), Is.EqualTo(new[] { First, Second }));
        Assert.That(recap.Select(item => ((RecapPlaced)item).Tile.Value), Is.EqualTo(new[] { "red", "blue" }));

        table.AcknowledgeRecap();
        Assert.That(table.Snapshot.Recap, Is.Empty);
        table.Submit(new Place(0, 1, 0));
        Assert.That(table.Snapshot.Recap.Select(item => item.Seat), Is.EqualTo(new[] { First, Second }), "only the newer turns remain");
    }

    [Test]
    public void ActingWhileARecapIsPending_StillWorks_AndTheRecapKeepsGrowing()
    {
        var table = Started(TableStart.HumanThenAi());
        table.Confirm();
        table.Submit(new Place(1, 0, 0));

        var second = table.Submit(new Place(0, 1, 0));

        Assert.That(second.IsAccepted, Is.True, second.Rejection?.Message);
        Assert.That(table.Snapshot.Recap, Has.Count.EqualTo(4));
    }

    [Test]
    public void ThePlacedItem_CarriesTheCellsAsPlaced_AndWhetherItCovered()
    {
        var table = Started(
            Humans(2),
            Cards.BlankTile("start"),
            Cards.Tile("mixed", Cards.Red, Cards.Blue, Cards.Green, Cards.Purple),
            Cards.Solid("spare", Cards.Red));
        table.Confirm();

        table.Submit(new Place(1, 0, 1));

        var placed = (RecapPlaced)table.Snapshot.Recap.Single();
        Assert.That(placed, Has.Property("TileX").EqualTo(1).And.Property("TileY").EqualTo(0).And.Property("QuarterTurnsClockwise").EqualTo(1));
        Assert.That(placed.Covered, Is.False);
        var onBoard = table.Snapshot.View.Board.CellsOf(1, 0);
        Assert.That(placed.Cells, Is.EquivalentTo(onBoard));
        Assert.That(placed.Cells, Has.Count.EqualTo(4));
        Assert.That(placed.Cells.Select(cell => cell.Value).Distinct().Count(), Is.EqualTo(4), "turned, not four copies of one cell");
    }

    [Test]
    public void TwoHumans_TheOutgoingSeatSeesTheirOwnUntilAcknowledged_TheIncomingSeatSeesThemAfterConfirming()
    {
        var table = Started(Humans(2));
        table.Confirm();

        table.Submit(new Place(1, 0, 0));

        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        Assert.That(table.Snapshot.Recap.Select(item => item.Seat), Is.EqualTo(new[] { First }), "the outgoing player's own placement, before the cover");

        table.AcknowledgeRecap();
        Assert.That(table.Snapshot.Recap, Is.Empty, "nothing for the next player until they confirm");

        table.Confirm();
        Assert.That(table.Snapshot.Status.Seat, Is.EqualTo(Second));
        Assert.That(table.Snapshot.Recap.Select(item => item.Seat), Is.EqualTo(new[] { First }));
    }

    [Test]
    public void ThreeSeats_OneHumansRecapSpansTwoOtherTurnsInOrder()
    {
        var table = Started(
            new TableStart(3, [false, true, false], 0, false),
            Cards.BlankTile("start"),
            Cards.Solid("red", Cards.Red),
            Cards.Solid("blue", Cards.Blue),
            Cards.Solid("green", Cards.Green),
            Cards.Solid("purple", Cards.Purple));
        table.Confirm();

        table.Submit(new Place(1, 0, 0));
        table.AcknowledgeRecap();
        table.Confirm();

        Assert.That(table.Snapshot.Status.Seat, Is.EqualTo(Third));
        var recap = table.Snapshot.Recap;
        Assert.That(recap.Select(item => item.Seat), Is.EqualTo(new[] { First, Second }));
        Assert.That(recap.Select(item => ((RecapPlaced)item).Tile.Value), Is.EqualTo(new[] { "red", "blue" }));
    }

    [Test]
    public void Acknowledging_AdvancesOnlyTheHoldersCursor()
    {
        var table = Started(Humans(2));
        table.Confirm();
        table.Submit(new Place(1, 0, 0));
        table.Confirm();
        table.AcknowledgeRecap();
        Assert.That(table.Snapshot.Recap, Is.Empty);

        table.Submit(new Place(0, 1, 0));
        table.Confirm();

        Assert.That(
            table.Snapshot.Recap.Select(item => item.Seat),
            Is.EqualTo(new[] { First, Second }),
            "the first seat never acknowledged, so both turns are still theirs to see");
    }

    [Test]
    public void ABounce_CarriesTheLiftedTilesCellsFromBeforeAndNothingRevealed()
    {
        var table = Started(
            Humans(2),
            Cards.BlankTile("start"),
            Cards.Tile("side", Cards.Red, Cards.Blue, Cards.Green, Cards.Purple),
            Cards.Tile("lifter", Cell.Symbol(OrdinaryCatalog.Bounce), Cards.Red, Cards.Red, Cards.Red),
            Cards.Solid("next", Cards.Blue));
        table.Confirm();
        table.Submit(new Place(1, 0, 0));
        var sideCells = table.Snapshot.View.Board.CellsOf(1, 0);
        table.Confirm();
        table.SelectPower(OrdinaryCatalog.Bounce);

        table.UseAt(1, 0);

        Assert.That(table.Snapshot.Recap.Select(item => item.GetType()), Is.EqualTo(new[] { typeof(RecapPlaced), typeof(RecapBounced) }));
        var bounced = (RecapBounced)table.Snapshot.Recap[1];
        Assert.That(bounced, Has.Property("Seat").EqualTo(Second).And.Property("TileX").EqualTo(1).And.Property("TileY").EqualTo(0));
        Assert.That(bounced.RemovedCells, Is.EquivalentTo(sideCells), "the cells that were lifted, though the board no longer shows them");
        Assert.That(bounced.RemovedCells, Has.Count.EqualTo(4));
        Assert.That(bounced.RevealedCells, Is.Empty);
        Assert.That(table.Snapshot.View.Board.CellsOf(1, 0), Is.Empty);
    }

    [Test]
    public void ABounceOfACoveringTile_CarriesTheRevealedTilesCells()
    {
        var table = Started(
            Humans(2),
            Cards.Tile("start", Cards.Green, Cards.Blue, Cards.Purple, Cards.Red),
            Cards.Tile("lid", Cell.Symbol(OrdinaryCatalog.Stack), Cards.Red, Cards.Red, Cards.Red),
            Cards.Tile("lifter", Cell.Symbol(OrdinaryCatalog.Bounce), Cards.Red, Cards.Red, Cards.Red),
            Cards.Solid("next", Cards.Blue));
        var startCells = table.Snapshot.View.Board.CellsOf(0, 0);
        table.Confirm();
        table.Submit(new Place(0, 0, 0));
        var lidCells = table.Snapshot.View.Board.CellsOf(0, 0);
        table.Confirm();
        table.SelectPower(OrdinaryCatalog.Bounce);

        table.UseAt(0, 0);

        var bounced = table.Snapshot.Recap.OfType<RecapBounced>().Single();
        Assert.That(bounced.RemovedCells, Is.EquivalentTo(lidCells));
        Assert.That(bounced.RevealedCells, Is.EquivalentTo(startCells));
        Assert.That(bounced.RevealedCells, Is.Not.EquivalentTo(bounced.RemovedCells));
    }

    [Test]
    public void ARotate_CarriesItsSeatPositionAmountAndTheCellsAfterTheTurn()
    {
        var table = Started(
            Humans(2),
            Cards.Tile("start", Cards.Red, Cards.Blue, Cards.Green, Cards.Purple),
            Cards.Tile("spinner", Cell.Symbol(OrdinaryCatalog.Rotate), Cards.Red, Cards.Red, Cards.Red),
            Cards.Solid("spare", Cards.Blue));
        var before = table.Snapshot.View.Board.CellsOf(0, 0);
        table.Confirm();
        table.SelectPower(OrdinaryCatalog.Rotate);
        table.SetQuarterTurns(3);

        table.UseAt(0, 0);

        var rotated = (RecapRotated)table.Snapshot.Recap.Single();
        Assert.That(rotated, Has.Property("Seat").EqualTo(First).And.Property("TileX").EqualTo(0).And.Property("TileY").EqualTo(0).And.Property("QuarterTurnsClockwise").EqualTo(3));
        Assert.That(rotated.Cells, Is.EquivalentTo(table.Snapshot.View.Board.CellsOf(0, 0)));
        Assert.That(rotated.Cells, Is.Not.EquivalentTo(before), "the cells after the turn, not before it");
    }

    [Test]
    public void AStackedPlacement_IsCoveredAndCarriesTheTopTilesFourCells()
    {
        var table = Started(
            Humans(2),
            Cards.BlankTile("start"),
            Cards.Tile("lid", Cell.Symbol(OrdinaryCatalog.Stack), Cards.Red, Cards.Red, Cards.Red),
            Cards.Solid("spare", Cards.Blue));
        table.Confirm();

        table.Submit(new Place(0, 0, 0));

        var placed = (RecapPlaced)table.Snapshot.Recap.Single();
        Assert.That(placed.Covered, Is.True);
        Assert.That(placed.Cells, Has.Count.EqualTo(4));
        Assert.That(
            placed.Cells.Select(cell => cell.Value),
            Is.EquivalentTo(new[] { Cell.Symbol(OrdinaryCatalog.Stack), Cards.Red, Cards.Red, Cards.Red }),
            "the lid's values, not the covered blank tile's");
    }

    [Test]
    public void APlacementAtANegativeTilePosition_CarriesItsCellsAtTheRightCoordinates()
    {
        var table = Started(
            Humans(2),
            Cards.BlankTile("start"),
            Cards.Tile("mixed", Cards.Red, Cards.Blue, Cards.Green, Cards.Purple),
            Cards.Solid("spare", Cards.Blue));
        table.Confirm();

        table.Submit(new Place(-1, 0, 0));

        var placed = (RecapPlaced)table.Snapshot.Recap.Single();
        Assert.That(
            placed.Cells.Select(cell => (cell.CellX, cell.CellY, cell.Value)),
            Is.EquivalentTo(new[]
            {
                (-2, 0, Cards.Red),
                (-1, 0, Cards.Blue),
                (-2, 1, Cards.Green),
                (-1, 1, Cards.Purple),
            }));
    }

    [Test]
    public void ARestartedTable_StartsItsRecapOverForAFreshFirstTurn()
    {
        var table = Started(new TableStart(2, [true, false], 0, false));
        table.Confirm();
        table.Submit(new Place(1, 0, 0));
        table.Confirm();
        Assert.That(table.Snapshot.Recap, Has.Count.GreaterThan(1), "nothing was acknowledged, so the old turns are still pending");

        table.Start();

        Assert.That(table.Snapshot.Recap, Is.Empty);
        Assert.That(table.Snapshot.RecapSeat, Is.Null);
        Assert.That(table.Snapshot.ConcealVisible, Is.True, "the human is handed the device afresh");
        table.Confirm();
        Assert.That(table.Snapshot.Recap.Select(item => item.Seat), Is.EqualTo(new[] { First }), "only the new opening move");
    }

    [Test]
    public void EveryGameEventTheRulesCanRaise_HasARecapItem()
    {
        var sample = new Dictionary<Type, GameEvent>
        {
            [typeof(TilePlaced)] = new TilePlaced(First, new TileId("t"), 0, 0, 0, null),
            [typeof(TileRotated)] = new TileRotated(First, 0, 0, 1),
            [typeof(TileBounced)] = new TileBounced(First, 0, 0, new TileId("t"), null),
            [typeof(BlobbityClaimed)] = new BlobbityClaimed(First, new BlobbityId("1-a")),
            [typeof(GameWon)] = new GameWon(First, 1),
            [typeof(BoardReshuffled)] = new BoardReshuffled(First, [new TileId("t")]),
        };
        var raised = typeof(GameEvent).Assembly.GetTypes().Where(type => typeof(GameEvent).IsAssignableFrom(type) && !type.IsAbstract);
        Assert.That(sample.Keys, Is.EquivalentTo(raised), "a new event needs a sample here and a recap item (or an explicit decision to ignore it)");

        var board = new BoardView([new OccupiedTile(new TileId("t"), 0, 0)], [new OccupiedCell(0, 0, Cards.Red)]);
        var setup = Setup(2, DefaultTiles);
        foreach (var gameEvent in sample.Values)
        {
            var feed = new RecapFeed(setup);
            feed.Record([gameEvent], board, board);
            Assert.That(feed.PendingFor(First), Has.Length.EqualTo(1), gameEvent.GetType().Name);
        }
    }

    [Test]
    public void AClaimAndTheWin_ProduceClaimedAndWonItems_WithTheMissionCard()
    {
        var square = Cards.Mission("one-square", BlobbityPattern.Square, OrdinaryCatalog.Red);
        var setup = new GameSetup(
            [First, Second],
            First,
            OrdinaryCatalog.Colors,
            OrdinaryCatalog.NonScoringSymbols,
            OrdinaryCatalog.Patterns,
            1,
            [square, Cards.Row("one-row"), Cards.Row("two-1"), Cards.Row("two-2"), Cards.Row("spare")],
            [Cards.BlankTile("start"), Cards.Solid("red", Cards.Red), Cards.BlankTile("next")]);
        var table = new Table(new LocalSession(), Humans(2), setup);
        table.Start();
        table.Confirm();

        table.Submit(new Place(1, 0, 0));

        var recap = table.Snapshot.Recap;
        Assert.That(recap, Has.Count.EqualTo(3));
        Assert.That(recap[0], Is.TypeOf<RecapPlaced>());
        Assert.That(recap[1], Is.EqualTo(new RecapClaimed(First, square)));
        Assert.That(recap[2], Is.EqualTo(new RecapWon(First, 1)));
    }

    [Test]
    public void ARotateClaim_IsRecappedAsRotatedThenClaimed_ForTheActingSeat()
    {
        var redRow = Cards.Mission("red-row", BlobbityPattern.Row, OrdinaryCatalog.Red);
        var setup = new GameSetup(
            [First, Second],
            First,
            OrdinaryCatalog.Colors,
            OrdinaryCatalog.NonScoringSymbols,
            OrdinaryCatalog.Patterns,
            OrdinaryCatalog.ClaimsRequiredToWin,
            [redRow, Cards.Row("a2"), Cards.Row("b1"), Cards.Row("b2"), Cards.Row("spare")],
            [
                Cards.Tile("start", Cards.Red, Cards.Red, Cards.Blank, Cards.Blank),
                Cards.Tile("east", Cards.Blank, Cards.Red, Cards.Blank, Cards.Red),
                Cards.BlankTile("pad"),
                Cards.Tile("spinner", Cell.Symbol(OrdinaryCatalog.Rotate), Cards.Blank, Cards.Blank, Cards.Blank),
            ]);
        var table = new Table(new LocalSession(), Humans(2), setup);
        table.Start();
        table.Confirm();
        table.Submit(new Place(1, 0, 0));
        table.Confirm();
        table.Submit(new Place(0, 1, 0));
        table.Confirm();
        table.SelectPower(OrdinaryCatalog.Rotate);
        table.SetQuarterTurns(1);

        table.UseAt(1, 0);

        var recap = table.Snapshot.Recap;
        Assert.That(recap.TakeLast(2).Select(item => item.GetType()), Is.EqualTo(new[] { typeof(RecapRotated), typeof(RecapClaimed) }));
        Assert.That(recap[^1], Is.EqualTo(new RecapClaimed(First, redRow)));
        Assert.That(((RecapRotated)recap[^2]).Seat, Is.EqualTo(First));
    }

    [Test]
    public void AReshuffle_BecomesAnItemThatCarriesNoOrder()
    {
        var table = Started(
            Humans(2),
            Cards.BlankTile("start"),
            Cards.Solid("red", Cards.Red),
            Cards.Solid("blue", Cards.Blue),
            Cards.Solid("green", Cards.Green),
            Cards.Solid("purple", Cards.Purple),
            Cards.Solid("red-2", Cards.Red));
        var placements = 0;
        while (table.Snapshot.Notice != TableNotice.DeckReshuffled)
        {
            Assert.That(placements, Is.LessThan(10), "the deck never ran out");
            table.Confirm();
            var slot = table.Snapshot.LegalPlacements[0];
            Assert.That(table.Place(slot.TileX, slot.TileY).IsAccepted, Is.True);
            placements++;
        }

        // The seat that placed last never acknowledged, so its recap is the whole log in order.
        var recap = table.Snapshot.Recap;
        Assert.That(recap, Has.Count.EqualTo(placements + 1));
        Assert.That(recap.Take(placements), Has.All.TypeOf<RecapPlaced>());
        Assert.That(recap[placements], Is.TypeOf<RecapReshuffled>());
        Assert.That(
            typeof(RecapReshuffled).GetProperties().Select(property => property.Name).Except(["EqualityContract"]),
            Is.EqualTo(new[] { "Seat" }));
    }

    private static Table Started(TableStart start, params Tile[] tiles)
    {
        var seats = start.SeatCount;
        var table = new Table(new LocalSession(), start, Setup(seats, tiles.Length == 0 ? DefaultTiles : tiles));
        table.Start();
        return table;
    }

    private static readonly Tile[] DefaultTiles =
    [
        Cards.BlankTile("start"),
        Cards.Solid("red", Cards.Red),
        Cards.Solid("blue", Cards.Blue),
        Cards.Solid("green", Cards.Green),
        Cards.Solid("purple", Cards.Purple),
        Cards.Solid("spare-1", Cards.Blue),
        Cards.Solid("spare-2", Cards.Green),
        Cards.Solid("spare-3", Cards.Red),
        Cards.Solid("spare-4", Cards.Purple),
    ];

    private static TableStart Humans(int seatCount) => new(seatCount, new bool[seatCount], 0, false);

    private static GameSetup Setup(int seatCount, Tile[] tiles)
    {
        var seats = Enumerable.Range(1, seatCount).Select(number => number.ToString()).ToArray();
        var missions = seats.SelectMany(seat => new[] { Cards.Row(seat + "-a"), Cards.Row(seat + "-b"), Cards.Row(seat + "-replacement") }).ToList();
        missions.Add(Cards.Row("spare"));
        return RepresentativeDeck.Setup(seats, "1", missions.ToArray(), tiles);
    }
}
