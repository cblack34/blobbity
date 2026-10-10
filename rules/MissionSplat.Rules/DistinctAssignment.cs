namespace MissionSplat.Rules;

// Picks entries that can each take a different set from their own candidates.
internal static class DistinctAssignment
{
    // The indices chosen, in input order: the largest count that can each hold a distinct set, preferring earlier
    // entries. An entry with no candidates is never chosen.
    public static IReadOnlyList<int> Choose(IReadOnlyList<IReadOnlyCollection<CellSet>> candidateSets)
    {
        var chosen = new List<int>();
        var kept = new List<IReadOnlyCollection<CellSet>>();
        for (var i = 0; i < candidateSets.Count; i++)
        {
            var entry = candidateSets[i];

            // Greedy is exact because the choosable sets form a transversal matroid.
            if (entry.Count > 0 && Fits([.. kept, entry], 0, []))
            {
                kept.Add(entry);
                chosen.Add(i);
            }
        }

        return chosen;
    }

    // Backtracks over each entry's candidates for one choice with no set used twice.
    private static bool Fits(
        IReadOnlyList<IReadOnlyCollection<CellSet>> entries,
        int next,
        HashSet<CellSet> used)
    {
        if (next == entries.Count)
        {
            return true;
        }

        foreach (var set in entries[next])
        {
            if (used.Add(set))
            {
                var fits = Fits(entries, next + 1, used);
                used.Remove(set);
                if (fits)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
