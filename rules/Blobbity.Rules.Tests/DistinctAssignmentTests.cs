namespace Blobbity.Rules.Tests;

// Choose keeps the largest count of entries that can each hold a distinct set, earliest entries first.
[TestFixture]
public sealed class DistinctAssignmentTests
{
    private static readonly CellSet A = Column(0);
    private static readonly CellSet B = Column(1);
    private static readonly CellSet C = Column(2);

    [Test]
    public void Choose_WhenAllThreeCannotFit_KeepsTheFirstTwo()
    {
        // Entry 2 {B} would leave entry 0 with nothing once entry 1 holds A, so only 0 and 1 are kept (0 takes B).
        var chosen = DistinctAssignment.Choose([[A, B], [A], [B]]);

        Assert.That(chosen, Is.EqualTo(new[] { 0, 1 }));
    }

    [Test]
    public void Choose_WhenEveryEntryWantsTheSameSet_KeepsOnlyTheFirst()
    {
        var chosen = DistinctAssignment.Choose([[A], [A], [A]]);

        Assert.That(chosen, Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public void Choose_SkipsAContendedMiddleEntry_AndStillKeepsALaterOne()
    {
        var chosen = DistinctAssignment.Choose([[A], [A], [B]]);

        Assert.That(chosen, Is.EqualTo(new[] { 0, 2 }));
    }

    [Test]
    public void Choose_WhenOnlyThreeSetsExist_KeepsTheEarliestThreeOfFour()
    {
        var chosen = DistinctAssignment.Choose([[A, B, C], [A], [B], [C]]);

        Assert.That(chosen, Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void Choose_SkipsAnEntryWithNoCandidates()
    {
        var chosen = DistinctAssignment.Choose([[], [A], []]);

        Assert.That(chosen, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void Choose_WithNoEntries_ChoosesNothing()
    {
        Assert.That(DistinctAssignment.Choose([]), Is.Empty);
    }

    private static CellSet Column(int x) =>
        CellSet.Of(new CellCoord(x, 0), new CellCoord(x, 1), new CellCoord(x, 2), new CellCoord(x, 3));
}
