namespace Blobbity.Rules;

public enum RejectionReason
{
    InvalidSetup,
    GameOver,
    NotYourTurn,
    InvalidQuarterTurns,
    NotAtOrigin,
    DoesNotShareFullSide,
    CellOccupied,
    NoUseRemaining,
    InvalidRotateQuarterTurns,
    NoTileToRotate,
    TileSurrounded,
    NoTileToBounce,
    AwaitingReshuffle,
    NotAwaitingReshuffle,
    InvalidReshuffleOrder,
}

public sealed record Rejection(RejectionReason Reason, string Message);
