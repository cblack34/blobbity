namespace MissionSplat.App.Tests;

using MissionSplat.App;
using Blobbity.Rules;

public class TableDeckTests
{
    private static readonly SeatId[] Seats = [new SeatId("1"), new SeatId("2")];

    // Written out from docs/census.md, top-left, top-right, bottom-left, bottom-right, independent of the deck's own encoding.
    private static readonly string[] CensusRows =
    [
        "blank wildcard red purple",
        "green blue blue green",
        "red purple red bounce",
        "green blue wildcard blank",
        "green blank blue stack",
        "wildcard blue blank purple",
        "green blank red wildcard",
        "rotate red blue blank",
        "purple red purple red",
        "green blue green stack",
        "purple stack blank purple",
        "stack blue green purple",
        "red green green red",
        "blank red red rotate",
        "bounce green green blank",
        "rotate wildcard blank blank",
        "purple blue green red",
        "blank wildcard rotate wildcard",
        "purple blue green red",
        "blank rotate purple green",
        "red blue green rotate",
        "blue purple blue purple",
        "purple blue stack red",
        "green purple red rotate",
        "purple blank red stack",
        "rotate purple red purple",
        "blue blank stack blue",
        "blue blue rotate green",
        "purple blank rotate green",
        "blue blank red rotate",
    ];

    [Test]
    public void ANullSeed_KeepsTheAuthoredOrder()
    {
        var setup = TableDeck.Ordinary(Seats, Seats[0], null);

        Assert.That(setup.MatchDeck[0].Id.Value, Is.EqualTo("census-01"));
        Assert.That(setup.MatchDeck[1].Id.Value, Is.EqualTo("census-02"));
        Assert.That(setup.MatchDeck[29].Id.Value, Is.EqualTo("census-30"));
        Assert.That(Ids(TableDeck.Ordinary(Seats, Seats[0], null)), Is.EqualTo(Ids(setup)));
        Assert.That(setup.BlobbityDeck, Has.Count.EqualTo(24));
        Assert.That(setup.MatchDeck, Has.Count.EqualTo(30));
    }

    [Test]
    public void TheMatchDeck_IsTheCensus_CellByCell()
    {
        var tiles = TableDeck.Ordinary(Seats, Seats[0], null).MatchDeck;

        Assert.That(tiles, Has.Count.EqualTo(CensusRows.Length));
        for (var i = 0; i < CensusRows.Length; i++)
        {
            var expected = CensusRows[i].Split(' ').Select(Describe).ToArray();
            var tile = tiles[i];
            // Tile cells count y upward, so the census's top-left is Local(0, 1); cells are compared by their text form.
            var actual = new[] { tile.Local(0, 1), tile.Local(1, 1), tile.Local(0, 0), tile.Local(1, 0) }
                .Select(cell => cell.ToString())
                .ToArray();

            Assert.That(tile.Id.Value, Is.EqualTo("census-" + (i + 1).ToString("00")));
            Assert.That(actual, Is.EqualTo(expected), "census row " + (i + 1));
        }
    }

    [Test]
    public void TheShippedDeck_IsNamedCensus()
    {
        Assert.That(TableDeck.Name, Is.EqualTo("census"));
    }

    [Test]
    public void TheBlobbityDeck_HoldsTwoOfEveryColorAndShape()
    {
        var blobbities = TableDeck.Ordinary(Seats, Seats[0], null).BlobbityDeck;

        foreach (var pattern in OrdinaryCatalog.Patterns)
        {
            foreach (var color in OrdinaryCatalog.Colors)
            {
                Assert.That(blobbities.Count(blobbity => blobbity.Pattern == pattern && blobbity.Color.Equals(color)), Is.EqualTo(2), pattern + " " + color.Value);
            }
        }

        Assert.That(blobbities.Select(blobbity => blobbity.Id.Value).Distinct().Count(), Is.EqualTo(24));
    }

    [Test]
    public void NoTile_ShowsTwoDifferentPowers()
    {
        foreach (var tile in TableDeck.Ordinary(Seats, Seats[0], null).MatchDeck)
        {
            var powers = new[] { tile.Local(0, 0), tile.Local(1, 0), tile.Local(0, 1), tile.Local(1, 1) }
                .Where(cell => cell.TryGetSymbol(out var symbol) && !symbol.Equals(OrdinaryCatalog.Blank))
                .Select(cell => cell.ToString())
                .Distinct();

            Assert.That(powers.Count(), Is.LessThanOrEqualTo(1), tile.Id.Value);
        }
    }

    [Test]
    public void TheSameSeed_DealsTheSameOrder_ADifferentSeedDoesNot()
    {
        var seeded = Ids(TableDeck.Ordinary(Seats, Seats[0], 11));

        Assert.That(Ids(TableDeck.Ordinary(Seats, Seats[0], 11)), Is.EqualTo(seeded));
        Assert.That(Ids(TableDeck.Ordinary(Seats, Seats[0], 12)), Is.Not.EqualTo(seeded));
        Assert.That(seeded, Is.Not.EqualTo(Ids(TableDeck.Ordinary(Seats, Seats[0], null))));
    }

    [Test]
    public void AShuffle_KeepsEveryTileAndBlobbity()
    {
        var authored = TableDeck.Ordinary(Seats, Seats[0], null);
        var shuffled = TableDeck.Ordinary(Seats, Seats[0], 99);

        Assert.That(shuffled.MatchDeck.Select(tile => tile.Id.Value), Is.EquivalentTo(authored.MatchDeck.Select(tile => tile.Id.Value)));
        Assert.That(shuffled.BlobbityDeck.Select(blobbity => blobbity.Id.Value), Is.EquivalentTo(authored.BlobbityDeck.Select(blobbity => blobbity.Id.Value)));
        Assert.That(Game.Start(shuffled).IsAccepted, Is.True);
    }

    private static string Describe(string word) => word switch
    {
        "red" or "blue" or "green" or "purple" => "color:" + word,
        "wildcard" => "wild",
        "blank" or "rotate" or "stack" or "bounce" => "symbol:" + word,
        _ => throw new ArgumentException("Unknown census cell " + word),
    };

    private static string[] Ids(GameSetup setup) =>
        setup.MatchDeck.Select(tile => tile.Id.Value).Concat(setup.BlobbityDeck.Select(blobbity => blobbity.Id.Value)).ToArray();
}
