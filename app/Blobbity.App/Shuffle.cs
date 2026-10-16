namespace Blobbity.App;

// The one Fisher-Yates, so a seed deals the same order wherever the table shuffles.
internal static class Shuffle
{
    public static void InPlace<T>(IList<T> items, Random random)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
