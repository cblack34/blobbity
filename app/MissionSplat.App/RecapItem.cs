namespace MissionSplat.App;

using MissionSplat.Rules;

// One public fact a human missed since they last held the device. A closed set a GUI can animate in order;
// nothing here names a hidden mission or the draw order, so it is safe to show on any seat's turn.
public abstract record RecapItem(SeatId Seat);

// A tile put on the board. Cells are the four board cells as placed, so a GUI draws it without turning it again.
public sealed record RecapPlaced(
    SeatId Seat,
    TileId Tile,
    int TileX,
    int TileY,
    int QuarterTurnsClockwise,
    IReadOnlyList<OccupiedCell> Cells,
    bool Covered) : RecapItem(Seat);

public sealed record RecapRotated(SeatId Seat, int TileX, int TileY, int QuarterTurnsClockwise) : RecapItem(Seat);

public sealed record RecapBounced(SeatId Seat, int TileX, int TileY) : RecapItem(Seat);

// A claimed mission is public, so the card itself rides along.
public sealed record RecapClaimed(SeatId Seat, Mission Mission) : RecapItem(Seat);

public sealed record RecapWon(SeatId Seat, int ClaimCount) : RecapItem(Seat);

// The board was reshuffled; the new order is deliberately absent.
public sealed record RecapReshuffled(SeatId Seat) : RecapItem(Seat);
