namespace MissionSplat.Rules;

internal static class PatternSearch
{
    private static readonly (int X, int Y)[][] Rows =
    [
        [(0, 0), (1, 0), (2, 0), (3, 0)],
        [(0, 0), (0, 1), (0, 2), (0, 3)],
        [(0, 0), (1, 1), (2, 2), (3, 3)],
        [(0, 0), (1, -1), (2, -2), (3, -3)],
    ];

    private static readonly (int X, int Y)[][] Squares =
    [
        [(0, 0), (1, 0), (0, 1), (1, 1)],
    ];

    // Length-3 arm with a foot on one end. Eight rotations and reflections.
    // A T, a skew, and a square are not here.
    private static readonly (int X, int Y)[][] Ls =
    [
        [(0, 0), (0, 1), (0, 2), (1, 0)],
        [(0, 0), (0, 1), (1, 1), (2, 1)],
        [(0, 2), (1, 0), (1, 1), (1, 2)],
        [(0, 0), (1, 0), (2, 0), (2, 1)],
        [(0, 0), (1, 0), (1, 1), (1, 2)],
        [(0, 0), (0, 1), (1, 0), (2, 0)],
        [(0, 0), (0, 1), (0, 2), (1, 2)],
        [(0, 1), (1, 1), (2, 0), (2, 1)],
    ];

    // Every distinct four-cell set that makes the pattern in the color and holds a written cell.
    public static IReadOnlyCollection<CellSet> CompletingSets(
        BlobbityPattern pattern,
        ColorId color,
        IReadOnlyDictionary<CellCoord, Cell> cells,
        HashSet<CellCoord> written)
    {
        var found = new HashSet<CellSet>();
        foreach (var shape in Shapes(pattern))
        {
            foreach (var origin in cells.Keys)
            {
                foreach (var pin in shape)
                {
                    if (TryMatch(origin.X - pin.X, origin.Y - pin.Y, shape, color, cells, written, out var set))
                    {
                        found.Add(set);
                    }
                }
            }
        }

        return found;
    }

    private static (int X, int Y)[][] Shapes(BlobbityPattern pattern) => pattern switch
    {
        BlobbityPattern.Row => Rows,
        BlobbityPattern.Square => Squares,
        BlobbityPattern.L => Ls,
        _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "That pattern has no matcher."),
    };

    private static bool TryMatch(
        int originX,
        int originY,
        (int X, int Y)[] shape,
        ColorId color,
        IReadOnlyDictionary<CellCoord, Cell> cells,
        HashSet<CellCoord> written,
        out CellSet set)
    {
        var coords = new CellCoord[shape.Length];
        var touchesPlacement = false;
        for (var i = 0; i < shape.Length; i++)
        {
            coords[i] = new CellCoord(originX + shape[i].X, originY + shape[i].Y);
            if (!cells.TryGetValue(coords[i], out var cell) || !cell.CountsAs(color))
            {
                set = default;
                return false;
            }

            touchesPlacement |= written.Contains(coords[i]);
        }

        set = CellSet.Of(coords[0], coords[1], coords[2], coords[3]);
        return touchesPlacement;
    }
}
