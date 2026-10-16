namespace MissionSplat.Player
{

using System.Collections.Generic;
using MissionSplat.App;
using MissionSplat.Rules;

// The one line that stays after a recap: what it showed, in plain words, one sentence per run of items by the same seat.
internal static class RecapSummary
{
    // What an item shows, for the heading while it plays.
    public static string Caption(RecapItem item) => item switch
    {
        RecapPlaced => Seats.NameOf(item.Seat) + " places a tile",
        RecapRotated => Seats.NameOf(item.Seat) + " rotates a tile",
        RecapBounced => Seats.NameOf(item.Seat) + " bounces a tile",
        RecapClaimed => Seats.NameOf(item.Seat) + " claims!",
        RecapWon => Seats.NameOf(item.Seat) + " wins!",
        RecapReshuffled => "Board reshuffled",
        _ => throw new System.ArgumentException("Unknown recap item " + item.GetType().Name + ".", nameof(item)),
    };

    // Null when there was nothing to show.
    public static string Of(IReadOnlyList<RecapItem> items)
    {
        if (items.Count == 0)
        {
            return null;
        }

        var sentences = new List<string>();
        var run = new List<RecapItem>();
        foreach (var item in items)
        {
            if (run.Count > 0 && !run[0].Seat.Equals(item.Seat))
            {
                sentences.Add(Sentence(run));
                run.Clear();
            }

            run.Add(item);
        }

        sentences.Add(Sentence(run));
        return string.Join(" ", sentences);
    }

    private static string Sentence(List<RecapItem> run) => Seats.NameOf(run[0].Seat) + " " + Joined(DeedsOf(run)) + ".";

    // A run of claims reads as one deed: "claimed a row and an L".
    private static List<string> DeedsOf(List<RecapItem> run)
    {
        var deeds = new List<string>();
        var shapes = new List<string>();
        foreach (var item in run)
        {
            if (item is RecapClaimed claimed)
            {
                shapes.Add(Shape(claimed.Card.Pattern));
                continue;
            }

            AddClaims(deeds, shapes);
            deeds.Add(Deed(item));
        }

        AddClaims(deeds, shapes);
        return deeds;
    }

    private static void AddClaims(List<string> deeds, List<string> shapes)
    {
        if (shapes.Count > 0)
        {
            deeds.Add("claimed " + Joined(shapes));
            shapes.Clear();
        }
    }

    // Claims are folded into their run, so only the other kinds come here.
    private static string Deed(RecapItem item) => item switch
    {
        RecapPlaced => "placed a tile",
        RecapRotated => "rotated a tile",
        RecapBounced => "bounced a tile",
        RecapWon => "won the game",
        RecapReshuffled => "reshuffled the board",
        _ => throw new System.ArgumentException("Unknown recap item " + item.GetType().Name + ".", nameof(item)),
    };

    private static string Shape(BlobbityPattern pattern) => pattern switch
    {
        BlobbityPattern.Row => "a row",
        BlobbityPattern.Square => "a square",
        BlobbityPattern.L => "an L",
        _ => throw new System.ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown blobbity pattern."),
    };

    // "a", "a and b", "a, b and c"; commas only when a part already holds an "and", so no sentence says "and" twice.
    private static string Joined(List<string> parts)
    {
        if (parts.Count == 1)
        {
            return parts[0];
        }

        var last = parts[parts.Count - 1];
        var glue = parts.Exists(part => part.Contains(" and ")) ? ", " : " and ";
        return string.Join(", ", parts.GetRange(0, parts.Count - 1)) + glue + last;
    }
}
}
