namespace MissionSplat.App;

using System.Globalization;
using MissionSplat.Rules;

public static class TableDeck
{
    public const string Name = "census";

    public static GameSetup Ordinary(IReadOnlyList<SeatId> seats, SeatId firstSeat, int? shuffleSeed)
    {
        if (seats is null)
        {
            throw new ArgumentNullException(nameof(seats));
        }

        var missions = Missions();
        var tiles = Tiles();
        if (shuffleSeed is int seed)
        {
            var rng = new Random(seed);
            Shuffle.InPlace(missions, rng);
            Shuffle.InPlace(tiles, rng);
        }

        return new GameSetup(
            seats,
            firstSeat,
            OrdinaryCatalog.Colors,
            OrdinaryCatalog.NonScoringSymbols,
            OrdinaryCatalog.Patterns,
            OrdinaryCatalog.ClaimsRequiredToWin,
            missions,
            tiles);
    }

    // Two of each colour and shape: the digital deck doubles the twelve printed cards so the mission deck cannot run out.
    private static Mission[] Missions()
    {
        var patterns = new[] { MissionPattern.Row, MissionPattern.Square, MissionPattern.L };
        var names = new[] { "line", "square", "l" };
        var colors = OrdinaryCatalog.Colors;
        const int copies = 2;
        var missions = new Mission[patterns.Length * copies * colors.Count];
        var index = 0;
        for (var pattern = 0; pattern < patterns.Length; pattern++)
        {
            for (var copy = 1; copy <= copies; copy++)
            {
                for (var color = 0; color < colors.Count; color++)
                {
                    var id = names[pattern] + "-" + colors[color].Value + "-" + copy;
                    missions[index++] = new Mission(new MissionId(id), patterns[pattern], colors[color]);
                }
            }
        }

        return missions;
    }

    // Rows follow docs/census.md: each lists top-left, top-right, bottom-left, bottom-right.
    private static Tile[] Tiles() =>
        [
            Row(1, Blank, Wildcard, Red, Purple),
            Row(2, Green, Blue, Blue, Green),
            Row(3, Red, Purple, Red, Bounce),
            Row(4, Green, Blue, Wildcard, Blank),
            Row(5, Green, Blank, Blue, Stack),
            Row(6, Wildcard, Blue, Blank, Purple),
            Row(7, Green, Blank, Red, Wildcard),
            Row(8, Rotate, Red, Blue, Blank),
            Row(9, Purple, Red, Purple, Red),
            Row(10, Green, Blue, Green, Stack),
            Row(11, Purple, Stack, Blank, Purple),
            Row(12, Stack, Blue, Green, Purple),
            Row(13, Red, Green, Green, Red),
            Row(14, Blank, Red, Red, Rotate),
            Row(15, Bounce, Green, Green, Blank),
            Row(16, Rotate, Wildcard, Blank, Blank),
            Row(17, Purple, Blue, Green, Red),
            Row(18, Blank, Wildcard, Rotate, Wildcard),
            Row(19, Purple, Blue, Green, Red),
            Row(20, Blank, Rotate, Purple, Green),
            Row(21, Red, Blue, Green, Rotate),
            Row(22, Blue, Purple, Blue, Purple),
            Row(23, Purple, Blue, Stack, Red),
            Row(24, Green, Purple, Red, Rotate),
            Row(25, Purple, Blank, Red, Stack),
            Row(26, Rotate, Purple, Red, Purple),
            Row(27, Blue, Blank, Stack, Blue),
            Row(28, Blue, Blue, Rotate, Green),
            Row(29, Purple, Blank, Rotate, Green),
            Row(30, Blue, Blank, Red, Rotate),
        ];

    // The constructor's y runs upward, so the census's top row becomes the high y.
    private static Tile Row(int number, Cell topLeft, Cell topRight, Cell bottomLeft, Cell bottomRight) =>
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
