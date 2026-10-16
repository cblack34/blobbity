namespace MissionSplat.Player
{

using System;
using Blobbity.App;
using Blobbity.Rules;

// What the player calls a seat, and where it sits, in one place.
internal static class Seats
{
    // The claims a seat needs to win, for the seat bar's pips and the game-end line. The session view does not carry the
    // setup's count yet, so this is the ordinary one; issue #51 replaces it with the configured count.
    public static int ClaimsToWin => OrdinaryCatalog.ClaimsRequiredToWin;

    public static string NameOf(SeatId seat) => "Seat " + seat.Value;

    // A seat's position in turn order, which also picks its critter colour. view.Claims lists every seat once, in fixed seat order.
    public static int IndexOf(SeatView view, SeatId seat)
    {
        for (var i = 0; i < view.Claims.Count; i++)
        {
            if (view.Claims[i].Seat.Equals(seat))
            {
                return i;
            }
        }

        throw new ArgumentException("That seat is not at the table.", nameof(seat));
    }
}
}
