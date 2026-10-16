namespace MissionSplat.App.Tests;

using MissionSplat.App;
using MissionSplat.Rules;

public class AiPlayerIsolationTests
{
    [Test]
    public void AiSeats_AreHandedOnlyTheirOwnViewAndTwoQueries_AndPlayAcceptedActions()
    {
        Assert.That(RepresentativeDeck.Label, Is.EqualTo("representative"));
        var session = ClaimingTable.Open(3).Session;

        foreach (var name in new[] { "a", "b", "c" })
        {
            var seat = new SeatId(name);
            var recorder = new Recorder(session, seat);
            var ai = new AiPlayer(seat, recorder.LegalPlacements, recorder.Preview);
            var view = session.View(seat);
            var hidden = HiddenBlobbityIds(session, seat);

            Assert.That(hidden, Is.Not.Empty);
            Assert.That(view.Seat, Is.EqualTo(seat));
            Assert.That(view.CurrentSeat, Is.EqualTo(seat));

            var chosen = ai.ChooseAction(view);

            Assert.That(recorder.Queries, Is.Not.Empty);
            Assert.That(recorder.Queries, Does.Contain("legal").And.Contain("preview"));
            Assert.That(recorder.PreviewedBeforeListed, Is.Empty);
            Assert.That(recorder.Previews.Select(preview => preview.Action), Does.Contain(chosen));

            // Everything the AI was handed: its view, each answer the two queries returned,
            // and the object behind both query delegates, which holds no session.
            var reachable = BlobbityIds.In(view);
            reachable.UnionWith(BlobbityIds.In(recorder.Answers));
            reachable.UnionWith(BlobbityIds.In(recorder));
            Assert.That(reachable.Intersect(hidden), Is.Empty);
            Assert.That(reachable, Is.SupersetOf(view.UnclaimedBlobbities.Select(blobbity => blobbity.Id.Value)));

            var result = session.Submit(seat, chosen);
            Assert.That(result.IsAccepted, Is.True, result.Rejection?.Message);
        }
    }

    [Test]
    public void ChooseAction_WithAnotherSeatsView_IsRefused()
    {
        var session = ClaimingTable.Open(3).Session;
        var ai = new AiPlayer(new SeatId("a"), _ => [], _ => ActionPreview.Accept([]));

        Assert.That(() => ai.ChooseAction(session.View(new SeatId("b"))), Throws.ArgumentException);
    }

    // The seats' unclaimed blobbity ids that the given seat must never see.
    private static HashSet<string> HiddenBlobbityIds(LocalSession session, SeatId seat)
    {
        var hidden = new HashSet<string>();
        foreach (var other in new[] { "a", "b", "c" }.Where(name => name != seat.Value))
        {
            foreach (var blobbity in session.View(new SeatId(other)).UnclaimedBlobbities)
            {
                hidden.Add(blobbity.Id.Value);
            }
        }

        foreach (var blobbity in session.View(seat).UnclaimedBlobbities)
        {
            hidden.Remove(blobbity.Id.Value);
        }

        return hidden;
    }

    // The only two doors an AI seat is given. Every answer is computed from the real session up front,
    // so the delegates' target keeps no reference to the session or to another seat's hand.
    private sealed class Recorder
    {
        private readonly IReadOnlyList<LegalPlacement>[] _legal = new IReadOnlyList<LegalPlacement>[4];
        private readonly Dictionary<GameAction, ActionPreview> _previews = [];
        private readonly HashSet<GameAction> _listed = [];

        public Recorder(LocalSession session, SeatId seat)
        {
            for (var quarterTurns = 0; quarterTurns < 4; quarterTurns++)
            {
                _legal[quarterTurns] = session.LegalPlacements(seat, quarterTurns);
                foreach (var legal in _legal[quarterTurns])
                {
                    var place = new Place(legal.TileX, legal.TileY, quarterTurns);
                    _previews[place] = session.Preview(seat, place);
                }
            }
        }

        public List<string> Queries { get; } = [];

        public List<(GameAction Action, ActionPreview Result)> Previews { get; } = [];

        public List<object> Answers { get; } = [];

        public List<GameAction> PreviewedBeforeListed { get; } = [];

        public IReadOnlyList<LegalPlacement> LegalPlacements(int quarterTurns)
        {
            Queries.Add("legal");
            var answer = _legal[quarterTurns];
            Answers.Add(answer);
            foreach (var legal in answer)
            {
                _listed.Add(new Place(legal.TileX, legal.TileY, quarterTurns));
            }

            return answer;
        }

        public ActionPreview Preview(GameAction action)
        {
            Queries.Add("preview");
            if (!_listed.Contains(action) || !_previews.TryGetValue(action, out var answer))
            {
                PreviewedBeforeListed.Add(action);
                throw new InvalidOperationException("The AI previewed an action before a legal-placement answer listed it.");
            }

            Previews.Add((action, answer));
            Answers.Add(answer);
            return answer;
        }
    }
}
