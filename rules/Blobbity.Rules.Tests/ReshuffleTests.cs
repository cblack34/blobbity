namespace Blobbity.Rules.Tests;

public class ReshuffleTests
{
    private static readonly Cell Stack = Cell.Symbol(OrdinaryCatalog.Stack);

    [Test]
    public void APlacementThatEmptiesTheDeck_AwaitsAReshuffle_WithTheWholeBoardListed()
    {
        var game = TwoSeatsOneTileLeft();
        Assert.That(game.AwaitsReshuffle, Is.False);
        Assert.That(game.TilesToReshuffle, Is.Empty);

        var exhausted = See.Game(RepresentativeDeck.Play(game, 1, 0));

        Assert.That(exhausted.AwaitsReshuffle, Is.True);
        Assert.That(exhausted.HasEnded, Is.False);
        Assert.That(exhausted.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(See.Ids(exhausted.TilesToReshuffle), Is.EqualTo(new[] { "start", "last" }));
    }

    [Test]
    public void WhileAwaiting_TheQueriesAnswerWithNothing()
    {
        var exhausted = Exhausted();

        Assert.That(exhausted.PendingMatchTile, Is.Null);
        Assert.That(exhausted.LegalPlacements(0), Is.Empty);
        Assert.That(exhausted.LegalTargets(OrdinaryCatalog.Rotate), Is.Empty);
        Assert.That(exhausted.LegalTargets(OrdinaryCatalog.Bounce), Is.Empty);
        Assert.That(exhausted.RemainingUses(OrdinaryCatalog.Rotate), Is.EqualTo(0));
        Assert.That(exhausted.RemainingUses(OrdinaryCatalog.Bounce), Is.EqualTo(0));
    }

    [Test]
    public void WhileAwaiting_EveryOtherActionIsRejected()
    {
        var exhausted = Exhausted();

        foreach (GameAction action in new GameAction[] { new Place(2, 0, 0), new UseRotate(0, 0, 1), new UseBounce(0, 0) })
        {
            Expect.Rejected(exhausted, RepresentativeDeck.Try(exhausted, action), RejectionReason.AwaitingReshuffle);
        }
    }

    [Test]
    public void AValidOrder_RestartsTheBoardFromTheFirstTile_AndDealsTheRestAsTheDeck()
    {
        var game = ThreeTileMatch();
        var exhausted = See.Game(RepresentativeDeck.Play(See.Game(RepresentativeDeck.Play(game, 1, 0)), 2, 0));
        var seat = exhausted.CurrentSeat;
        var handA = See.Ids(exhausted.Hand(Cards.Seat("a")));
        var handB = See.Ids(exhausted.Hand(Cards.Seat("b")));

        var result = RepresentativeDeck.Try(exhausted, new ReshuffleBoard(Order("t2", "start", "t1")));

        var next = See.Game(result);
        Assert.That(next.AwaitsReshuffle, Is.False);
        Assert.That(next.TileCount, Is.EqualTo(1));
        Assert.That(next.Tiles, Is.EqualTo(new[] { new VisibleTile(new TileId("t2"), 0, 0) }));
        Assert.That(next.PendingMatchTile!.Id.Value, Is.EqualTo("start"));
        Assert.That(next.MatchDeckRemaining, Is.EqualTo(2));
        Assert.That(next.CurrentSeat, Is.EqualTo(seat));
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(handA));
        Assert.That(See.Ids(next.Hand(Cards.Seat("b"))), Is.EqualTo(handB));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(next.Claims(Cards.Seat("b")), Is.Empty);
        Assert.That(next.BlobbityDeckRemaining, Is.EqualTo(exhausted.BlobbityDeckRemaining));
        var reshuffled = (BoardReshuffled)result.Events.Single();
        Assert.That(reshuffled.Seat, Is.EqualTo(seat));
        Assert.That(See.Ids(reshuffled.Order), Is.EqualTo(new[] { "t2", "start", "t1" }));
    }

