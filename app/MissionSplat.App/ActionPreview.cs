namespace MissionSplat.App;

using Blobbity.Rules;

public sealed class ActionPreview
{
    private ActionPreview(SessionRejection? rejection, IReadOnlyList<BlobbityId> claimedBlobbityIds)
    {
        Rejection = rejection;
        ClaimedBlobbityIds = claimedBlobbityIds;
    }

    public SessionRejection? Rejection { get; }

    public bool IsAccepted => Rejection is null;

    public IReadOnlyList<BlobbityId> ClaimedBlobbityIds { get; }

    public static ActionPreview Accept(IReadOnlyList<BlobbityId> claimedBlobbityIds) =>
        new(null, Copied.List(claimedBlobbityIds, nameof(claimedBlobbityIds)));

    public static ActionPreview Reject(SessionRejection rejection)
    {
        if (rejection is null)
        {
            throw new ArgumentNullException(nameof(rejection));
        }

        return new ActionPreview(rejection, []);
    }

    public static ActionPreview Reject(Rejection rejection) => Reject(SessionRejection.From(rejection));
}
