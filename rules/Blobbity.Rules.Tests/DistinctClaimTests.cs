namespace Blobbity.Rules.Tests;

// Each claim in one placement needs its own four cells: shared cells are fine, an identical set is not.
[TestFixture]
public sealed class DistinctClaimTests
{
    private static readonly Ruleset Ordinary =
        new(OrdinaryCatalog.NonScoringSymbols, OrdinaryCatalog.Patterns);

    private static readonly Cell Blue = Cards.Blue;

    [Test]
    public void BlueLineAndBlueL_SharingCells_BothClaim()
    {
        // Blue along y=0 from x=0..3 is the line; the same three cells at x=0..2 plus (2, 1) are the L.
        var claimed = Claim(
            Cards.Tile("start", Blue, Blue, Cards.Blank, Cards.Blank),
            Cards.Tile("finish", Blue, Blue, Blue, Cards.Blank),
            Cards.BlobbityCard("blue-row", BlobbityPattern.Row, OrdinaryCatalog.Blue),
            Cards.BlobbityCard("blue-l", BlobbityPattern.L, OrdinaryCatalog.Blue));

        Assert.That(See.Ids(claimed), Is.EqualTo(new[] { "blue-row", "blue-l" }));
    }

    [Test]
    public void IdenticalBlobbities_WithOneCompletingSet_ClaimOnlyTheEarlierCard()
    {
        var claimed = Claim(
            Cards.Tile("start", Blue, Blue, Cards.Blank, Cards.Blank),
            Cards.Tile("finish", Blue, Blue, Cards.Blank, Cards.Blank),
            BlueRow("row-1"),
            BlueRow("row-2"));

        Assert.That(See.Ids(claimed), Is.EqualTo(new[] { "row-1" }));
    }

    [Test]
    public void IdenticalBlobbities_WithTwoCompletingSets_BothClaim()
    {
        var claimed = Claim(
            Cards.Solid("start", Blue),
            Cards.Solid("finish", Blue),
            BlueRow("row-1"),
            BlueRow("row-2"));

        Assert.That(See.Ids(claimed), Is.EqualTo(new[] { "row-1", "row-2" }));
    }

    [Test]
    public void EarlierBlobbityWithTwoSets_LeavesTheOnlySetToALaterBlobbity()
    {
        // Wild cells on y=0 serve a blue row and a red row alike; only the blue row also has y=1.
        var claimed = Claim(
            Cards.Tile("start", Cards.Wild, Cards.Wild, Blue, Blue),
            Cards.Tile("finish", Cards.Wild, Cards.Wild, Blue, Blue),
            BlueRow("blue-row"),
            Cards.BlobbityCard("red-row", BlobbityPattern.Row, OrdinaryCatalog.Red));

        Assert.That(See.Ids(claimed), Is.EqualTo(new[] { "blue-row", "red-row" }));
    }

    [Test]
    public void Game_IdenticalBlobbitiesWithOneSet_ClaimsOnceAndDrawsOneReplacement()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                BlueRow("row-1"),
                BlueRow("row-2"),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("repl-1"),
                Cards.Purple("repl-2"),
            ],
            [
                Cards.Tile("start", Blue, Blue, Cards.Blank, Cards.Blank),
                Cards.Tile("finish", Blue, Blue, Cards.Blank, Cards.Blank),
            ]);

        var placed = RepresentativeDeck.Play(game, 1, 0);
        var next = See.Game(placed);

        Assert.That(See.Ids(next.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "row-1" }));
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "row-2", "repl-1" }));
        Assert.That(next.BlobbityDeckRemaining, Is.EqualTo(1));
        Assert.That(placed.Events.OfType<BlobbityClaimed>().Select(e => e.Id.Value), Is.EqualTo(new[] { "row-1" }));
    }

    [Test]
    public void Game_FourWildcardsInARow_ClaimTheEarlierOfARedAndABlueLine()
    {
        var game = RepresentativeDeck.Start(
            ["a", "b"],
            "a",
            [
                BlueRow("blue-row"),
                Cards.BlobbityCard("red-row", BlobbityPattern.Row, OrdinaryCatalog.Red),
                Cards.Purple("b1"),
                Cards.Purple("b2"),
                Cards.Purple("repl-1"),
                Cards.Purple("repl-2"),
            ],
            [
                Cards.Tile("start", Cards.Wild, Cards.Wild, Cards.Blank, Cards.Blank),
                Cards.Tile("finish", Cards.Wild, Cards.Wild, Cards.Blank, Cards.Blank),
            ]);

        var placed = RepresentativeDeck.Play(game, 1, 0);
        var next = See.Game(placed);

        Assert.That(See.Ids(next.Claims(Cards.Seat("a"))), Is.EqualTo(new[] { "blue-row" }));
        Assert.That(See.Ids(next.Hand(Cards.Seat("a"))), Is.EqualTo(new[] { "red-row", "repl-1" }));
        Assert.That(next.BlobbityDeckRemaining, Is.EqualTo(1));
    }

    private static BlobbityCard BlueRow(string id) => Cards.BlobbityCard(id, BlobbityPattern.Row, OrdinaryCatalog.Blue);

    // The finish tile goes beside the start tile, so it writes cells x=2..3.
    private static List<BlobbityCard> Claim(Tile start, Tile finish, params BlobbityCard[] hand)
    {
        var (grid, located, _) = Ordinary.Place(Grid.FromStart(start), finish, new Place(1, 0, 0));
        return Ordinary.CompletedBlobbities(grid, located, hand);
    }
}
