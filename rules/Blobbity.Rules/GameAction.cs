namespace Blobbity.Rules;

public abstract record GameAction
{
    // An abstract member no other assembly can see cannot be overridden there, so the action set is closed to this assembly.
    internal abstract void MarkClosed();
}

// One rotate cell on the drawn tile, spent on a tile already on the board. Zero quarter-turns is not a rotate.
public sealed record UseRotate(int TileX, int TileY, int QuarterTurnsClockwise) : GameAction
{
    internal override void MarkClosed()
    {
    }
}

public sealed record UseBounce(int TileX, int TileY) : GameAction
{
    internal override void MarkClosed()
    {
    }
}

// The drawn tile covers the 2×2 cells at (2*TileX + lx, 2*TileY + ly), y upward. Quarter-turns are clockwise and
// only orient this placement. On an occupied position the placement is the stack.
public sealed record Place(int TileX, int TileY, int QuarterTurnsClockwise) : GameAction
{
    internal override void MarkClosed()
    {
    }
}

// Only while the match awaits a reshuffle: the full set of board tiles, visible and buried, in the new draw order.
// The first becomes the start tile at the origin and the rest the draw deck.
public sealed record ReshuffleBoard(IReadOnlyList<TileId> Order) : GameAction
{
    public IReadOnlyList<TileId> Order { get; } = Order.ToArray();

    internal override void MarkClosed()
    {
    }
}
