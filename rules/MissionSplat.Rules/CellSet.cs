namespace MissionSplat.Rules;

// Four cells in a fixed order, so the same cells found from any origin compare equal and color plays no part.
internal readonly record struct CellSet(CellCoord First, CellCoord Second, CellCoord Third, CellCoord Fourth)
{
    public static CellSet Of(CellCoord a, CellCoord b, CellCoord c, CellCoord d)
    {
        CellCoord[] four = [a, b, c, d];
        Array.Sort(four, (p, q) => p.X != q.X ? p.X.CompareTo(q.X) : p.Y.CompareTo(q.Y));
        return new CellSet(four[0], four[1], four[2], four[3]);
    }
}
