namespace Blobbity.Player.Tests
{

using System.Linq;
using Blobbity.App;
using Blobbity.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

// A drawn tile's rotate or bounce is used by tapping a board tile: select it, try it, then confirm or cancel. Only the strip's Confirm and Bounce submit.
public class TableSessionPowerTapsPlayModeTests : TableSessionFixture
{
    private static readonly SymbolId Rotate = OrdinaryCatalog.Rotate;

    [Test]
    public void TappingARotateTarget_SelectsTheTile_ShowsTheRotateStrip_AndLeavesTheGameAlone()
    {
        var table = BeginHumanTable(RotateSetup());
        Assert.That(LiveButton("Turn"), Is.Null, "no strip until a tile is tapped");
        var before = table.Snapshot;

        LiveButton("Target0_0").onClick.Invoke();

        Assert.That(LiveRect("Preview"), Is.Not.Null, "the selected tile lifts");
        Assert.That(LiveRect("Preview").Find("Ring").GetComponent<Image>().color, Is.EqualTo(SplatPalette.Go));
        Assert.That(LiveRect("Tilestart"), Is.Null, "the tile on the board gives way to the lifted one");
        Assert.That(LiveButton("Place"), Is.Null);
        foreach (var name in new[] { "Cancel", "Turn", "RotateConfirm" })
        {
            Assert.That(((RectTransform)LiveButton(name).transform).rect.height, Is.GreaterThanOrEqualTo(120f), name);
        }

        Assert.That(ButtonFace("Turn"), Is.EqualTo(SplatPalette.Sunny));
        Assert.That(ButtonFace("RotateConfirm"), Is.EqualTo(SplatPalette.Go));
        Assert.That(ButtonFace("Cancel"), Is.EqualTo(SplatPalette.Paper));
        Assert.That(LiveButton("RotateConfirm").interactable, Is.False, "no turns yet");
        Assert.That(FindText("Status").text, Is.EqualTo("Turn the tile, then confirm."));
        Assert.That(CellsOf(table.Snapshot.View), Is.EqualTo(CellsOf(before.View)));
        Assert.That(table.Snapshot.View.CurrentSeat, Is.EqualTo(before.View.CurrentSeat));
        Assert.That(table.Snapshot.View.PendingMatchTile.Id, Is.EqualTo(before.View.PendingMatchTile.Id));
        Assert.That(table.Snapshot.Recap, Is.Empty);
    }

    [Test]
    public void TheTurnButton_TurnsTheSelectedTileLive_WithoutTouchingTheGameOrTheDrawnTilesSpin()
    {
        var table = BeginHumanTable(RotateSetup());
        var before = CellsOf(table.Snapshot.View);
        LiveButton("Target0_0").onClick.Invoke();
        var unturned = PreviewColours();

        LiveButton("Turn").onClick.Invoke();

        Assert.That(PreviewColours(), Is.Not.EqualTo(unturned), "the selected tile's cells turned");
        Assert.That(LiveButton("RotateConfirm").interactable, Is.True);
        Assert.That(CellsOf(table.Snapshot.View), Is.EqualTo(before), "view only");
        Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(0), "Spin is the drawn tile's own turn");

