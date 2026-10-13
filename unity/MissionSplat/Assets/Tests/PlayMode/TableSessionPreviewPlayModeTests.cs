namespace MissionSplat.Player.Tests
{

using System.Linq;
using MissionSplat.App;
using MissionSplat.Rules;
using NUnit.Framework;
using UnityEngine;

// A tap on a legal spot previews the drawn tile there; only Place submits it.
public class TableSessionPreviewPlayModeTests : TableSessionFixture
{
    [Test]
    public void TappingALegalSpot_PreviewsTheTile_ShowsPlaceAndCancel_AndLeavesTheGameAlone()
    {
        var table = BeginHumanTable(RotateSetup());
        Assert.That(LiveButton("Place"), Is.Null, "no strip until a spot is tapped");
        Assert.That(LiveButton("Cancel"), Is.Null);
        var before = table.Snapshot;

        LiveButtons("Highlight").First().onClick.Invoke();

        Assert.That(LiveRect("Preview"), Is.Not.Null);
        Assert.That(LiveButton("Place"), Is.Not.Null);
        Assert.That(LiveButton("Cancel"), Is.Not.Null);
        Assert.That(((RectTransform)LiveButton("Place").transform).rect.height, Is.GreaterThanOrEqualTo(120f));
        Assert.That(((RectTransform)LiveButton("Cancel").transform).rect.height, Is.GreaterThanOrEqualTo(120f));
        Assert.That(ButtonFace("Place"), Is.EqualTo(SplatPalette.Go));
        Assert.That(ButtonFace("Cancel"), Is.EqualTo(SplatPalette.Paper));
        var after = table.Snapshot;
        Assert.That(after.View.Board.Tiles.Count, Is.EqualTo(before.View.Board.Tiles.Count));
        Assert.That(CellsOf(after.View), Is.EqualTo(CellsOf(before.View)));
        Assert.That(after.View.CurrentSeat, Is.EqualTo(before.View.CurrentSeat));
        Assert.That(after.View.PendingMatchTile.Id, Is.EqualTo(before.View.PendingMatchTile.Id));
        Assert.That(after.Recap, Is.Empty);
    }

    [Test]
    public void Place_CommitsThePreviewedSpot_AndTheStripGoes()
    {
        var table = BeginHumanTable(RotateSetup());
        var spot = table.Snapshot.LegalPlacements.First();

        LiveButton("Highlight" + spot.TileX + "_" + spot.TileY).onClick.Invoke();
        LiveButton("Place").onClick.Invoke();

        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(2));
        Assert.That(table.Snapshot.View.Board.Tiles.Any(tile => tile.TileX == spot.TileX && tile.TileY == spot.TileY), Is.True);
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("2"));
        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("Place"), Is.Null);
    }

    [Test]
    public void Cancel_ClearsThePreview_WithNoChangeToTheGame()
    {
        var table = BeginHumanTable(RotateSetup());
        var before = table.Snapshot;
        LiveButtons("Highlight").First().onClick.Invoke();

        LiveButton("Cancel").onClick.Invoke();

        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("Place"), Is.Null);
        Assert.That(LiveButton("Cancel"), Is.Null);
        Assert.That(LiveButtons("Highlight").Length, Is.EqualTo(before.LegalPlacements.Count), "every spot is tappable again");
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(before.View.Board.Tiles.Count));
        Assert.That(table.Snapshot.View.CurrentSeat, Is.EqualTo(before.View.CurrentSeat));
        Assert.That(table.Snapshot.Recap, Is.Empty);
    }

    [Test]
    public void TappingAnotherSpot_MovesThePreview()
    {
        var table = BeginHumanTable(RotateSetup());
        var spots = table.Snapshot.LegalPlacements;
        Assume.That(spots.Count, Is.GreaterThan(1));

        LiveButton("Highlight" + spots[0].TileX + "_" + spots[0].TileY).onClick.Invoke();
        var first = LiveRect("Preview").anchoredPosition;
        LiveButton("Highlight" + spots[1].TileX + "_" + spots[1].TileY).onClick.Invoke();

        Assert.That(_root.GetComponentsInChildren<RectTransform>(false).Count(rect => rect.name == "Preview"), Is.EqualTo(1));
        Assert.That(LiveRect("Preview").anchoredPosition, Is.Not.EqualTo(first));
        Assert.That(LiveButton("Highlight" + spots[0].TileX + "_" + spots[0].TileY), Is.Not.Null, "the spot left behind is tappable again");
        Assert.That(LiveButton("Highlight" + spots[1].TileX + "_" + spots[1].TileY), Is.Null, "the previewed spot shows the tile instead");
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
    }

    [Test]
    public void SpinningWhilePreviewing_TurnsThePreviewedTile()
    {
        var table = BeginHumanTable(RotateSetup());
        // Orientation never changes where a tile may go (Ruleset.PlacementRefusal), so any spot stays legal through a Spin and the preview stays.
        var spot = table.Snapshot.LegalPlacements.First();
        LiveButton("Highlight" + spot.TileX + "_" + spot.TileY).onClick.Invoke();
        var before = PreviewCellPosition("C00");

        LiveButton("Spin").onClick.Invoke();

        Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(1));
        Assert.That(LiveRect("Preview"), Is.Not.Null, "the spot is still legal, so the preview stays");
        Assert.That(PreviewCellPosition("C00"), Is.Not.EqualTo(before), "the previewed tile's cells turned");
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
    }

    [Test]
    public void SelectingAPower_ClearsThePreview()
    {
        var table = BeginHumanTable(RotateSetup());
        LiveButtons("Highlight").First().onClick.Invoke();

        LiveButton("PowerRotate").onClick.Invoke();

        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("Place"), Is.Null);

        LiveButton("PlaceInstead").onClick.Invoke();
        Assert.That(LiveRect("Preview"), Is.Null, "the preview does not come back");
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
    }

    [Test]
    public void ThePreviewDoesNotSurviveTheHandOff_OrANewGame()
    {
        var table = BeginHumanTable(SetupFrom(BlankTile("start"), BlankTile("one"), BlankTile("two"), BlankTile("three")));
        LiveButtons("Highlight").First().onClick.Invoke();
        LiveButton("Place").onClick.Invoke();
        SkipTheWholeRecap();
        Assert.That(LiveRect("Preview"), Is.Null, "the cover is up");
        table.ConfirmIncomingSeat();
        SkipTheWholeRecap();
        Assert.That(LiveRect("Preview"), Is.Null, "the next seat starts with none");

        LiveButtons("Highlight").First().onClick.Invoke();
        Assert.That(LiveRect("Preview"), Is.Not.Null);
        table.PlayAgain();
        table.ConfirmIncomingSeat();
        SkipTheWholeRecap();

        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("Place"), Is.Null);
    }

    [Test]
    public void AStackTarget_PreviewsOnTopOfTheTile_AndPlaceStacksIt()
    {
        var table = BeginHumanTable(StackSetup());
        var covered = table.Snapshot.View.Board.Tiles.Single();

        LiveButton("Highlight0_0").onClick.Invoke();

        var preview = LiveRect("Preview");
        Assert.That(preview, Is.Not.Null);
        var tile = _root.GetComponentsInChildren<RectTransform>(false).First(rect => rect.name == "Tile" + covered.Id.Value);
        Assert.That(preview.parent, Is.SameAs(tile.parent));
        Assert.That(preview.TransformPoint(preview.rect.center).y, Is.GreaterThan(tile.TransformPoint(tile.rect.center).y), "lifted above the tile it covers");
        Assert.That(preview.GetSiblingIndex(), Is.GreaterThan(tile.GetSiblingIndex()), "drawn over it");
        Assert.That(LiveButton("Place"), Is.Not.Null);
        Assert.That(table.Snapshot.View.Board.Tiles.Single().Id.Value, Is.EqualTo(covered.Id.Value), "nothing stacked yet");

        LiveButton("Place").onClick.Invoke();

        Assert.That(table.Snapshot.View.Board.Tiles.Single().Id.Value, Is.EqualTo("lid"));
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("2"));
    }

    private Vector2 PreviewCellPosition(string cell) =>
        _root.GetComponentsInChildren<RectTransform>(false)
            .First(rect => rect.name == cell && IsUnder(rect, "Preview"))
            .anchoredPosition;
}
}
