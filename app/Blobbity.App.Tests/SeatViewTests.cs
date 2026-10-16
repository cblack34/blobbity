namespace Blobbity.App.Tests;

using Blobbity.App;
using Blobbity.Rules;

public class SeatViewTests
{
    [Test]
    public void View_ShowsThatSeatsUnclaimedBlobbities_AndEverySeatsClaims()
    {
        Assert.That(RepresentativeDeck.Label, Is.EqualTo("representative"));
        var session = ClaimingTable.Open(3).Session;

        var before = session.View(new SeatId("a"));
        Assert.That(Ids(before.UnclaimedBlobbities), Is.EqualTo(new[] { "a-square", "a-row" }));
        Assert.That(BlobbityIds.In(before), Does.Contain("a-square"));
        Assert.That(BlobbityIds.In(before), Does.Contain("a-row"));
        Assert.That(before.Claims.Select(row => row.Seat.Value), Is.EqualTo(new[] { "a", "b", "c" }));
        Assert.That(before.Claims.Select(row => row.Blobbities.Count), Is.All.EqualTo(0));
        Assert.That(before.CurrentSeat, Is.EqualTo(new SeatId("a")));
        Assert.That(before.HasEnded, Is.False);
        Assert.That(before.Board.Tiles, Has.Count.EqualTo(1));
        Assert.That(before.Board.Tiles[0].Id.Value, Is.EqualTo("start"));
        Assert.That(before.Board.Tiles[0].TileX, Is.EqualTo(0));
        Assert.That(before.Board.Tiles[0].TileY, Is.EqualTo(0));
        Assert.That(
            before.Board.Cells.Select(cell => (cell.CellX, cell.CellY, cell.Value)),
            Is.EqualTo(new[]
            {
                (0, 0, Cards.Blank),
                (1, 0, Cards.Blank),
                (0, 1, Cards.Blank),
                (1, 1, Cards.Blank),
            }));
        AssertPending(before, "a-tile", Cards.Red);
        AssertAbsent(before, "b-square", "b-row", "c-square", "c-row");

        var placed = session.Submit(new SeatId("a"), new Place(1, 0, 0));
        Assert.That(placed.IsAccepted, Is.True, placed.Rejection?.Message);

        var fromB = session.View(new SeatId("b"));
        Assert.That(Ids(fromB.UnclaimedBlobbities), Is.EqualTo(new[] { "b-square", "b-row" }));
        Assert.That(ClaimIds(fromB, "a"), Is.EqualTo(new[] { "a-square" }));
        Assert.That(ClaimIds(fromB, "b"), Is.Empty);
        Assert.That(ClaimIds(fromB, "c"), Is.Empty);
        AssertAbsent(fromB, "a-row", "a-replacement", "c-square", "c-row", "spare-blobbity");

        var fromC = session.View(new SeatId("c"));
        Assert.That(ClaimIds(fromC, "a"), Is.EqualTo(new[] { "a-square" }));
        Assert.That(Ids(fromC.UnclaimedBlobbities), Is.EqualTo(new[] { "c-square", "c-row" }));
        AssertAbsent(fromC, "a-row", "a-replacement", "b-square", "b-row");

        var fromA = session.View(new SeatId("a"));
        Assert.That(Ids(fromA.UnclaimedBlobbities), Is.EqualTo(new[] { "a-row", "a-replacement" }));
        Assert.That(ClaimIds(fromA, "a"), Is.EqualTo(new[] { "a-square" }));
        AssertAbsent(fromA, "b-square", "b-row", "c-square", "c-row");
    }

    private static void AssertPending(SeatView view, string tileId, Cell cell)
    {
        var pending = view.PendingMatchTile;
        Assert.That(pending, Is.Not.Null);
        Assert.That(pending!.Id.Value, Is.EqualTo(tileId));
        Assert.That(pending.Local(0, 0), Is.EqualTo(cell));
        Assert.That(pending.Local(1, 0), Is.EqualTo(cell));
        Assert.That(pending.Local(0, 1), Is.EqualTo(cell));
        Assert.That(pending.Local(1, 1), Is.EqualTo(cell));
    }

    private static void AssertAbsent(SeatView view, params string[] blobbityIds)
    {
        var present = BlobbityIds.In(view);
        foreach (var blobbityId in blobbityIds)
        {
            Assert.That(present, Does.Not.Contain(blobbityId));
        }
    }

    private static string[] Ids(IReadOnlyList<BlobbityCard> blobbities) =>
        blobbities.Select(blobbity => blobbity.Id.Value).ToArray();

    private static string[] ClaimIds(SeatView view, string seat) =>
        Ids(view.Claims.Single(row => row.Seat.Value == seat).Blobbities);
}
