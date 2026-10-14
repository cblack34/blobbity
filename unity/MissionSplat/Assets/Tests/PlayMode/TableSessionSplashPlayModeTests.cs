namespace MissionSplat.Player.Tests
{

using System.Collections.Generic;
using System.Linq;
using MissionSplat.App;
using MissionSplat.Player;
using MissionSplat.Rules;
using NUnit.Framework;
using UnityEngine;

// The power splash: when a drawn tile's powers are announced, what it waits for, what it blocks, and when it plays again. Splashes play for real here.
public class TableSessionSplashPlayModeTests : TableSessionFixture
{
    protected override bool RecapsPlayInstantly => false;

    [Test]
    public void ADrawnPowerTile_SplashesOnceItsHolderCanAct_NotBehindTheHandOffCover()
    {
        var table = NewTable();
        table.Begin(TableStart.PassAndPlayDraft(), RotateSetup());
        var recap = RecapOf();
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        Assert.That(recap.IsPlaying, Is.False, "the device is not yet in the player's hands");

        table.ConfirmIncomingSeat();

        Assert.That(recap.IsPlaying, Is.True);
        Assert.That(recap.Count, Is.EqualTo(1));
        Assert.That(recap.Caption, Is.EqualTo("You got a rotate!"));
        Assert.That(FindText("Heading").text, Is.EqualTo("You got a rotate!"));
        Assert.That(LiveRect("SplashBadge"), Is.Not.Null);
        Assert.That(table.Snapshot.Recap, Is.Empty, "a splash is not a recap");

        LiveButton("RecapSkip").onClick.Invoke();

        Assert.That(recap.IsPlaying, Is.False);
        Assert.That(LiveRect("SplashBadge"), Is.Null);
        Assert.That(LiveRect("RecapOverlay"), Is.Null, "the overlay closed");
        Assert.That(FindText("Heading").text, Is.EqualTo("Seat 1’s turn!"), "the heading goes back to the turn");
    }

    [Test]
    public void APlainTile_DoesNotSplash()
    {
        BeginHumanTable(SetupFrom(BlankTile("start"), BlankTile("one"), BlankTile("two")));

        Assert.That(RecapOf().IsPlaying, Is.False);
        Assert.That(LiveRect("SplashBadge"), Is.Null);
    }

    [Test]
    public void TwoCellsOfOnePower_SplashOnce()
    {
        BeginHumanTable(SetupWith(new Tile(new TileId("twin"), Cell.Symbol(OrdinaryCatalog.Rotate), Cell.Symbol(OrdinaryCatalog.Rotate), Cell.Symbol(OrdinaryCatalog.Blank), Cell.Symbol(OrdinaryCatalog.Blank))));

        Assert.That(RecapOf().Count, Is.EqualTo(1), "one splash per kind");
        LiveButton("RecapSkip").onClick.Invoke();
        Assert.That(RecapOf().IsPlaying, Is.False);
    }

    // The rules stop on a tile showing two different powers, so no game reaches this through play; the queue is fed by hand.
    [Test]
    public void TwoPowers_SplashInTheOrderGiven_AndATapSkipsOnlyTheOneOnScreen()
    {
        BeginHumanTable(SetupFrom(BlankTile("start"), BlankTile("one"), BlankTile("two")));
        var recap = RecapOf();
        var corners = (new Vector3(100f, 100f), new Vector3(180f, 180f));

        recap.PlaySplashes(new[] { new PowerSpot(OrdinaryCatalog.Rotate, corners.Item1, corners.Item2), new PowerSpot(OrdinaryCatalog.Bounce, corners.Item1, corners.Item2) });

        Assert.That(recap.Count, Is.EqualTo(2));
        Assert.That(recap.Caption, Is.EqualTo("You got a rotate!"));
        LiveButton("RecapSkip").onClick.Invoke();
        Assert.That(recap.IsPlaying, Is.True, "a tap skips only the splash on screen");
        Assert.That(recap.Index, Is.EqualTo(1));
        Assert.That(FindText("Heading").text, Is.EqualTo("You got a bounce!"));
        LiveButton("RecapSkip").onClick.Invoke();
        Assert.That(recap.IsPlaying, Is.False);
    }

    [Test]
    public void ATileTheRulesStopOn_DoesNotSplash()
    {
        var table = BeginHumanTable(MixedSetup());

        Assert.That(table.Snapshot.CanAct, Is.False);
        Assert.That(RecapOf().IsPlaying, Is.False);
    }

    [Test]
    public void ASplashLeftAlone_GrowsOutToTheMiddleSpinningAndEndsBackInItsCell_InAboutASecond()
    {
        BeginHumanTable(RotateSetup());
        var recap = RecapOf();
        var badge = LiveRect("SplashBadge");
        var start = badge.anchoredPosition;
        var startSize = badge.sizeDelta.x;
        Assert.That(startSize, Is.GreaterThan(1f));
        Assert.That(start, Is.Not.EqualTo(Vector2.zero), "it starts at the cell on the tray, not the middle");

        recap.Tick(0.25f);
        Assert.That(badge.localScale.x, Is.GreaterThan(1.2f));
        Assert.That(badge.localRotation, Is.Not.EqualTo(Quaternion.identity), "it spins");

        recap.Tick(0.25f);
        Assert.That(badge.anchoredPosition.magnitude, Is.LessThan(2f), "at the middle of the table");
        Assert.That(badge.localScale.x, Is.EqualTo(3.5f).Within(0.01f));

        recap.Tick(0.49f);
        Assert.That(recap.IsPlaying, Is.True);
        Assert.That(Vector2.Distance(badge.anchoredPosition, start), Is.LessThan(startSize * 0.1f), "back into its cell");
        Assert.That(badge.localScale.x, Is.EqualTo(1f).Within(0.05f));

        recap.Tick(0.02f);
        Assert.That(recap.IsPlaying, Is.False);
    }

    [Test]
    public void TheSplashWaitsForTheRecap_ThenPlaysBeforeTheHolderCanAct()
    {
        var table = BeginHumanTable(BounceBoardSetup());
        var recap = RecapOf();
        PlaceRightmost(table, false);
        Assert.That(recap.IsPlaying, Is.True, "the outgoing player's recap");
        LiveButton("RecapSkip").onClick.Invoke();
        Assert.That(recap.IsPlaying, Is.False);
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        Assert.That(LiveRect("SplashBadge"), Is.Null, "nothing behind the hand-off cover");

        table.ConfirmIncomingSeat();

        Assert.That(recap.IsPlaying, Is.True);
        Assert.That(recap.Caption, Is.EqualTo("Seat 1 places a tile"), "the recap first");
        Assert.That(LiveRect("SplashBadge"), Is.Null);
        Assert.That(table.Snapshot.Recap, Is.Not.Empty);

        LiveButton("RecapSkip").onClick.Invoke();

        Assert.That(table.Snapshot.Recap, Is.Empty, "the recap was acknowledged once it ended");
        Assert.That(recap.IsPlaying, Is.True);
        Assert.That(recap.Caption, Is.EqualTo("You got a bounce!"));
        Assert.That(LiveRect("SplashBadge"), Is.Not.Null);
        Assert.That(FindText("Notice").text, Does.StartWith("Seat 1 placed a tile"), "the recap's line stays");
        Assert.That(table.Snapshot.CanAct, Is.True);

        LiveButton("RecapSkip").onClick.Invoke();
        Assert.That(recap.IsPlaying, Is.False);
        Assert.That(table.Snapshot.Recap, Is.Empty);
    }

    [Test]
    public void WhileASplashPlays_TheTableTakesNoInput()
    {
        var table = BeginHumanTable(RotateSetup());
        var before = CellsOf(table.Snapshot.View);
        var spot = table.Snapshot.LegalPlacements.First();

        table.SetQuarterTurns(1);
        table.Place(spot.TileX, spot.TileY);
        table.ConfirmIncomingSeat();

        Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(0));
        Assert.That(CellsOf(table.Snapshot.View), Is.EqualTo(before));
        Assert.That(RecapOf().IsPlaying, Is.True);
        var skip = LiveRect("RecapOverlay");
        Assert.That(skip.GetSiblingIndex(), Is.EqualTo(skip.parent.childCount - 1), "the skip layer is over everything");
        Assert.That(LiveButton("RecapSkip"), Is.Not.Null);
    }

    [Test]
    public void ARenderOrASpin_DoesNotPlayTheSplashAgain_ButANewGameDoes()
    {
        var table = BeginHumanTable(RotateSetup());
        LiveButton("RecapSkip").onClick.Invoke();
        var view = _root.GetComponent<TableView>();

        table.SetQuarterTurns(1);
        view.RenderCurrentSnapshot();
        view.Render(table.Snapshot);
        LiveButton("Target0_0").onClick.Invoke();
        LiveButton("Cancel").onClick.Invoke();

        Assert.That(RecapOf().IsPlaying, Is.False, "announced once");

        table.Begin(TableStart.PassAndPlayDraft(), RotateSetup());
        table.ConfirmIncomingSeat();

        Assert.That(RecapOf().IsPlaying, Is.True, "the same tile in a new game");
    }

    [Test]
    public void AnAiSeatsPowerTile_NeverSplashes()
    {
        var table = NewTable();
        table.Begin(TableStart.HumanThenAi(), SetupFrom(BlankTile("start"), BlankTile("one"), new Tile(new TileId("ai"), Cell.Symbol(OrdinaryCatalog.Rotate), Cell.Symbol(OrdinaryCatalog.Blank), Cell.Symbol(OrdinaryCatalog.Blank), Cell.Symbol(OrdinaryCatalog.Blank)), BlankTile("two"), BlankTile("pad")));
        table.ConfirmIncomingSeat();
        var recap = RecapOf();
        var spot = table.Snapshot.LegalPlacements.First();

        table.Place(spot.TileX, spot.TileY);

        var captions = new List<string>();
        while (recap.IsPlaying)
        {
            captions.Add(recap.Caption);
            LiveButton("RecapSkip").onClick.Invoke();
        }

        Assert.That(captions.Count, Is.GreaterThanOrEqualTo(2), "the human's placement and the AI's turn");
        Assert.That(captions.Any(caption => caption.StartsWith("You got")), Is.False, string.Join(" | ", captions));
        Assert.That(LiveRect("SplashBadge"), Is.Null);
    }

    [Test]
    public void WhenPlayIsInstant_ASplashIsOverAtOnce_AndTheTableTakesInput()
    {
        var table = NewTable();
        RecapOf().PlaysInstantly = true;
        table.Begin(TableStart.PassAndPlayDraft(), RotateSetup());

        table.ConfirmIncomingSeat();

        Assert.That(RecapOf().IsPlaying, Is.False);
        Assert.That(LiveRect("SplashBadge"), Is.Null);
        table.SetQuarterTurns(1);
        Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(1));
    }

    private RecapPlayer RecapOf() => _root.GetComponent<TableView>().Recap;

    // The first seat places a tile at the rightmost legal spot; a recap and the hand-off cover follow.
    private void PlaceRightmost(TableSession table, bool skipRecap)
    {
        var spot = table.Snapshot.LegalPlacements.OrderByDescending(slot => slot.TileX).First();
        table.Place(spot.TileX, spot.TileY);
        if (skipRecap)
        {
            SkipTheWholeRecap();
        }
    }
}
}