    [Test]
    public void AfterTheReshuffle_TheNextPlacementPlaysTheNewDrawnTile()
    {
        var exhausted = Exhausted();
        var reshuffled = See.Game(RepresentativeDeck.Try(exhausted, new ReshuffleBoard(Order("last", "start"))));

        var placed = RepresentativeDeck.Play(reshuffled, 1, 0);

        var next = See.Game(placed);
        Assert.That(placed.Events.OfType<TilePlaced>().Single().Tile.Value, Is.EqualTo("start"));
        Assert.That(next.TileCount, Is.EqualTo(2));
        Assert.That(next.CurrentSeat, Is.Not.EqualTo(reshuffled.CurrentSeat));
        Assert.That(next.AwaitsReshuffle, Is.True);
    }

    [Test]
    public void ABuriedTile_MustBeInTheOrder_AndEndsInTheDeck()
    {
        var game = RepresentativeDeck.TwoSeats(
            Cards.BlankTile("start"),
            Cards.Tile("lid", Stack, Cards.Blank, Cards.Blank, Cards.Blank),
            Cards.BlankTile("last"));
        var stacked = See.Game(RepresentativeDeck.Play(game, 0, 0));
        Assert.That(stacked.CoveredTileIds(0, 0).Select(id => id.Value), Is.EqualTo(new[] { "start" }));
        var exhausted = See.Game(RepresentativeDeck.Play(stacked, 1, 0));

        Assert.That(exhausted.AwaitsReshuffle, Is.True);
        Assert.That(See.Ids(exhausted.TilesToReshuffle), Is.EquivalentTo(new[] { "lid", "start", "last" }));

        var withoutBuried = RepresentativeDeck.Try(exhausted, new ReshuffleBoard(Order("last", "lid")));
        Expect.Rejected(exhausted, withoutBuried, RejectionReason.InvalidReshuffleOrder);

        var accepted = See.Game(RepresentativeDeck.Try(exhausted, new ReshuffleBoard(Order("last", "start", "lid"))));
        Assert.That(accepted.Tiles, Is.EqualTo(new[] { new VisibleTile(new TileId("last"), 0, 0) }));
        Assert.That(accepted.CoveredTileIds(0, 0), Is.Empty);
        Assert.That(accepted.PendingMatchTile!.Id.Value, Is.EqualTo("start"));
        Assert.That(accepted.MatchDeckRemaining, Is.EqualTo(2));
    }

    [TestCase("")]
    [TestCase("start")]
    [TestCase("start,start")]
    [TestCase("start,last,last")]
    [TestCase("start,last,ghost")]
    [TestCase("start,ghost")]
    public void AnOrderThatIsNotExactlyTheBoard_IsRejected_AndLeavesTheGame(string ids)
    {
        var exhausted = Exhausted();

        var result = RepresentativeDeck.Try(exhausted, new ReshuffleBoard(Order(ids.Split(',', StringSplitOptions.RemoveEmptyEntries))));

        Expect.Rejected(exhausted, result, RejectionReason.InvalidReshuffleOrder);
        Assert.That(exhausted.AwaitsReshuffle, Is.True);
        Assert.That(exhausted.TileCount, Is.EqualTo(2));
    }

    [Test]
    public void AReshuffle_WhenTheDeckIsNotEmpty_IsRejected()
    {
        var game = TwoSeatsOneTileLeft();

        var result = RepresentativeDeck.Try(game, new ReshuffleBoard(Order("start")));

        Expect.Rejected(game, result, RejectionReason.NotAwaitingReshuffle);
    }

    [Test]
    public void AReshuffle_ByAnotherSeat_IsRejectedAsNotTheirTurn()
    {
        var exhausted = Exhausted();
        var other = exhausted.CurrentSeat.Equals(Cards.Seat("a")) ? Cards.Seat("b") : Cards.Seat("a");

        var result = exhausted.Apply(other, new ReshuffleBoard(Order("start", "last")));

        Expect.Rejected(exhausted, result, RejectionReason.NotYourTurn);
    }

