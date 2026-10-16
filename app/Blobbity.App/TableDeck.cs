namespace Blobbity.App;

using System.Globalization;
using Blobbity.Rules;

public static class TableDeck
{
    public const string Name = "census";

    public static GameSetup Ordinary(IReadOnlyList<SeatId> seats, SeatId firstSeat, int? shuffleSeed)
    {
        if (seats is null)
        {
            throw new ArgumentNullException(nameof(seats));
        }

        var blobbities = Blobbities();
        var tiles = Tiles();
        if (shuffleSeed is int seed)
        {
            var rng = new Random(seed);
            Shuffle.InPlace(blobbities, rng);
            Shuffle.InPlace(tiles, rng);
        }

        return new GameSetup(
            seats,
            firstSeat,
            OrdinaryCatalog.Colors,
            OrdinaryCatalog.NonScoringSymbols,
            OrdinaryCatalog.Patterns,
            OrdinaryCatalog.ClaimsRequiredToWin,
            blobbities,
            tiles);
    }

    // Two of each colour and shape: the digital deck doubles the twelve printed cards so the blobbity deck cannot run out.
    private static BlobbityCard[] Blobbities()
    {
        var shapes = new[] { (BlobbityPattern.Row, "line"), (BlobbityPattern.Square, "square"), (BlobbityPattern.L, "l") };
        const int copies = 2;
        var blobbities = new List<BlobbityCard>();
        foreach (var (pattern, name) in shapes)
        {
            for (var copy = 1; copy <= copies; copy++)
            {
                foreach (var color in OrdinaryCatalog.Colors)
                {
                    blobbities.Add(new BlobbityCard(new BlobbityId(name + "-" + color.Value + "-" + copy), pattern, color));
                }
            }
        }

        return blobbities.ToArray();
    }

    // Rows follow docs/census.md: each lists top-left, top-right, bottom-left, bottom-right.
    private static Tile[] Tiles() =>
        [
            CensusTile(1, Blank, Wildcard, Red, Purple),
            CensusTile(2, Green, Blue, Blue, Green),
            CensusTile(3, Red, Purple, Red, Bounce),
            CensusTile(4, Green, Blue, Wildcard, Blank),
            CensusTile(5, Green, Blank, Blue, Stack),
            CensusTile(6, Wildcard, Blue, Blank, Purple),
            CensusTile(7, Green, Blank, Red, Wildcard),
            CensusTile(8, Rotate, Red, Blue, Blank),
            CensusTile(9, Purple, Red, Purple, Red),
            CensusTile(10, Green, Blue, Green, Stack),
            CensusTile(11, Purple, Stack, Blank, Purple),
            CensusTile(12, Stack, Blue, Green, Purple),
            CensusTile(13, Red, Green, Green, Red),
            CensusTile(14, Blank, Red, Red, Rotate),
            CensusTile(15, Bounce, Green, Green, Blank),
            CensusTile(16, Rotate, Wildcard, Blank, Blank),
            CensusTile(17, Purple, Blue, Green, Red),
            CensusTile(18, Blank, Wildcard, Rotate, Wildcard),
            CensusTile(19, Purple, Blue, Green, Red),
            CensusTile(20, Blank, Rotate, Purple, Green),
            CensusTile(21, Red, Blue, Green, Rotate),
            CensusTile(22, Blue, Purple, Blue, Purple),
            CensusTile(23, Purple, Blue, Stack, Red),
            CensusTile(24, Green, Purple, Red, Rotate),
            CensusTile(25, Purple, Blank, Red, Stack),
            CensusTile(26, Rotate, Purple, Red, Purple),
            CensusTile(27, Blue, Blank, Stack, Blue),
            CensusTile(28, Blue, Blue, Rotate, Green),
            CensusTile(29, Purple, Blank, Rotate, Green),
            CensusTile(30, Blue, Blank, Red, Rotate),
        ];

    // The constructor's y runs upward, so the census's top row becomes the high y.
    private static Tile CensusTile(int number, Cell topLeft, Cell topRight, Cell bottomLeft, Cell bottomRight) =>
        new(new TileId("census-" + number.ToString("00", CultureInfo.InvariantCulture)), bottomLeft, bottomRight, topLeft, topRight);

    private static Cell Red { get; } = Cell.Color(OrdinaryCatalog.Red);

    private static Cell Blue { get; } = Cell.Color(OrdinaryCatalog.Blue);

    private static Cell Green { get; } = Cell.Color(OrdinaryCatalog.Green);

    private static Cell Purple { get; } = Cell.Color(OrdinaryCatalog.Purple);

    private static Cell Blank { get; } = Cell.Symbol(OrdinaryCatalog.Blank);

    private static Cell Wildcard { get; } = Cell.Wild();

    private static Cell Rotate { get; } = Cell.Symbol(OrdinaryCatalog.Rotate);

    private static Cell Stack { get; } = Cell.Symbol(OrdinaryCatalog.Stack);

    private static Cell Bounce { get; } = Cell.Symbol(OrdinaryCatalog.Bounce);
}
