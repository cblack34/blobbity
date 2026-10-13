namespace MissionSplat.Player.Tests
{

using System.Linq;
using MissionSplat.App;
using MissionSplat.Rules;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
        Assert.That(spots.Count, Is.GreaterThan(1));

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

    // The editor's game view may be too short for the table, so the canvas gets a camera and a phone-sized screen, as in the drag test.
    [UnityTest]
    public IEnumerator APreviewInTheBottomRow_ScrollsClearOfTheStrip()
    {
        // A column down from the start tile that fits the view exactly, so the next tile down sits in the board's bottom row with no slack below.
        var tiles = new[] { BlankTile("start") }.Concat(Enumerable.Range(0, 14).Select(i => BlankTile("t" + i))).ToArray();
        var table = BeginHumanTable(SetupFrom(tiles));
        for (var i = 0; i < 6; i++)
        {
            PlaceLowest(table);
        }

        var target = new RenderTexture(1080, 2340, 24);
        var camera = new GameObject("Camera").AddComponent<Camera>();
        try
        {
            camera.targetTexture = target;
            camera.orthographic = true;
            var canvas = _root.GetComponentInChildren<Canvas>(true);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvas.GetComponent<CanvasScaler>().scaleFactor = 1f;
            table.SelectPower(null);
            yield return null;
            table.SelectPower(null);
            yield return null;

            LiveButton("Highlight0_-7").onClick.Invoke();

            var corners = new Vector3[4];
            LiveRect("Preview").GetWorldCorners(corners);
            var previewBottom = corners[0].y;
            LiveRect("PlacementStrip").GetWorldCorners(corners);
            var stripTop = corners[1].y;
            Assert.That(previewBottom, Is.GreaterThanOrEqualTo(stripTop), "the lifted tile ends above the strip");
        }
        finally
        {
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void ASafeAreaRender_KeepsAValidPreview()
    {
        BeginHumanTable(RotateSetup());
        LiveButtons("Highlight").First().onClick.Invoke();

        _root.GetComponent<TableView>().RenderCurrentSnapshot();

        Assert.That(LiveRect("Preview"), Is.Not.Null);
        Assert.That(LiveButton("Place"), Is.Not.Null);
    }

    // The next three change the game under a live preview, by rendering another table's snapshot into the view, so each render differs
    // from the previewed one in just one way and only one reason to drop the preview applies.
    [Test]
    public void ADifferentDrawnTile_DropsThePreview_EvenAtTheSameLegalSpot()
    {
        var view = NewViewOnly();
        var other = StartedTable(SetupFrom(BlankTile("start"), BlankTile("other"), BlankTile("pad")));
        view.RenderNewGame(StartedTable(RotateSetup()).Snapshot);
        LiveButton("Highlight1_0").onClick.Invoke();
        Assert.That(LiveRect("Preview"), Is.Not.Null);
        Assert.That(other.Snapshot.LegalPlacements.Any(spot => spot.TileX == 1 && spot.TileY == 0), Is.True);
        Assert.That(other.Snapshot.Recap, Is.Empty);

        view.Render(other.Snapshot);

        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("Place"), Is.Null);
    }

    [Test]
    public void APendingRecap_DropsThePreview_EvenForTheSameTileAndSpot()
    {
        var view = NewViewOnly();
        var rotated = StartedTable(RotateSetup());
        rotated.SelectPower(OrdinaryCatalog.Rotate);
        rotated.SetQuarterTurns(2);
        rotated.UseAt(0, 0);
        view.RenderNewGame(StartedTable(RotateSetup()).Snapshot);
        LiveButton("Highlight1_0").onClick.Invoke();
        Assert.That(LiveRect("Preview"), Is.Not.Null);
        Assert.That(rotated.Snapshot.Recap, Is.Not.Empty);
        Assert.That(rotated.Snapshot.View.PendingMatchTile.Id.Value, Is.EqualTo("spinner"));
        Assert.That(rotated.Snapshot.LegalPlacements.Any(spot => spot.TileX == 1 && spot.TileY == 0), Is.True);

        view.Render(rotated.Snapshot);

        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("Place"), Is.Null);
    }

    [Test]
    public void TheHandOffCover_DropsThePreview()
    {
        var view = NewViewOnly();
        var table = StartedTable(RotateSetup());
        view.RenderNewGame(table.Snapshot);
        LiveButton("Highlight1_0").onClick.Invoke();
        Assert.That(LiveRect("Preview"), Is.Not.Null);
        table.Place(1, 0);
        table.AcknowledgeRecap();
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        Assert.That(table.Snapshot.Recap, Is.Empty);

        view.Render(table.Snapshot);

        Assert.That(LiveRect("Preview"), Is.Null);
        Assert.That(LiveButton("Place"), Is.Null);
    }

    private Vector2 PreviewCellPosition(string cell) =>
        _root.GetComponentsInChildren<RectTransform>(false)
            .First(rect => rect.name == cell && IsUnder(rect, "Preview"))
            .anchoredPosition;
}
}
