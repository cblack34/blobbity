namespace Blobbity.Player
{

using System;
using System.Collections.Generic;
using System.Linq;
using Blobbity.App;
using Blobbity.Rules;

internal enum PreviewKind
{
    Placement,
    Rotate,
    Bounce,
}

// What the player is trying on the board before anything is submitted: the drawn tile at a spot, or a board tile selected for a rotate or bounce.
// View state only. The table learns of it when the strip's Place, Confirm, or Bounce is pressed. It is tied to the drawn tile of the turn it began on,
// so it never outlives that turn, and Holds says whether the snapshot still offers it.
internal sealed class TilePreview
{
    // The powers a tap on a board tile can select, in the order a tap tries them.
    private static readonly PreviewKind[] PowerKinds = { PreviewKind.Rotate, PreviewKind.Bounce };

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

    // Whether Confirm has anything to submit: a rotate by no turns is no rotate.
    public bool CanConfirm => Kind switch
    {
        PreviewKind.Placement => true,
        PreviewKind.Rotate => Turns > 0,
        PreviewKind.Bounce => true,
        _ => throw Unknown(Kind),
    };

    // The power's action for a rotate or a bounce that can be confirmed; null otherwise, and for a placement,
    // which the table places with its own Spin turns.
    public GameAction ToAction() => Kind switch
    {
        PreviewKind.Placement => null,
        PreviewKind.Rotate => CanConfirm ? new UseRotate(TileX, TileY, Turns) : null,
        PreviewKind.Bounce => new UseBounce(TileX, TileY),
        _ => throw Unknown(Kind),
    };

    // The board positions a tap selects as the target of a power the drawn tile still has a use of. A drawn tile never offers two different powers,
    // so a position is at most one kind of target.
    public static IEnumerable<BoardPosition> PowerTargets(TableSnapshot snapshot) =>
        PowerKinds.SelectMany(kind => TargetsOf(snapshot, PowerOf(kind)));

    // What a tap at the tile position starts: a power target selects that tile, a legal spot previews the drawn tile there.
    // Null when the tap means nothing, including while a recap is pending.
    public static TilePreview Tapped(TableSnapshot snapshot, int tileX, int tileY)
    {
        if (snapshot.View.PendingMatchTile is not { } drawn)
        {
            return null;
        }

        var kind = PreviewKind.Placement;
        foreach (var power in PowerKinds)
        {
            if (IsTarget(snapshot, PowerOf(power), tileX, tileY))
            {
                kind = power;
                break;
            }
        }

        var tried = new TilePreview(kind, tileX, tileY, drawn.Id.Value, 0);
        return tried.Holds(snapshot) ? tried : null;
    }

    // This preview's position, whatever it is trying there.
    public bool IsAt(int tileX, int tileY) => TileX == tileX && TileY == tileY;

    // A board tile selected for a power, as against a spot the drawn tile is tried at.
    public bool SelectsBoardTile(int tileX, int tileY) => Kind != PreviewKind.Placement && IsAt(tileX, tileY);

    // One more clockwise quarter-turn of a rotate, back to none after three.
    public TilePreview Turned() => new(Kind, TileX, TileY, Tile, (Turns + 1) % 4);

    // The preview holds only while the same drawn tile still offers its spot or target, with no recap in the way.
    public bool Holds(TableSnapshot snapshot) =>
        snapshot.Recap.Count == 0
        && snapshot.View.PendingMatchTile?.Id.Value == Tile
        && Kind switch
        {
            PreviewKind.Placement => snapshot.LegalPlacements.Any(spot => spot.TileX == TileX && spot.TileY == TileY),
            PreviewKind.Rotate or PreviewKind.Bounce => IsTarget(snapshot, PowerOf(Kind), TileX, TileY),
            _ => throw Unknown(Kind),
        };

    // The one place a kind of preview is tied to its power.
    private static SymbolId PowerOf(PreviewKind kind) => kind switch
    {
        PreviewKind.Rotate => OrdinaryCatalog.Rotate,
        PreviewKind.Bounce => OrdinaryCatalog.Bounce,
        _ => throw Unknown(kind),
    };

    private static ArgumentOutOfRangeException Unknown(PreviewKind kind) =>
        new(nameof(kind), kind, "Unknown kind of preview.");

    private static bool IsTarget(TableSnapshot snapshot, SymbolId power, int tileX, int tileY) =>
        TargetsOf(snapshot, power).Any(target => target.TileX == tileX && target.TileY == tileY);

    // The power's legal targets; the table offers none once the drawn tile has no use of it left.
    private static IEnumerable<BoardPosition> TargetsOf(TableSnapshot snapshot, SymbolId power) =>
        snapshot.LegalTargets.Where(entry => entry.Power.Equals(power)).SelectMany(entry => entry.Targets);
}
}
