namespace MissionSplat.Player.Tests
{

using System.Linq;
using MissionSplat.App;
using MissionSplat.Player;
using MissionSplat.Rules;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class TableSessionPlayModeTests
{
    private GameObject _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            Object.DestroyImmediate(_root);
        }
    }

    [Test]
    public void ScriptedHumanPlacement_ThenOneAiTurn_PlacesThroughLocalSession()
    {
        Assert.That(OrdinaryCatalog.ClaimsRequiredToWin, Is.EqualTo(4));

        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        table.Begin(TableStart.HumanThenAi());

        Assert.That(table.IsStarted, Is.True);
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("1"));
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
        Assert.That(table.Snapshot.View.Board.Tiles[0].Id.Value, Is.EqualTo("census-01"));
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        Assert.That(table.Snapshot.SecretsVisible, Is.False);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToConfirm));
        Assert.That(table.Snapshot.LegalPlacements, Is.Empty);

        table.ConfirmIncomingSeat();

        var placements = table.Snapshot.LegalPlacements;
        Assert.That(table.Snapshot.ConcealVisible, Is.False);
        Assert.That(table.Snapshot.SecretsVisible, Is.True);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToAct));
        Assert.That(table.Snapshot.View.UnclaimedMissions.Count, Is.EqualTo(2));
        Assert.That(table.Snapshot.View.PendingMatchTile?.Id.Value, Is.EqualTo("census-02"));
        Assert.That(placements, Is.Not.Empty);
        Assert.That(placements.Any(slot => slot.TileX == 1 && slot.TileY == 1), Is.False);
        Assert.That(placements.Any(slot => slot.TileX == 1 && slot.TileY == 0), Is.True);

        table.Tap(1, 0);

        var view = table.Snapshot.View;
        Assert.That(view.Board.Tiles.Count, Is.EqualTo(3));
        Assert.That(view.CurrentSeat.Value, Is.EqualTo("1"));
        Assert.That(view.HasEnded, Is.False);
        // No line of four can finish beside census row 1: its top row starts with a blank and its bottom row mixes two colours.
        Assert.That(view.Claims.Single(row => row.Seat.Value == "1").Missions.Count, Is.EqualTo(0));
        Assert.That(view.UnclaimedMissions.Count, Is.EqualTo(2));
        // The same human holds the device after an AI turn, so the hand stays visible.
        Assert.That(table.Snapshot.ConcealVisible, Is.False);
        Assert.That(table.Snapshot.SecretsVisible, Is.True);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToAct));
    }

    [Test]
    public void StackTile_OffersAnOnTopPlacement_AndTappingItCoversWithoutAddingATile()
    {
        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        table.Begin(TableStart.PassAndPlayDraft(), StackSetup());
        table.ConfirmIncomingSeat();

        var before = table.Snapshot.View.Board;
        var onTop = table.Snapshot.LegalPlacements.Where(slot => slot.Kind == PlacementKind.OnTop).ToArray();
        Assert.That(onTop, Is.EqualTo(new[] { new LegalPlacement(0, 0, PlacementKind.OnTop) }));
        Assert.That(table.Snapshot.LegalPlacements.Any(slot => slot.Kind == PlacementKind.Beside), Is.True);
        Assert.That(before.Tiles.Count, Is.EqualTo(1));
        Assert.That(before.Cells.All(cell => cell.Value.Equals(Cell.Symbol(OrdinaryCatalog.Blank))), Is.True);

        // The card is inset, but a tap in the gap between cards must still land, so the highlight covers the tile's whole 2×2 block.
        var highlight = FindButton("Highlight0_0").GetComponent<RectTransform>();
        var block = _root.GetComponentsInChildren<RectTransform>(false).First(rect => rect.name == "Tile" + before.Tiles[0].Id.Value);
        Assert.That(highlight.parent, Is.SameAs(block.parent));
        Assert.That(highlight.anchoredPosition, Is.EqualTo(block.anchoredPosition));
        Assert.That(highlight.sizeDelta, Is.EqualTo(block.sizeDelta));

        FindButton("Highlight0_0").onClick.Invoke();

        var after = table.Snapshot.View.Board;
        Assert.That(after.Tiles.Count, Is.EqualTo(1));
        Assert.That(after.Tiles[0].Id.Value, Is.EqualTo("lid"));
        Assert.That(after.Cells.Any(cell => cell.Value.Equals(Cell.Color(OrdinaryCatalog.Red))), Is.True);
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("2"));
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
    }

    [Test]
    public void BouncingTheLastTile_StillRendersTheOriginPlacement()
    {
        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        table.Begin(TableStart.PassAndPlayDraft(), BounceSetup());
        table.ConfirmIncomingSeat();

        table.Submit(new UseBounce(0, 0));

        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(0));
        Assert.That(table.Snapshot.LegalPlacements, Is.EqualTo(new[] { new LegalPlacement(0, 0, PlacementKind.Beside) }));
        var origin = FindButton("Highlight0_0");
        Assert.That(origin, Is.Not.Null);
        Assert.That(origin.transform.parent.name, Is.EqualTo("Lattice"));

        origin.onClick.Invoke();

        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
    }

    [Test]
    public void InteractiveLayoutsSitInsideTheReportedSafeArea()
    {
        _root = new GameObject("Table");
        _root.AddComponent<TableSession>();

        var canvas = FindRect("Canvas");
        var safe = FindRect("SafeArea");
        var play = FindRect("Play");
        var setup = FindRect("Setup");
        var background = FindRect("Background");
        Assert.That(canvas, Is.Not.Null);
        Assert.That(safe, Is.Not.Null);
        Assert.That(play, Is.Not.Null);
        Assert.That(setup, Is.Not.Null);
        Assert.That(background, Is.Not.Null);
        Assert.That(play.transform.parent, Is.SameAs(safe.transform));
        Assert.That(setup.transform.parent, Is.SameAs(safe.transform));
        Assert.That(background.transform.parent, Is.SameAs(canvas.transform));

        var width = Screen.width;
        var height = Screen.height;
        var area = Screen.safeArea;
        if (width <= 0 || height <= 0 || area.width <= 0f || area.height <= 0f)
        {
            Assert.That(safe.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(safe.anchorMax, Is.EqualTo(Vector2.one));
            return;
        }

        var min = new Vector2(Mathf.Clamp01(area.xMin / width), Mathf.Clamp01(area.yMin / height));
        var max = new Vector2(Mathf.Clamp01(area.xMax / width), Mathf.Clamp01(area.yMax / height));
        if (max.x <= min.x || max.y <= min.y)
        {
            min = Vector2.zero;
            max = Vector2.one;
        }

        Assert.That(safe.anchorMin.x, Is.EqualTo(min.x).Within(0.0001f));
        Assert.That(safe.anchorMin.y, Is.EqualTo(min.y).Within(0.0001f));
        Assert.That(safe.anchorMax.x, Is.EqualTo(max.x).Within(0.0001f));
        Assert.That(safe.anchorMax.y, Is.EqualTo(max.y).Within(0.0001f));
        Assert.That(safe.offsetMin, Is.EqualTo(Vector2.zero));
        Assert.That(safe.offsetMax, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void TheBackgroundArtIsDecorationOnly_NothingInItTakesATap()
    {
        _root = new GameObject("Table");
        _root.AddComponent<TableSession>();

        var art = FindRect("Background").GetComponentsInChildren<Image>(true);
        Assert.That(art.Length, Is.GreaterThan(5), "the background paints splats and drips");
        Assert.That(art.Any(image => image.raycastTarget), Is.False);
    }

    [Test]
    public void StackAndTargetOutlines_AndLegalSpots_StillTakeTheTap()
    {
        var table = BeginHumanTable(RotateSetup());
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty);
        Assert.That(LiveButtons("Highlight").All(button => button.GetComponent<Image>().raycastTarget), Is.True, "a legal spot takes taps over its whole block");

        LiveButton("PowerRotate").onClick.Invoke();
        LiveButton("Spin").onClick.Invoke();

        var target = LiveButton("Target0_0");
        Assert.That(target.GetComponent<Image>().raycastTarget, Is.True, "a ring takes taps inside it too");
        Assert.That(target.transform.Find("Ring").GetComponent<Image>().color, Is.EqualTo(SplatPalette.TargetRing));
        Assert.That(table.Snapshot.SelectedPower, Is.EqualTo(OrdinaryCatalog.Rotate));
    }

    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void SeatEntriesAreSizedToContentAndSpacedEvenly_ForEverySeatCount(int seats)
    {
        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        table.Begin(new TableStart(seats, new bool[seats], 0, false));

        var entries = Enumerable.Range(1, seats).Select(seat => FindRect("Seat" + seat)).ToArray();
        var centres = entries.Select(entry => entry.anchorMin.x).ToArray();
        Assert.That(entries.All(entry => entry.anchorMin.x == entry.anchorMax.x), "an entry hugs its share's middle, not the share's width");
        for (var i = 0; i < seats; i++)
        {
            Assert.That(centres[i], Is.EqualTo((i + 0.5f) / seats).Within(0.0001f), "each entry is centred in an equal share of the bar");
        }

        Assert.That(entries.Select(entry => entry.sizeDelta.x).Distinct().Count(), Is.EqualTo(1));

        // Before the first layout the bar has no width, so the reference width stands in, as the bar itself does.
        var barWidth = FindRect("SeatBar").rect.width > 1f ? FindRect("SeatBar").rect.width : Ui.ReferenceWidthUnits;
        foreach (var entry in entries)
        {
            Assert.That(entry.sizeDelta.x / 2f, Is.LessThanOrEqualTo(barWidth / seats / 2f), "an entry stays inside its share of the bar");
        }
    }

    [Test]
    public void StatusStopTextWrapsInsideItsRect()
    {
        _root = new GameObject("Table");
        _root.AddComponent<TableSession>();

        var status = FindText("Status");
        var confirm = FindRect("Confirm");
        Assert.That(status, Is.Not.Null);
        Assert.That(confirm, Is.Not.Null);
        Assert.That(status.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
        Assert.That(confirm.GetComponentInChildren<Text>(true).horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Overflow));

        status.transform.parent.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        Assert.That(status.rectTransform.rect.height, Is.GreaterThan(status.fontSize));
    }

    [Test]
    public void RuntimeInputModule_BindsPointClickAndSubmit()
    {
        _root = new GameObject("Table");
        _root.AddComponent<TableSession>();

        var module = _root.GetComponentInChildren<InputSystemUIInputModule>(true);
        Assert.That(module, Is.Not.Null);
        Assert.That(module.point?.action, Is.Not.Null);
        Assert.That(module.leftClick?.action, Is.Not.Null);
        Assert.That(module.submit?.action, Is.Not.Null);
        Assert.That(module.point.action.bindings.Any(binding => binding.path.Contains("Touchscreen")), Is.True);
        Assert.That(module.leftClick.action.bindings.Any(binding => binding.path.Contains("Touchscreen")), Is.True);
    }

    [Test]
    public void AHumanTurnWithAMixedPowerTile_ShowsTheRulesMessage_AndOffersNothingToTap()
    {
        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        table.Begin(TableStart.PassAndPlayDraft(), MixedSetup());
        table.ConfirmIncomingSeat();

        var snapshot = table.Snapshot;
        Assert.That(snapshot.Status.Kind, Is.EqualTo(TableStatusKind.Stopped));
        Assert.That(snapshot.Status.Message, Does.Contain("open ruling"));
        Assert.That(FindText("Status").text, Is.EqualTo(snapshot.Status.Message));
        var lattice = FindRect("Lattice");
        Assert.That(lattice, Is.Not.Null);
        Assert.That(lattice.GetComponentsInChildren<Button>(true).Any(button => button.name.StartsWith("Highlight")), Is.False);
        Assert.That(FindButton("Spin"), Is.Not.Null);
        Assert.That(FindButton("Spin").interactable, Is.False);
    }

    [Test]
    public void SelectingRotate_TurnsTheTappedTile_AndKeepsTheDrawnTileToPlace()
    {
        var table = BeginHumanTable(RotateSetup());
        var before = CellsOf(table.Snapshot.View);
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty);

        LiveButton("PowerRotate").onClick.Invoke();

        Assert.That(table.Snapshot.SelectedPower, Is.EqualTo(OrdinaryCatalog.Rotate));
        Assert.That(LiveButtons("Highlight"), Is.Empty);
        Assert.That(LiveButton("Target0_0"), Is.Not.Null);
        Assert.That(LiveButton("Target0_0").interactable, Is.False, "a rotate target waits for a 1–3 amount");
        Assert.That(LiveButton("Spin").interactable, Is.True);
        Assert.That(FindText("Status").text, Does.Contain("rotate"));

        LiveButton("Spin").onClick.Invoke();
        LiveButton("Spin").onClick.Invoke();
        Assert.That(table.Snapshot.QuarterTurns, Is.EqualTo(2));
        Assert.That(LiveButton("Target0_0").interactable, Is.True);
        LiveButton("Target0_0").onClick.Invoke();

        var snapshot = table.Snapshot;
        Assert.That(CellsOf(snapshot.View), Is.Not.EqualTo(before));
        Assert.That(snapshot.View.PendingMatchTile, Is.Not.Null);
        Assert.That(snapshot.View.CurrentSeat.Value, Is.EqualTo("1"));
        Assert.That(snapshot.SelectedPower, Is.Null);
        Assert.That(LiveButton("PowerRotate"), Is.Null, "a power with no use left has no chip");
        Assert.That(LiveButtons("Target"), Is.Empty);
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty);
    }

    [Test]
    public void SelectingBounce_RemovesTheTappedTile_ThenAHighlightPlacesTheDrawnTile()
    {
        var table = BeginHumanTable(BounceBoardSetup());
        LiveButton("Highlight1_0").onClick.Invoke();
        table.ConfirmIncomingSeat();
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(2));
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("2"));

        LiveButton("PowerBounce").onClick.Invoke();

        Assert.That(table.Snapshot.SelectedPower, Is.EqualTo(OrdinaryCatalog.Bounce));
        Assert.That(LiveButton("Spin").interactable, Is.True);
        Assert.That(FindText("Status").text, Does.Contain("bounce"));

        // The lifter's bounce cell is local (0, 0); a quarter-turn moves it up the pending tile's lattice, so bounce still previews the turn.
        Assert.That(PendingCellPosition("C00"), Is.EqualTo(Vector2.zero));
        LiveButton("Spin").onClick.Invoke();
        Assert.That(PendingCellPosition("C00").x, Is.EqualTo(0f));
        Assert.That(PendingCellPosition("C00").y, Is.GreaterThan(0f));
        for (var i = 0; i < 3; i++)
        {
            LiveButton("Spin").onClick.Invoke();
        }

        Assert.That(PendingCellPosition("C00"), Is.EqualTo(Vector2.zero));

        LiveButton("Target1_0").onClick.Invoke();

        var snapshot = table.Snapshot;
        Assert.That(snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
        Assert.That(snapshot.View.PendingMatchTile, Is.Not.Null);
        Assert.That(snapshot.View.CurrentSeat.Value, Is.EqualTo("2"));
        Assert.That(snapshot.SelectedPower, Is.Null);
        Assert.That(LiveButton("PowerBounce"), Is.Null);

        LiveButtons("Highlight").First().onClick.Invoke();

        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(2));
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("1"));
    }

    [Test]
    public void PlaceInstead_AfterSelectingAPower_ReturnsThePlacementHighlights()
    {
        var table = BeginHumanTable(RotateSetup());
        Assert.That(LiveButton("PlaceInstead"), Is.Null);

        LiveButton("PowerRotate").onClick.Invoke();
        Assert.That(LiveButtons("Highlight"), Is.Empty);

        LiveButton("PlaceInstead").onClick.Invoke();

        Assert.That(table.Snapshot.SelectedPower, Is.Null);
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty);
        Assert.That(LiveButtons("Target"), Is.Empty);
        Assert.That(LiveButton("PlaceInstead"), Is.Null);
        Assert.That(LiveButton("PowerRotate"), Is.Not.Null);
        Assert.That(LiveButton("PowerRotate").transform.Find("Count"), Is.Null, "one use left shows no count");
    }

    [Test]
    public void TappingTheSelectedPowerAgain_Deselects()
    {
        var table = BeginHumanTable(RotateSetup());

        LiveButton("PowerRotate").onClick.Invoke();
        LiveButton("PowerRotate").onClick.Invoke();

        Assert.That(table.Snapshot.SelectedPower, Is.Null);
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty);
    }

    [Test]
    public void ADrawDeckThatRunsOut_ShowsTheReshuffleNotice_AndOffersAPlacementOnTheNewBoard()
    {
        var table = BeginHumanTable(SetupFrom(BlankTile("start"), BlankTile("one"), BlankTile("two")));
        Assert.That(FindText("Notice").gameObject.activeInHierarchy, Is.False);
        LiveButtons("Highlight").First().onClick.Invoke();
        table.ConfirmIncomingSeat();
        Assert.That(table.Snapshot.Notice, Is.EqualTo(TableNotice.None));

        LiveButtons("Highlight").First().onClick.Invoke();

        Assert.That(table.Snapshot.Notice, Is.EqualTo(TableNotice.DeckReshuffled));
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
        var notice = FindText("Notice");
        Assert.That(notice.gameObject.activeInHierarchy, Is.True);
        Assert.That(notice.text, Is.EqualTo("The draw deck ran out; the board was reshuffled."));
        table.ConfirmIncomingSeat();
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty);
    }

    [Test]
    public void SecretAndClaimedMissions_RenderAsPortraitCardsWithFourSplats()
    {
        var purple = Cell.Color(OrdinaryCatalog.Purple);
        var table = BeginHumanTable(SetupFrom(
            new[] { "a1", "a2", "b1", "b2", "r1", "r2", "r3", "r4" },
            new Tile(new TileId("start"), purple, purple, purple, purple),
            new Tile(new TileId("next"), purple, purple, purple, purple),
            BlankTile("pad")));

        // Secrets: the seat's two unclaimed missions, drawn in the turn tray.
        AssertMissionCards("Mission", 2);
        AssertPips("Seat1", filled: 0, empty: 4);

        // Placing the second purple tile beside the first finishes a purple row, so the seat claims both of its purple-row missions.
        table.Tap(1, 0);
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(2));
        Assert.That(table.Snapshot.View.Claims.Single(row => row.Seat.Value == "1").Missions.Count, Is.EqualTo(2));

        // The score shows as pips; the claimed missions themselves open on a tap, drawn with the same painter.
        AssertPips("Seat1", filled: 2, empty: 2);
        AssertPips("Seat2", filled: 0, empty: 4);
        Assert.That(FindRect("ClaimsPanel").gameObject.activeSelf, Is.False);
        LiveButton("Seat1").onClick.Invoke();
        AssertMissionCards("Claim", 2);
    }

    [Test]
    public void SpinTurnsTheDrawnTileOneQuarterAtATime_AndWrapsFromThreeToZero()
    {
        var table = BeginHumanTable(RotateSetup());
        var before = PendingCellPosition("C00");

        var seen = new System.Collections.Generic.List<int>();
        for (var i = 0; i < 4; i++)
        {
            LiveButton("Spin").onClick.Invoke();
            seen.Add(table.Snapshot.QuarterTurns);
            Assert.That(LiveButton("Spin").GetComponentInChildren<Text>().text, Is.EqualTo("Spin"));
        }

        Assert.That(seen, Is.EqualTo(new[] { 1, 2, 3, 0 }));
        Assert.That(PendingCellPosition("C00"), Is.EqualTo(before));
        Assert.That(LiveButtons("Turn"), Is.Empty);
    }

    [Test]
    public void TappingASeat_OpensItsClaims_AndTappingAnywhereClosesThem_EvenDuringTheHandOff()
    {
        var purple = Cell.Color(OrdinaryCatalog.Purple);
        var table = BeginHumanTable(SetupFrom(
            new[] { "a1", "a2", "b1", "b2", "r1", "r2", "r3", "r4" },
            new Tile(new TileId("start"), purple, purple, purple, purple),
            new Tile(new TileId("next"), purple, purple, purple, purple),
            BlankTile("pad")));
        table.Tap(1, 0);
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        var panel = FindRect("ClaimsPanel");
        Assert.That(panel.gameObject.activeSelf, Is.False);

        LiveButton("Seat1").onClick.Invoke();

        Assert.That(panel.gameObject.activeSelf, Is.True);
        AssertMissionCards("Claim", 2);
        Assert.That(FindText("Title", panel).text, Does.Contain("Seat 1"));
        Assert.That(table.Snapshot.ConcealVisible, Is.True, "reading claims does not confirm the hand-off");

        LiveButton("Dim").onClick.Invoke();
        Assert.That(panel.gameObject.activeSelf, Is.False);

        LiveButton("Seat2").onClick.Invoke();
        Assert.That(panel.gameObject.activeSelf, Is.True);
        Assert.That(FindText("None", panel).text, Is.EqualTo("No claims yet."));
        LiveButton("Dim").onClick.Invoke();
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
    }

    [Test]
    public void TheHandOffCover_SitsOverTheSecretMissionsOnly()
    {
        var table = BeginHumanTable(SetupFrom(BlankTile("start"), BlankTile("one"), BlankTile("two"), BlankTile("three")));
        LiveButtons("Highlight").First().onClick.Invoke();
        Assert.That(table.Snapshot.ConcealVisible, Is.True);

        var conceal = FindRect("Conceal");
        var secrets = FindRect("Secrets");
        Assert.That(conceal.gameObject.activeSelf, Is.True);
        Assert.That(conceal.parent, Is.SameAs(secrets.parent));
        Assert.That(conceal.anchorMin, Is.EqualTo(secrets.anchorMin));
        Assert.That(conceal.anchorMax, Is.EqualTo(secrets.anchorMax));
        Assert.That(LiveButton("Spin"), Is.Not.Null);
        Assert.That(LiveButton("Confirm"), Is.Not.Null);
        Assert.That(_root.GetComponentsInChildren<RectTransform>(false).Any(rect => rect.name.StartsWith("Mission") && rect.name.Length > "Mission".Length && char.IsDigit(rect.name["Mission".Length])), Is.False, "no mission card is drawn under the cover");
    }

    [Test]
    public void WhenTheGameEnds_TheTrayNamesTheWinner()
    {
        var purple = Cell.Color(OrdinaryCatalog.Purple);
        var seats = TableStart.PassAndPlayDraft().SeatsInTurnOrder();
        var setup = new GameSetup(
            seats,
            seats[0],
            OrdinaryCatalog.Colors,
            OrdinaryCatalog.NonScoringSymbols,
            OrdinaryCatalog.Patterns,
            1,
            new[] { "a1", "a2", "b1", "b2", "r1", "r2" }.Select(id => new Mission(new MissionId(id), MissionPattern.Row, OrdinaryCatalog.Purple)).ToArray(),
            new[] { new Tile(new TileId("start"), purple, purple, purple, purple), new Tile(new TileId("next"), purple, purple, purple, purple), BlankTile("pad") });
        var table = BeginHumanTable(setup);

        table.Tap(1, 0);

        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.Won), table.Snapshot.Status.Message);
        Assert.That(FindText("Heading").text, Is.EqualTo("Seat 1 wins!"));
        Assert.That(FindRect("Winner").gameObject.activeInHierarchy, Is.True);
        Assert.That(FindRect("Turn").gameObject.activeInHierarchy, Is.False);
        Assert.That(LiveButton("Spin"), Is.Null);
        Assert.That(_root.GetComponentsInChildren<Text>(false).Any(text => text.text == "Pending tile" || text.text == "Missions hidden"), Is.False);
    }

    [Test]
    public void ARenderClosesTheClaimsPanel()
    {
        var table = BeginHumanTable(SetupFrom(BlankTile("start"), BlankTile("one"), BlankTile("two"), BlankTile("three")));
        LiveButton("Seat1").onClick.Invoke();
        Assert.That(FindRect("ClaimsPanel").gameObject.activeSelf, Is.True);

        table.SelectPower(null);

        Assert.That(FindRect("ClaimsPanel").gameObject.activeSelf, Is.False);
    }

    [Test]
    public void PowerChips_ShowOnlyPowersTheDrawnTileStillHas_AsIconsWithACountAboveOne()
    {
        var rotate = Cell.Symbol(OrdinaryCatalog.Rotate);
        var table = BeginHumanTable(SetupFrom(
            BlankTile("start"),
            new Tile(new TileId("double"), rotate, rotate, Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red)),
            BlankTile("pad")));

        Assert.That(LiveButton("PowerRotate"), Is.Not.Null);
        Assert.That(LiveButton("PowerBounce"), Is.Null, "the drawn tile has no bounce");
        Assert.That(LiveButton("PowerRotate").GetComponentInChildren<Text>().text, Is.Empty, "a chip is an icon, not words");
        Assert.That(LiveButton("PowerRotate").transform.Find("Count").GetComponentInChildren<Text>().text, Does.Contain("2"));

        LiveButton("PowerRotate").onClick.Invoke();
        Assert.That(table.Snapshot.SelectedPower, Is.EqualTo(OrdinaryCatalog.Rotate));
        Assert.That(_root.GetComponentsInChildren<RectTransform>(false).Any(rect => rect.name == "TurnHint"), Is.False, "no turns chosen yet");
        LiveButton("Spin").onClick.Invoke();
        LiveButton("Spin").onClick.Invoke();
        var hint = _root.GetComponentsInChildren<RectTransform>(false).Single(rect => rect.name == "TurnHint");
        Assert.That(hint.childCount, Is.EqualTo(2), "one arrow per quarter-turn");
    }

    [Test]
    public void ABoardThatFitsStaysCentred_AndOneThatDoesNotScrollsAndKeepsItsPlaceAcrossRenders()
    {
        var tiles = new[] { BlankTile("start") }.Concat(Enumerable.Range(0, 14).Select(i => BlankTile("t" + i))).ToArray();
        var table = BeginHumanTable(SetupFrom(tiles));
        var scroll = _root.GetComponentInChildren<ScrollRect>(true);
        Assert.That(scroll, Is.Not.Null);
        Assert.That(scroll.content.anchoredPosition, Is.EqualTo(Vector2.zero), "a board that fits is centred");
        var view = scroll.GetComponent<RectTransform>();

        for (var i = 0; i < 9; i++)
        {
            PlaceRightmost(table);
        }

        Canvas.ForceUpdateCanvases();
        Assert.That(scroll.content.rect.width, Is.GreaterThan(view.rect.width), "ten tiles in a row need scrolling");
        Assert.That(scroll.content.Find("Lattice").GetComponentsInChildren<Button>(false).Count(button => button.name.StartsWith("Highlight")), Is.GreaterThan(0));

        // Scroll to the left end, then place another tile at the right end: the first tile stays where the player had it.
        scroll.content.anchoredPosition = new Vector2((scroll.content.rect.width - view.rect.width) / 2f, 0f);
        Canvas.ForceUpdateCanvases();
        var first = TileCentreInView("Tilestart", view);
        PlaceRightmost(table);
        Canvas.ForceUpdateCanvases();
        Assert.That(TileCentreInView("Tilestart", view).x, Is.EqualTo(first.x).Within(1f));
        Assert.That(TileCentreInView("Tilestart", view).y, Is.EqualTo(first.y).Within(1f));
    }

    [Test]
    public void ANewGameStartsWithItsBoardCentred_NotWhereTheLastOneWasScrolled()
    {
        var table = BeginLongRowTable();
        var scroll = _root.GetComponentInChildren<ScrollRect>(true);
        var slack = (scroll.content.rect.width - scroll.viewport.rect.width) / 2f;
        scroll.content.anchoredPosition = new Vector2(-slack, 0f);
        table.SelectPower(null);
        Assert.That(scroll.content.anchoredPosition.x, Is.EqualTo(-slack).Within(1f), "the scrolled place is kept within a game");

        _root.GetComponent<TableView>().ShowSetup(TableStart.PassAndPlayDraft());
        table.Begin(TableStart.PassAndPlayDraft(), SetupFrom(new[] { BlankTile("start") }.Concat(Enumerable.Range(0, 14).Select(i => BlankTile("t" + i))).ToArray()));
        table.ConfirmIncomingSeat();
        for (var i = 0; i < 9; i++)
        {
            PlaceRightmost(table);
        }

        scroll = _root.GetComponentInChildren<ScrollRect>(true);
        Assert.That(scroll.content.rect.width, Is.GreaterThan(scroll.viewport.rect.width), "the new board overflows too");
        Assert.That(scroll.content.anchoredPosition.x, Is.EqualTo(0f).Within(1f), "and starts centred");
    }

    [Test]
    public void AtTheRightEndOfABoard_TheHandOffRenderKeepsThePlace_InsteadOfClampingIt()
    {
        var table = BeginLongRowTable();
        var scroll = _root.GetComponentInChildren<ScrollRect>(true);
        var view = scroll.GetComponent<RectTransform>();
        var rightmost = "Tile" + table.Snapshot.View.Board.Tiles.OrderByDescending(tile => tile.TileX).First().Id.Value;
        scroll.content.anchoredPosition = new Vector2(-(scroll.content.rect.width - view.rect.width) / 2f, 0f);
        Canvas.ForceUpdateCanvases();
        var before = TileCentreInView(rightmost, view);

        // Placing at the left hands the device over: that render has no placement highlights, so the board is a tile narrower at the right.
        if (table.Snapshot.ConcealVisible)
        {
            table.ConfirmIncomingSeat();
        }

        var left = table.Snapshot.LegalPlacements.OrderBy(slot => slot.TileX).First();
        table.Tap(left.TileX, left.TileY);
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        Canvas.ForceUpdateCanvases();
        table.ConfirmIncomingSeat();
        Canvas.ForceUpdateCanvases();

        Assert.That(TileCentreInView(rightmost, view).x, Is.EqualTo(before.x).Within(1f));
    }

    [Test]
    public void APowerChipWithNothingToActOnIsGreyedOutAndNotTappable()
    {
        var bounce = Cell.Symbol(OrdinaryCatalog.Bounce);
        var blank = Cell.Symbol(OrdinaryCatalog.Blank);
        var table = BeginHumanTable(SetupWith(new Tile(new TileId("twice"), bounce, bounce, blank, blank)));
        Assert.That(LiveButton("PowerBounce").interactable, Is.True);
        Assert.That(ChipFace("PowerBounce"), Is.EqualTo(SplatPalette.BounceBadge));
        Assert.That(ChipGrayedOut("PowerBounce"), Is.False);

        // Bouncing the only tile empties the board; the second bounce cell then has no tile to act on.
        table.SelectPower(OrdinaryCatalog.Bounce);
        table.UseAt(0, 0);

        Assert.That(table.Snapshot.View.Board.Tiles, Is.Empty);
        Assert.That(LiveButton("PowerBounce").interactable, Is.True, "still selected, so it can be put back");
        LiveButton("PowerBounce").onClick.Invoke();
        Assert.That(table.Snapshot.SelectedPower, Is.Null);
        Assert.That(LiveButton("PowerBounce"), Is.Not.Null, "one use is still left");
        Assert.That(LiveButton("PowerBounce").interactable, Is.False);
        Assert.That(ChipGrayedOut("PowerBounce"), Is.True);
    }

    // A chip's face colour, and whether it shows as grayed out: its power's badge colour while it can be used, gray when it cannot.
    private Color ChipFace(string chip) => Ui.ToyFace(LiveButton(chip)).GetComponent<Image>().color;

    private bool ChipGrayedOut(string chip)
    {
        var cover = Ui.ToyFace(LiveButton(chip)).Find("Disabled");
        return cover != null && cover.gameObject.activeSelf;
    }

    private TableSession BeginLongRowTable()
    {
        var tiles = new[] { BlankTile("start") }.Concat(Enumerable.Range(0, 14).Select(i => BlankTile("t" + i))).ToArray();
        var table = BeginHumanTable(SetupFrom(tiles));
        for (var i = 0; i < 9; i++)
        {
            PlaceRightmost(table);
        }

        Canvas.ForceUpdateCanvases();
        return table;
    }

    // Driven through the real input module with a virtual mouse: a drag on a highlight scrolls the board and does not place, a tap places.
    [UnityTest]
    public IEnumerator DraggingAHighlightScrollsTheBoard_AndATapStillPlaces()
    {
        var tiles = new[] { BlankTile("start") }.Concat(Enumerable.Range(0, 14).Select(i => BlankTile("t" + i))).ToArray();
        var table = BeginHumanTable(SetupFrom(tiles));
        for (var i = 0; i < 9; i++)
        {
            PlaceRightmost(table);
        }

        RenderTexture target = null;
        Camera camera = null;
        InputSettings original = null;
        InputSettings temporary = null;
        Mouse mouse = null;
        try
        {
            // A camera gives the canvas a screen of known size, so the pointer can land on it whatever the editor's game view is.
            target = new RenderTexture(1080, 2340, 24);
            camera = new GameObject("Camera").AddComponent<Camera>();
            camera.targetTexture = target;
            camera.orthographic = true;
            var canvas = _root.GetComponentInChildren<Canvas>(true);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvas.GetComponent<CanvasScaler>().scaleFactor = 1f;

            // The test player is not the focused application, so input is told to count anyway; the project's settings come back in finally.
            original = InputSystem.settings;
            temporary = ScriptableObject.CreateInstance<InputSettings>();
            temporary.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            temporary.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = temporary;
            mouse = InputSystem.AddDevice<Mouse>();

            // Render once to lay the table out on the new canvas, let a frame pass so layout settles, then render again to draw on the settled sizes.
            table.SelectPower(null);
            yield return null;
            Canvas.ForceUpdateCanvases();
            table.SelectPower(null);
            yield return null;
            var scroll = _root.GetComponentInChildren<ScrollRect>(true);
            var before = table.Snapshot.View.Board.Tiles.Count;
            var view = scroll.viewport;
            var placement = LiveButtons("Highlight").First(button => Mathf.Abs(view.InverseTransformPoint(button.transform.position).x) < (view.rect.width / 2f) - 100f);
            var start = (Vector2)camera.WorldToScreenPoint(placement.transform.position);
            Assert.That(scroll.content.rect.width, Is.GreaterThan(scroll.viewport.rect.width));
            var contentBefore = scroll.content.anchoredPosition;

            yield return Hold(mouse, start);
            for (var step = 1; step <= 8; step++)
            {
                yield return Hold(mouse, start + new Vector2(-30f * step, 0f));
            }

            yield return Release(mouse, start + new Vector2(-240f, 0f));

            Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(before), "a drag does not place");
            Assert.That(scroll.content.anchoredPosition, Is.Not.EqualTo(contentBefore), "a drag scrolls the board");

            placement = LiveButtons("Highlight").First(button => Mathf.Abs(view.InverseTransformPoint(button.transform.position).x) < (view.rect.width / 2f) - 100f);
            var spot = (Vector2)camera.WorldToScreenPoint(placement.transform.position);
            yield return Hold(mouse, spot);
            yield return Release(mouse, spot);

            Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(before + 1), "a tap places");
        }
        finally
        {
            if (mouse != null)
            {
                InputSystem.RemoveDevice(mouse);
            }

            if (original != null)
            {
                InputSystem.settings = original;
            }

            if (temporary != null)
            {
                Object.DestroyImmediate(temporary);
            }

            if (camera != null)
            {
                Object.DestroyImmediate(camera.gameObject);
            }

            if (target != null)
            {
                target.Release();
            }
        }
    }

    // The mouse at position with the left button down; held through each call it presses, and moved it drags.
    private static IEnumerator Hold(Mouse mouse, Vector2 position)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
        yield return null;
        yield return null;
    }

    private static IEnumerator Release(Mouse mouse, Vector2 position)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
        yield return null;
        yield return null;
    }

    private void PlaceRightmost(TableSession table)
    {
        if (table.Snapshot.ConcealVisible)
        {
            table.ConfirmIncomingSeat();
        }

        var spot = table.Snapshot.LegalPlacements.OrderByDescending(slot => slot.TileX).First();
        table.Tap(spot.TileX, spot.TileY);
        if (table.Snapshot.ConcealVisible)
        {
            table.ConfirmIncomingSeat();
        }
    }

    // Where a board tile's centre sits relative to the middle of the board view, in canvas units.
    private Vector2 TileCentreInView(string tileName, RectTransform view)
    {
        var tile = _root.GetComponentsInChildren<RectTransform>(false).First(rect => rect.name == tileName);
        var local = view.InverseTransformPoint(tile.TransformPoint(tile.rect.center));
        return new Vector2(local.x, local.y);
    }

    private void AssertPips(string seatButton, int filled, int empty)
    {
        var pips = LiveButton(seatButton).GetComponentsInChildren<RectTransform>(false);
        Assert.That(pips.Count(rect => rect.name == "PipFilled"), Is.EqualTo(filled), seatButton + " filled pips");
        Assert.That(pips.Count(rect => rect.name == "PipEmpty"), Is.EqualTo(empty), seatButton + " empty pips");
    }

    // Every live slot named prefix + index holds one 5:7 fit-in-parent Card whose pattern is four splats.
    private void AssertMissionCards(string prefix, int expected)
    {
        var slots = _root.GetComponentsInChildren<RectTransform>(false)
            .Where(rect => rect.name.StartsWith(prefix) && rect.name.Length > prefix.Length && char.IsDigit(rect.name[prefix.Length]))
            .ToArray();
        Assert.That(slots.Length, Is.EqualTo(expected), prefix + " slots");
        foreach (var slot in slots)
        {
            var card = slot.Find("Card");
            Assert.That(card, Is.Not.Null, slot.name + " has a Card");
            var fit = card.GetComponent<AspectRatioFitter>();
            Assert.That(fit, Is.Not.Null, slot.name + " card has a fitter");
            Assert.That(fit.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent));
            Assert.That(fit.aspectRatio, Is.EqualTo(5f / 7f).Within(0.01f));
            var splats = card.GetComponentsInChildren<Image>(false).Where(image => image.name == "Splat").ToArray();
            Assert.That(splats.Length, Is.EqualTo(4), slot.name + " splats");
        }
    }

    private TableSession BeginHumanTable(GameSetup setup)
    {
        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        table.Begin(TableStart.PassAndPlayDraft(), setup);
        table.ConfirmIncomingSeat();
        return table;
    }

    // An asymmetric start tile under a drawn tile with one rotate cell, so a turn shows in the cells.
    private static GameSetup RotateSetup() => SetupFrom(
        new Tile(new TileId("start"), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Blue), Cell.Color(OrdinaryCatalog.Green), Cell.Color(OrdinaryCatalog.Purple)),
        new Tile(new TileId("spinner"), Cell.Symbol(OrdinaryCatalog.Rotate), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red)),
        BlankTile("pad"));

    // The first seat places a plain tile beside the start; the second seat then draws the tile with a bounce cell.
    private static GameSetup BounceBoardSetup() => SetupFrom(
        BlankTile("start"),
        new Tile(new TileId("side"), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red)),
        new Tile(new TileId("lifter"), Cell.Symbol(OrdinaryCatalog.Bounce), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red)),
        BlankTile("pad"));

    private static Tile BlankTile(string id)
    {
        var blank = Cell.Symbol(OrdinaryCatalog.Blank);
        return new Tile(new TileId(id), blank, blank, blank, blank);
    }

    private static string CellsOf(SeatView view) =>
        string.Join(";", view.Board.Cells.Select(cell => cell.CellX + "," + cell.CellY + ":" + cell.Value));

    private static GameSetup StackSetup() => SetupWith(
        new Tile(new TileId("lid"), Cell.Symbol(OrdinaryCatalog.Stack), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Blue), Cell.Symbol(OrdinaryCatalog.Blank)));

    private static GameSetup BounceSetup() => SetupWith(
        new Tile(new TileId("lid"), Cell.Symbol(OrdinaryCatalog.Bounce), Cell.Symbol(OrdinaryCatalog.Blank), Cell.Symbol(OrdinaryCatalog.Blank), Cell.Symbol(OrdinaryCatalog.Blank)));

    private static GameSetup MixedSetup() => SetupWith(
        new Tile(new TileId("mixed"), Cell.Symbol(OrdinaryCatalog.Rotate), Cell.Symbol(OrdinaryCatalog.Bounce), Cell.Symbol(OrdinaryCatalog.Blank), Cell.Symbol(OrdinaryCatalog.Blank)));

    private static GameSetup SetupWith(Tile drawn) => SetupFrom(BlankTile("start"), drawn, BlankTile("pad"));

    private static GameSetup SetupFrom(params Tile[] tiles) =>
        SetupFrom(new[] { "a1", "a2", "b1", "b2", "spare" }, tiles);

    // A seat that claims draws a replacement, so a test that claims needs missions beyond the opening hands.
    private static GameSetup SetupFrom(string[] missionIds, params Tile[] tiles)
    {
        var missions = missionIds
            .Select(id => new Mission(new MissionId(id), MissionPattern.Row, OrdinaryCatalog.Purple))
            .ToArray();
        var seats = TableStart.PassAndPlayDraft().SeatsInTurnOrder();
        return new GameSetup(
            seats,
            seats[0],
            OrdinaryCatalog.Colors,
            OrdinaryCatalog.NonScoringSymbols,
            OrdinaryCatalog.Patterns,
            OrdinaryCatalog.ClaimsRequiredToWin,
            missions,
            tiles);
    }

    // The live (not yet destroyed) cell of the pending tile's lattice.
    private Vector2 PendingCellPosition(string cell) =>
        _root.GetComponentsInChildren<RectTransform>(false)
            .First(rect => rect.name == cell && IsUnder(rect, "Pending"))
            .anchoredPosition;

    private static bool IsUnder(Transform node, string ancestor)
    {
        for (var parent = node.parent; parent != null; parent = parent.parent)
        {
            if (parent.name == ancestor)
            {
                return true;
            }
        }

        return false;
    }

    private RectTransform FindRect(string name)
    {
        foreach (var rect in _root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == name)
            {
                return rect;
            }
        }

        return null;
    }

    // A re-render leaves the replaced buttons inactive until the frame ends, so only live ones count.
    private Button LiveButton(string name) =>
        _root.GetComponentsInChildren<Button>(false).FirstOrDefault(button => button.name == name);

    private Button[] LiveButtons(string prefix) =>
        _root.GetComponentsInChildren<Button>(false).Where(button => button.name.StartsWith(prefix)).ToArray();

    private Button FindButton(string name)
    {
        foreach (var button in _root.GetComponentsInChildren<Button>(true))
        {
            if (button.name == name)
            {
                return button;
            }
        }

        return null;
    }

    private static Text FindText(string name, Component within)
    {
        return within.GetComponentsInChildren<Text>(true).First(text => text.name == name);
    }

    private Text FindText(string name)
    {
        foreach (var text in _root.GetComponentsInChildren<Text>(true))
        {
            if (text.name == name)
            {
                return text;
            }
        }

        return null;
    }
}
}