    [Test]
    public void ANewStartTileShowingAPattern_ClaimsNothing()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.BlobbityCard("red-square", BlobbityPattern.Square, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("spare"),
            ],
            [Cards.BlankTile("start"), Cards.BlankTile("pad"), Cards.Solid("red", Cards.Red)]);
        var exhausted = See.Game(RepresentativeDeck.Play(See.Game(RepresentativeDeck.Play(game, 1, 0)), 2, 0));
        Assert.That(exhausted.AwaitsReshuffle, Is.True);
        Assert.That(exhausted.CurrentSeat, Is.EqualTo(Cards.Seat("a")));

        var result = RepresentativeDeck.Try(exhausted, new ReshuffleBoard(Order("red", "start", "pad")));

        var next = See.Game(result);
        Assert.That(next.CellAt(0, 0), Is.EqualTo(Cards.Red));
        Assert.That(next.Claims(Cards.Seat("a")), Is.Empty);
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "red-square", "a2" }));
        Assert.That(result.Events.OfType<BlobbityClaimed>(), Is.Empty);
        Assert.That(next.HasEnded, Is.False);
    }

    [Test]
    public void AfterTheReshuffle_ThePowerUsesAreFullyAvailableOnTheNewPendingTile()
    {
        var rotate = Cell.Symbol(OrdinaryCatalog.Rotate);
        var game = RepresentativeDeck.TwoSeats(
            Cards.Tile("start", rotate, rotate, Cards.Blank, Cards.Blank),
            Cards.Tile("drawn", rotate, Cards.Blank, Cards.Blank, Cards.Blank));
        var rotated = See.Game(RepresentativeDeck.Rotate(game, 0, 0));
        Assert.That(rotated.RemainingUses(OrdinaryCatalog.Rotate), Is.EqualTo(0));
        var exhausted = See.Game(RepresentativeDeck.Play(rotated, 1, 0));
        Assert.That(exhausted.AwaitsReshuffle, Is.True);

        var next = See.Game(RepresentativeDeck.Try(exhausted, new ReshuffleBoard(Order("drawn", "start"))));

        Assert.That(next.PendingMatchTile!.Id.Value, Is.EqualTo("start"));
        Assert.That(next.RemainingUses(OrdinaryCatalog.Rotate), Is.EqualTo(2));
        Assert.That(next.LegalTargets(OrdinaryCatalog.Rotate), Is.Not.Empty);
        Assert.That(See.Game(RepresentativeDeck.Rotate(next, 0, 0)).RemainingUses(OrdinaryCatalog.Rotate), Is.EqualTo(1));
    }

    [Test]
    public void AGameWonOnTheLastTile_DoesNotAwaitAReshuffle()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                Cards.BlobbityCard("red-square", BlobbityPattern.Square, OrdinaryCatalog.Red),
                Cards.Purple("a2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("spare"),
            ],
            [Cards.BlankTile("start"), Cards.Solid("red", Cards.Red)],
            claimsRequiredToWin: 1);

        var won = See.Game(RepresentativeDeck.Play(game, 1, 0));

        Assert.That(won.HasEnded, Is.True);
        Assert.That(won.MatchDeckRemaining, Is.EqualTo(0));
        Assert.That(won.AwaitsReshuffle, Is.False);
        Assert.That(won.TilesToReshuffle, Is.Empty);
        Expect.Rejected(won, RepresentativeDeck.Try(won, new ReshuffleBoard(Order("start", "red"))), RejectionReason.GameOver);
    }

    [Test]
    public void TheActionCopiesItsOrder()
    {
        var ids = new List<TileId> { new("start"), new("last") };

        var action = new ReshuffleBoard(ids);
        ids.Clear();

        Assert.That(See.Ids(action.Order), Is.EqualTo(new[] { "start", "last" }));
    }

    private static Game TwoSeatsOneTileLeft() =>
        RepresentativeDeck.TwoSeats(Cards.BlankTile("start"), Cards.BlankTile("last"));

    private static Game Exhausted() => See.Game(RepresentativeDeck.Play(TwoSeatsOneTileLeft(), 1, 0));

    private static Game ThreeTileMatch() =>
        RepresentativeDeck.TwoSeats(Cards.BlankTile("start"), Cards.BlankTile("t1"), Cards.BlankTile("t2"));

    private static TileId[] Order(params string[] ids) => ids.Select(id => new TileId(id)).ToArray();
}
