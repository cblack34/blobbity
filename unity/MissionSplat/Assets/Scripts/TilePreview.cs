namespace MissionSplat.Player
{

using System.Collections.Generic;
using System.Linq;
using MissionSplat.App;
using MissionSplat.Rules;

internal enum PreviewKind
{
    Placement,
    Rotate,
    Bounce,
}

// What the player is trying on the board before anything is submitted: the drawn tile at a spot, or a board tile selected for a rotate or a bounce.
// View state only. The table learns of it when the strip's Place, Confirm, or Bounce is pressed. It is tied to the drawn tile of the turn it began on,
// so it never outlives that turn, and Holds says whether the snapshot still offers it.
internal sealed class TilePreview
{
    private TilePreview(PreviewKind kind, int tileX, int tileY, string tile, int turns)
    {
        Kind = kind;
        TileX = tileX;
        TileY = tileY;
        Tile = tile;
        Turns = turns;
    }

    public PreviewKind Kind { get; }

    public int TileX { get; }

    public int TileY { get; }

    // The drawn tile this preview belongs to.
    public string Tile { get; }

    // Clockwise quarter-turns a rotate is set to; always 0 for the other kinds.
    public int Turns { get; }

    // The board positions a tap selects as the target of a power the drawn tile still has a use of. A drawn tile never offers two different powers,
    // so a position is at most one kind of target.
    public static IEnumerable<BoardPosition> PowerTargets(TableSnapshot snapshot) =>
        Powers.SelectMany(power => TargetsOf(snapshot, power));

    // What a tap at the tile position starts: a power target selects that tile, a legal spot previews the drawn tile there. Null when the tap means nothing.
    public static TilePreview Tapped(TableSnapshot snapshot, int tileX, int tileY)
    {
        if (snapshot.View.PendingMatchTile is not { } drawn)
        {
            return null;
        }

        var tile = drawn.Id.Value;
        if (IsTarget(snapshot, OrdinaryCatalog.Rotate, tileX, tileY))
        {
            return new TilePreview(PreviewKind.Rotate, tileX, tileY, tile, 0);
        }

        if (IsTarget(snapshot, OrdinaryCatalog.Bounce, tileX, tileY))
        {
            return new TilePreview(PreviewKind.Bounce, tileX, tileY, tile, 0);
        }

        return snapshot.LegalPlacements.Any(spot => spot.TileX == tileX && spot.TileY == tileY)
            ? new TilePreview(PreviewKind.Placement, tileX, tileY, tile, 0)
            : null;
    }

    // One more clockwise quarter-turn of a rotate, back to none after three.
    public TilePreview Turned() => new(Kind, TileX, TileY, Tile, (Turns + 1) % 4);

    // The preview holds only while the same drawn tile still offers its spot or target, with no recap in the way.
    public bool Holds(TableSnapshot snapshot) =>
        snapshot.Recap.Count == 0
        && snapshot.View.PendingMatchTile?.Id.Value == Tile
        && Kind switch
        {
            PreviewKind.Rotate => IsTarget(snapshot, OrdinaryCatalog.Rotate, TileX, TileY),
            PreviewKind.Bounce => IsTarget(snapshot, OrdinaryCatalog.Bounce, TileX, TileY),
            _ => snapshot.LegalPlacements.Any(spot => spot.TileX == TileX && spot.TileY == TileY),
        };

    private static readonly SymbolId[] Powers = { OrdinaryCatalog.Rotate, OrdinaryCatalog.Bounce };

    private static bool IsTarget(TableSnapshot snapshot, SymbolId power, int tileX, int tileY) =>
        TargetsOf(snapshot, power).Any(target => target.TileX == tileX && target.TileY == tileY);

    // The power's legal targets, if the drawn tile has a use of it left.
    private static IEnumerable<BoardPosition> TargetsOf(TableSnapshot snapshot, SymbolId power) =>
        snapshot.View.RemainingUses.Any(charge => charge.Power.Equals(power) && charge.Remaining > 0)
            ? snapshot.LegalTargets.Where(entry => entry.Power.Equals(power)).SelectMany(entry => entry.Targets)
            : Enumerable.Empty<BoardPosition>();
}
}