        // Spin still turns the drawn tile for placement and leaves the rotate as it was.
        var turned = PreviewColours();
        LiveButton("Spin").onClick.Invoke();
        Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(1));
        Assert.That(LiveRect("Preview"), Is.Not.Null);
        Assert.That(PreviewColours(), Is.EqualTo(turned));
    }

    [Test]
    public void TheTurnButtonWraps_AndConfirmIsOnlyLiveForOneToThreeTurns()
    {
        var table = BeginHumanTable(RotateSetup());
        var before = CellsOf(table.Snapshot.View);
        LiveButton("Target0_0").onClick.Invoke();

        var enabled = new System.Collections.Generic.List<bool>();
        for (var press = 0; press < 4; press++)
        {
            LiveButton("Turn").onClick.Invoke();
            enabled.Add(LiveButton("RotateConfirm").interactable);
        }

        Assert.That(enabled, Is.EqualTo(new[] { true, true, true, false }), "the fourth press is back at zero");
        LiveButton("RotateConfirm").onClick.Invoke();
        Assert.That(CellsOf(table.Snapshot.View), Is.EqualTo(before), "a rotate by zero is not submitted");
        Assert.That(LiveRect("Preview"), Is.Not.Null);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void Confirm_AppliesTheTurnsShown_AndTheStripGoes(int turns)
    {
        var table = BeginHumanTable(RotateSetup());
        var before = CellsOf(table.Snapshot.View);
        LiveButton("Target0_0").onClick.Invoke();
        for (var press = 0; press < turns; press++)
        {
            LiveButton("Turn").onClick.Invoke();
        }

        var shown = PreviewColours();

        LiveButton("RotateConfirm").onClick.Invoke();

        var snapshot = table.Snapshot;
        Assert.That(CellsOf(snapshot.View), Is.Not.EqualTo(before));
        Assert.That(LiveRect("Tilestart"), Is.Not.Null);
        Assert.That(ColoursBySlot(LiveRect("Tilestart")), Is.EqualTo(shown), "the board shows what the preview showed");
        Assert.That(snapshot.View.PendingMatchTile.Id.Value, Is.EqualTo("spinner"), "the drawn tile is still to place");
        Assert.That(snapshot.View.CurrentSeat.Value, Is.EqualTo("1"));
        Assert.That(snapshot.View.RemainingUses.Single(charge => charge.Power.Equals(Rotate)).Remaining, Is.EqualTo(0));
        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("RotateConfirm"), Is.Null);
        Assert.That(LiveButtons("Target"), Is.Empty);
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty);
        Assert.That(FindText("Status").text, Is.EqualTo("Place your tile."));
    }

    [Test]
    public void AConfirmedRotate_LeavesTheDrawnTilesSpinWhereItWas()
    {
        var table = BeginHumanTable(RotateSetup());
        LiveButton("Spin").onClick.Invoke();
        LiveButton("Spin").onClick.Invoke();
        Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(2));
        LiveButton("Target0_0").onClick.Invoke();
        LiveButton("Turn").onClick.Invoke();

        LiveButton("RotateConfirm").onClick.Invoke();

        Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(2), "the rotate has its own turns; Spin is for the placement");
        Assert.That(table.Snapshot.View.PendingMatchTile.Id.Value, Is.EqualTo("spinner"));
    }

    [Test]
    public void Cancel_RestoresTheTile_WithNoChangeToTheGame()
    {
        var table = BeginHumanTable(RotateSetup());
        var before = CellsOf(table.Snapshot.View);
        var beforeColours = ColoursBySlot(LiveRect("Tilestart"));
        LiveButton("Target0_0").onClick.Invoke();
        LiveButton("Turn").onClick.Invoke();

        LiveButton("Cancel").onClick.Invoke();

        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("Turn"), Is.Null);
        Assert.That(ColoursBySlot(LiveRect("Tilestart")), Is.EqualTo(beforeColours));
        Assert.That(LiveButton("Target0_0"), Is.Not.Null, "tappable again");
        Assert.That(CellsOf(table.Snapshot.View), Is.EqualTo(before));
        Assert.That(table.Snapshot.Recap, Is.Empty);
        Assert.That(FindText("Status").text, Does.Contain("tap a ringed tile"));

        // Selecting it again starts from no turns.
        LiveButton("Target0_0").onClick.Invoke();
        Assert.That(LiveButton("RotateConfirm").interactable, Is.False);
    }

    [Test]
    public void TappingABounceTarget_ShowsTheBounceStrip_CancelDeselects_AndBounceRemovesTheTile()
    {
        var table = BeginHumanTable(BounceBoardSetup());
        PlaceAt(LiveButton("Highlight1_0"));
        table.ConfirmIncomingSeat();
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(2));
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("2"));

        LiveButton("Target1_0").onClick.Invoke();

        Assert.That(LiveRect("Preview"), Is.Not.Null);
        Assert.That(LiveButton("Turn"), Is.Null, "a bounce has no turns");
        Assert.That(((RectTransform)LiveButton("Bounce").transform).rect.height, Is.GreaterThanOrEqualTo(120f));
        Assert.That(ButtonFace("Bounce"), Is.EqualTo(SplatPalette.BounceBadge));
        Assert.That(FindText("Status").text, Is.EqualTo("Bounce this tile?"));
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(2), "nothing bounced yet");

        LiveButton("Cancel").onClick.Invoke();

        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("Bounce"), Is.Null);
        Assert.That(LiveButton("Target1_0"), Is.Not.Null);
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(2));

        LiveButton("Target1_0").onClick.Invoke();
        LiveButton("Bounce").onClick.Invoke();

        var snapshot = table.Snapshot;
        Assert.That(snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
        Assert.That(snapshot.View.PendingMatchTile, Is.Not.Null);
        Assert.That(snapshot.View.CurrentSeat.Value, Is.EqualTo("2"));
        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("Bounce"), Is.Null);

        PlaceAt(LiveButtons("Highlight").First());

        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(2));
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("1"));
    }

    [Test]
    public void ABadgeOnTheDrawnTileGreysOutAsItsUseIsSpent()
    {
        var rotate = Cell.Symbol(Rotate);
        var table = BeginHumanTable(SetupFrom(
            BlankTile("start"),
            new Tile(new TileId("double"), rotate, rotate, Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red)),
            BlankTile("pad")));
        Assert.That(PendingBadges(SplatPalette.RotateBadge), Is.EqualTo(2));
        Assert.That(PendingBadges(SplatPalette.Gray), Is.EqualTo(0));

        LiveButton("Target0_0").onClick.Invoke();
        LiveButton("Turn").onClick.Invoke();
        LiveButton("RotateConfirm").onClick.Invoke();

        Assert.That(PendingBadges(SplatPalette.RotateBadge), Is.EqualTo(1));
        Assert.That(PendingBadges(SplatPalette.Gray), Is.EqualTo(1), "one use spent, one badge gray");
        Assert.That(LiveButton("Target0_0"), Is.Not.Null, "one use is left");

        table.Submit(new UseRotate(0, 0, 1));

        Assert.That(PendingBadges(SplatPalette.RotateBadge), Is.EqualTo(0));
        Assert.That(PendingBadges(SplatPalette.Gray), Is.EqualTo(2));
        Assert.That(LiveButtons("Target"), Is.Empty, "no use left, nothing to tap");
    }

    [Test]
    public void ABounceWithNoTileLeftToAct_OffersNothingToTap_AndItsSecondBadgeStaysLit()
    {
        var bounce = Cell.Symbol(OrdinaryCatalog.Bounce);
        var blank = Cell.Symbol(OrdinaryCatalog.Blank);
        BeginHumanTable(SetupWith(new Tile(new TileId("twice"), bounce, bounce, blank, blank)));
        Assert.That(PendingBadges(SplatPalette.BounceBadge), Is.EqualTo(2));

        // Bouncing the only tile empties the board; the second bounce then has no tile to act on.
        LiveButton("Target0_0").onClick.Invoke();
        LiveButton("Bounce").onClick.Invoke();

        Assert.That(PendingBadges(SplatPalette.Gray), Is.EqualTo(1));
        Assert.That(PendingBadges(SplatPalette.BounceBadge), Is.EqualTo(1), "a use is still left, though nothing can take it");
        Assert.That(LiveButtons("Target"), Is.Empty);
        Assert.That(FindText("Status").text, Is.EqualTo("Place your tile."));
    }

    [Test]
    public void ATapOnAnOccupiedTileWithNoPowerToUse_DoesNothing()
    {
        var table = BeginHumanTable(SetupFrom(BlankTile("start"), BlankTile("one"), BlankTile("two")));

        Assert.That(LiveButton("Target0_0"), Is.Null);
        Assert.That(LiveButton("Highlight0_0"), Is.Null);
        Assert.That(TilePreview.Tapped(table.Snapshot, 0, 0), Is.Null);
        Assert.That(FindText("Status").text, Is.EqualTo("Place your tile."));
    }

    [Test]
    public void TappingAnotherTargetOrSpot_ReplacesThePreviewWithThatMode()
    {
        var table = BeginHumanTable(RotateSetup());
        var spot = table.Snapshot.LegalPlacements.First();

        LiveButton("Target0_0").onClick.Invoke();
        LiveButton("Turn").onClick.Invoke();
        LiveButton("Highlight" + spot.TileX + "_" + spot.TileY).onClick.Invoke();

        Assert.That(LiveButton("Place"), Is.Not.Null);
        Assert.That(LiveButton("Turn"), Is.Null);
        Assert.That(_root.GetComponentsInChildren<RectTransform>(false).Count(rect => rect.name == "Preview"), Is.EqualTo(1));
        Assert.That(LiveRect("Tilestart"), Is.Not.Null, "the tile put back");

        LiveButton("Target0_0").onClick.Invoke();

        Assert.That(LiveButton("Place"), Is.Null);
        Assert.That(LiveButton("RotateConfirm").interactable, Is.False, "the turns start again");
        Assert.That(LiveButton("Highlight" + spot.TileX + "_" + spot.TileY), Is.Not.Null, "the spot is tappable again");
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
    }

    [Test]
    public void ASelectedTile_DoesNotSurviveADifferentDrawnTile_OrItsUseBeingSpent()
    {
        var view = NewViewOnly();
        var other = StartedTable(SetupFrom(BlankTile("start"), BlankTile("other"), BlankTile("pad")));
        view.RenderNewGame(StartedTable(RotateSetup()).Snapshot);
        LiveButton("Target0_0").onClick.Invoke();
        Assert.That(LiveRect("Preview"), Is.Not.Null);

        view.Render(other.Snapshot);

        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("RotateConfirm"), Is.Null);

        var spent = StartedTable(RotateSetup());
        view.RenderNewGame(spent.Snapshot);
        LiveButton("Target0_0").onClick.Invoke();
        Assert.That(LiveRect("Preview"), Is.Not.Null);
        spent.Submit(new UseRotate(0, 0, 1));
        spent.AcknowledgeRecap();
        Assert.That(spent.Snapshot.View.PendingMatchTile.Id.Value, Is.EqualTo("spinner"));
        Assert.That(spent.Snapshot.Recap, Is.Empty);

        view.Render(spent.Snapshot);

        Assert.That(LiveRect("Preview"), Is.Null, "the drawn tile has no rotate left");
        Assert.That(LiveButton("RotateConfirm"), Is.Null);
    }

    // The next three render another table's snapshot into the view under a live bounce selection, so each render differs from the selected one
    // in just one way and only one reason to drop the selection applies.
    [Test]
    public void ASelectedBounce_DoesNotSurviveItsUseBeingSpent()
    {
        var view = NewViewOnly();
        var table = BounceTableAtSecondSeat(BounceTile("lid", 1));
        view.RenderNewGame(table.Snapshot);
        LiveButton("Target0_0").onClick.Invoke();
        Assert.That(LiveRect("Preview"), Is.Not.Null);

        table.Submit(new UseBounce(1, 0));
        table.AcknowledgeRecap();
        Assert.That(table.Snapshot.View.PendingMatchTile.Id.Value, Is.EqualTo("lid"), "the same drawn tile");
        Assert.That(table.Snapshot.Recap, Is.Empty);
        Assert.That(table.Snapshot.View.Board.Tiles.Any(tile => tile.TileX == 0 && tile.TileY == 0), Is.True, "the selected tile is still on the board");

        view.Render(table.Snapshot);

        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("Bounce"), Is.Null);
    }

    [Test]
    public void ASelectedBounce_DoesNotSurviveARecapPending_EvenWithTheTargetAndAUseLeft()
    {
        var view = NewViewOnly();
        var table = BounceTableAtSecondSeat(BounceTile("twice", 2));
        view.RenderNewGame(table.Snapshot);
        LiveButton("Target0_0").onClick.Invoke();
        Assert.That(LiveRect("Preview"), Is.Not.Null);

        table.Submit(new UseBounce(1, 0));
        Assert.That(table.Snapshot.Recap, Is.Not.Empty);
        Assert.That(table.Snapshot.LegalTargets.Single(entry => entry.Power.Equals(OrdinaryCatalog.Bounce)).Targets, Does.Contain(new BoardPosition(0, 0)));
        Assert.That(table.Snapshot.View.RemainingUses.Single(charge => charge.Power.Equals(OrdinaryCatalog.Bounce)).Remaining, Is.EqualTo(1));

        view.Render(table.Snapshot);

        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("Bounce"), Is.Null);
    }

    [Test]
    public void ASelectedBounce_DoesNotSurviveADifferentDrawnTile_EvenWithTheSameTarget()
    {
        var view = NewViewOnly();
        var other = BounceTableAtSecondSeat(BounceTile("other", 1));
        view.RenderNewGame(BounceTableAtSecondSeat(BounceTile("lid", 1)).Snapshot);
        LiveButton("Target0_0").onClick.Invoke();
        Assert.That(LiveRect("Preview"), Is.Not.Null);
        Assert.That(other.Snapshot.LegalTargets.Single(entry => entry.Power.Equals(OrdinaryCatalog.Bounce)).Targets, Does.Contain(new BoardPosition(0, 0)));
        Assert.That(other.Snapshot.Recap, Is.Empty);

        view.Render(other.Snapshot);

        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("Bounce"), Is.Null);
    }

    private static Tile BounceTile(string id, int bounceCells)
    {
        var bounce = Cell.Symbol(OrdinaryCatalog.Bounce);
        var blank = Cell.Symbol(OrdinaryCatalog.Blank);
        return new Tile(new TileId(id), bounce, bounceCells > 1 ? bounce : blank, blank, blank);
    }

    // Two tiles on the board and the second seat holding the bounce tile, with the first seat's placement already seen.
    private static Table BounceTableAtSecondSeat(Tile bounce)
    {
        var table = StartedTable(SetupFrom(BlankTile("start"), BlankTile("side"), bounce, BlankTile("pad")));
        table.Place(1, 0);
        table.AcknowledgeRecap();
        table.Confirm();
        table.AcknowledgeRecap();
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("2"));
        Assert.That(table.Snapshot.Recap, Is.Empty);
        return table;
    }

    // The colours of the live "Preview" tile's four splats, in board order (bottom row then top row, left to right).
    private string[] PreviewColours() => ColoursBySlot(LiveRect("Preview"));

    private static string[] ColoursBySlot(RectTransform tile) =>
        tile.GetComponentsInChildren<RectTransform>(false)
            .Where(rect => rect.parent != null && rect.parent.name == "Cells" && rect.name.StartsWith("C"))
            .OrderBy(cell => Mathf.Round(cell.anchoredPosition.y))
            .ThenBy(cell => Mathf.Round(cell.anchoredPosition.x))
            .Select(cell => ColorUtility.ToHtmlStringRGB(cell.GetComponentsInChildren<Image>(false).First(image => image.name == "Splat").color))
            .ToArray();

    // The badges drawn on the drawn tile in the tray, by face colour.
    private int PendingBadges(Color face) =>
        LiveRect("Pending").GetComponentsInChildren<Image>(false).Count(image => image.name == "Face" && image.color == face && image.transform.parent.name == "Rim");
}
}
