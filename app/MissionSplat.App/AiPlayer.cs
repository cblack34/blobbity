namespace MissionSplat.App;

using Blobbity.Rules;

public sealed class AiPlayer : IPlayer
{
    private readonly Func<int, IReadOnlyList<LegalPlacement>> _legalPlacements;
    private readonly Func<GameAction, ActionPreview> _preview;

    public AiPlayer(
        SeatId seat,
        Func<int, IReadOnlyList<LegalPlacement>> legalPlacements,
        Func<GameAction, ActionPreview> preview)
    {
        _legalPlacements = legalPlacements ?? throw new ArgumentNullException(nameof(legalPlacements));
        _preview = preview ?? throw new ArgumentNullException(nameof(preview));
        Seat = seat;
    }

    public SeatId Seat { get; }

    // It only places beside: a power use or a stack is never chosen.
    public GameAction ChooseAction(SeatView view)
    {
        if (view is null)
        {
            throw new ArgumentNullException(nameof(view));
        }

        if (!view.Seat.Equals(Seat))
        {
            throw new ArgumentException("The view is for a different seat.", nameof(view));
        }

        var ownBlobbityIds = IdsOf(view.UnclaimedBlobbities);
        Place? firstAccepted = null;
        for (var quarterTurns = 0; quarterTurns < 4; quarterTurns++)
        {
            foreach (var legal in _legalPlacements(quarterTurns))
            {
                if (legal.Kind != PlacementKind.Beside)
                {
                    continue;
                }

                var place = new Place(legal.TileX, legal.TileY, quarterTurns);
                var preview = _preview(place);
                if (!preview.IsAccepted)
                {
                    continue;
                }

                if (ClaimsOwnBlobbity(preview.ClaimedBlobbityIds, ownBlobbityIds))
                {
                    return place;
                }

                firstAccepted ??= place;
            }
        }

        if (firstAccepted is not null)
        {
            return firstAccepted;
        }

        var pending = view.PendingMatchTile;
        var message = pending is null
            ? "No accepted placement was found."
            : "No accepted placement was found for tile '" + pending.Id.Value + "'.";
        throw new InvalidOperationException(message);
    }

    private static bool ClaimsOwnBlobbity(IReadOnlyList<BlobbityId> claimed, HashSet<BlobbityId> ownBlobbityIds)
    {
        for (var i = 0; i < claimed.Count; i++)
        {
            if (ownBlobbityIds.Contains(claimed[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<BlobbityId> IdsOf(IReadOnlyList<BlobbityCard> blobbities)
    {
        var ids = new HashSet<BlobbityId>();
        for (var i = 0; i < blobbities.Count; i++)
        {
            ids.Add(blobbities[i].Id);
        }

        return ids;
    }
}
