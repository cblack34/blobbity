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
            var hidden = HiddenMissionIds(session, seat);

            Assert.That(hidden, Is.Not.Empty);
            Assert.That(view.Seat, Is.EqualTo(seat));
            Assert.That(view.CurrentSeat, Is.EqualTo(seat));

            var chosen = ai.ChooseAction(view);

            Assert.That(recorder.Queries, Is.Not.Empty);
            Assert.That(recorder.Queries, Does.Contain("legal").And.Contain("preview"));
            Assert.That(recorder.PreviewedBeforeListed, Is.Empty);
            Assert.That(recorder.Previews.Select(preview => preview.Action), Does.Contain(chosen));

            // Everything the AI was handed: its view plus each answer the two queries returned.
            var reachable = MissionIds.In(view);
            reachable.UnionWith(MissionIds.In(recorder.Answers));
            Assert.That(reachable.Intersect(hidden), Is.Empty);
            Assert.That(reachable, Is.SupersetOf(view.UnclaimedMissions.Select(mission => mission.Id.Value)));

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

    // The seats' unclaimed mission ids that the given seat must never see.
    private static HashSet<string> HiddenMissionIds(LocalSession session, SeatId seat)
    {
        var hidden = new HashSet<string>();
        foreach (var other in new[] { "a", "b", "c" }.Where(name => name != seat.Value))
        {
            foreach (var mission in session.View(new SeatId(other)).UnclaimedMissions)
            {
                hidden.Add(mission.Id.Value);
            }
        }

        foreach (var mission in session.View(seat).UnclaimedMissions)
        {
            hidden.Remove(mission.Id.Value);
        }

        return hidden;
    }

    // A real session behind the only two doors an AI seat is given, noting every call and answer.
    private sealed class Recorder
    {
        private readonly LocalSession _session;
        private readonly SeatId _seat;
        private readonly HashSet<(int X, int Y, int QuarterTurns)> _listed = [];

        public Recorder(LocalSession session, SeatId seat)
        {
            _session = session;
            _seat = seat;
        }

        public List<string> Queries { get; } = [];

        public List<(GameAction Action, ActionPreview Result)> Previews { get; } = [];

        public List<object> Answers { get; } = [];

        public List<GameAction> PreviewedBeforeListed { get; } = [];

        public IReadOnlyList<LegalPlacement> LegalPlacements(int quarterTurns)
        {
            Queries.Add("legal");
            var answer = _session.LegalPlacements(_seat, quarterTurns);
            Answers.Add(answer);
            foreach (var legal in answer)
            {
                _listed.Add((legal.TileX, legal.TileY, quarterTurns));
            }

            return answer;
        }

        public ActionPreview Preview(GameAction action)
        {
            Queries.Add("preview");
            if (action is not Place place || !_listed.Contains((place.TileX, place.TileY, place.QuarterTurnsClockwise)))
            {
                PreviewedBeforeListed.Add(action);
            }

            var answer = _session.Preview(_seat, action);
            Previews.Add((action, answer));
            Answers.Add(answer);
            return answer;
        }
    }
}
