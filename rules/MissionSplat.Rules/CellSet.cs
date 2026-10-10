namespace MissionSplat.Rules;

// Four cells in a fixed order, so the same cells found from any origin compare equal and color plays no part.
internal readonly record struct CellSet(CellCoord First, CellCoord Second, CellCoord Third, CellCoord Fourth)
{
    public static CellSet Of(CellCoord[] four)
    {
        Array.Sort(four, (a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
        return new CellSet(four[0], four[1], four[2], four[3]);
    }
}
