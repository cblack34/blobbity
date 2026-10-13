namespace MissionSplat.Player
{

using System.Collections.Generic;
using MissionSplat.App;
using MissionSplat.Rules;

// The one line that stays after a recap: what it showed, in plain words, one sentence per run of items by the same seat.
internal static class RecapSummary
{
    // Null when there was nothing to show.
    public static string Of(IReadOnlyList<RecapItem> items)
    {
        if (items.Count == 0)
        {
            return null;
        }

        var sentences = new List<string>();
        var i = 0;
        while (i < items.Count)
        {
            var seat = items[i].Seat;
            var deeds = new List<string>();
            while (i < items.Count && items[i].Seat.Equals(seat))
            {
                deeds.Add(items[i] is RecapClaimed ? ClaimRun(items, seat, ref i) : Deed(items[i++]));
            }

            sentences.Add(Seats.NameOf(seat) + " " + Joined(deeds) + ".");
        }

        return string.Join(" ", sentences);
    }

    // A run of claims by one seat reads as one deed: "claimed a row and an L". Moves i past the run.
    private static string ClaimRun(IReadOnlyList<RecapItem> items, SeatId seat, ref int i)
    {
        var shapes = new List<string>();
        while (i < items.Count && items[i].Seat.Equals(seat) && items[i] is RecapClaimed claimed)
        {
            shapes.Add(Shape(claimed.Mission.Pattern));
            i++;
        }

        return "claimed " + Joined(shapes);
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

    private static string Shape(MissionPattern pattern) => pattern switch
    {
        MissionPattern.Row => "a row",
        MissionPattern.Square => "a square",
        MissionPattern.L => "an L",
        _ => throw new System.ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown mission pattern."),
    };

    // "a", "a and b", "a, b and c".
    private static string Joined(List<string> deeds) =>
        deeds.Count == 1
            ? deeds[0]
            : string.Join(", ", deeds.GetRange(0, deeds.Count - 1)) + " and " + deeds[deeds.Count - 1];
}
}
