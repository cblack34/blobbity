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

public class TableSessionPlayModeTests : TableSessionFixture
{


    [Test]
    public void ScriptedHumanPlacement_ThenOneAiTurn_PlacesThroughLocalSession()
    {
        Assert.That(OrdinaryCatalog.ClaimsRequiredToWin, Is.EqualTo(4));

        var table = NewTable();
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
        Assert.That(table.Snapshot.View.UnclaimedBlobbities.Count, Is.EqualTo(2));
        Assert.That(table.Snapshot.View.PendingMatchTile?.Id.Value, Is.EqualTo("census-02"));
        Assert.That(placements, Is.Not.Empty);
        Assert.That(placements.Any(slot => slot.TileX == 1 && slot.TileY == 1), Is.False);
        Assert.That(placements.Any(slot => slot.TileX == 1 && slot.TileY == 0), Is.True);

        table.Place(1, 0);

        var view = table.Snapshot.View;
        Assert.That(view.Board.Tiles.Count, Is.EqualTo(3));
        Assert.That(view.CurrentSeat.Value, Is.EqualTo("1"));
        Assert.That(view.HasEnded, Is.False);
        // No line of four can finish beside census row 1: its top row starts with a blank and its bottom row mixes two colours.
        Assert.That(view.Claims.Single(row => row.Seat.Value == "1").Blobbities.Count, Is.EqualTo(0));
        Assert.That(view.UnclaimedBlobbities.Count, Is.EqualTo(2));
        // The same human holds the device after an AI turn, so the hand stays visible.
        Assert.That(table.Snapshot.ConcealVisible, Is.False);
        Assert.That(table.Snapshot.SecretsVisible, Is.True);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.ToAct));
    }

    [Test]
    public void StackTile_OffersAnOnTopPlacement_AndPlacingItCoversWithoutAddingATile()
    {
        var table = NewTable();
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

        PlaceAt(FindButton("Highlight0_0"));

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
        var table = NewTable();
        table.Begin(TableStart.PassAndPlayDraft(), BounceSetup());
        table.ConfirmIncomingSeat();

        table.Submit(new UseBounce(0, 0));

        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(0));
        Assert.That(table.Snapshot.LegalPlacements, Is.EqualTo(new[] { new LegalPlacement(0, 0, PlacementKind.Beside) }));
        var origin = FindButton("Highlight0_0");
        Assert.That(origin, Is.Not.Null);
        Assert.That(origin.transform.parent.name, Is.EqualTo("Lattice"));

        PlaceAt(origin);

        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
    }

    [Test]
    public void InteractiveLayoutsSitInsideTheReportedSafeArea()
    {
        NewTable();

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
        NewTable();

        var art = FindRect("Background").GetComponentsInChildren<Image>(true);
        Assert.That(art.Length, Is.GreaterThan(5), "the background paints splats and drips");
        Assert.That(art.Any(image => image.raycastTarget), Is.False);
    }

    [Test]
    public void StackAndTargetOutlines_AndLegalSpots_StillTakeTheTap()
    {
        BeginHumanTable(RotateSetup());
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty);
        Assert.That(LiveButtons("Highlight").All(button => button.GetComponent<Image>().raycastTarget), Is.True, "a legal spot takes taps over its whole block");

        var target = LiveButton("Target0_0");
        Assert.That(target.GetComponent<Image>().raycastTarget, Is.True, "a ring takes taps inside it too");
        Assert.That(target.transform.Find("Ring").GetComponent<Image>().color, Is.EqualTo(SplatPalette.TargetRing));
    }

    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void SeatEntriesAreSizedToContentAndSpacedEvenly_ForEverySeatCount(int seats)
    {
        var table = NewTable();
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
        NewTable();

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
        NewTable();

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
        var table = NewTable();
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
    public void ADrawDeckThatRunsOut_ShowsTheReshuffleNotice_AndOffersAPlacementOnTheNewBoard()
    {
        var table = BeginHumanTable(SetupFrom(BlankTile("start"), BlankTile("one"), BlankTile("two")));
        Assert.That(FindText("Notice").gameObject.activeInHierarchy, Is.False);
        PlaceAt(LiveButtons("Highlight").First());
        table.ConfirmIncomingSeat();
        Assert.That(table.Snapshot.Notice, Is.EqualTo(TableNotice.None));

        PlaceAt(LiveButtons("Highlight").First());

        Assert.That(table.Snapshot.Notice, Is.EqualTo(TableNotice.DeckReshuffled));
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(1));
        var notice = FindText("Notice");
        Assert.That(notice.gameObject.activeInHierarchy, Is.True);
        Assert.That(notice.text, Is.EqualTo("The draw deck ran out; the board was reshuffled."));
        table.ConfirmIncomingSeat();
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty);
    }

    [Test]
    public void SecretAndClaimedBlobbities_RenderAsPortraitCardsWithFourSplats()
    {
        var purple = Cell.Color(OrdinaryCatalog.Purple);
        var table = BeginHumanTable(SetupFrom(
            new[] { "a1", "a2", "b1", "b2", "r1", "r2", "r3", "r4" },
            new Tile(new TileId("start"), purple, purple, purple, purple),
            new Tile(new TileId("next"), purple, purple, purple, purple),
            BlankTile("pad")));

        // Secrets: the seat's two unclaimed blobbities, drawn in the turn tray.
        AssertBlobbityCards("Blobbity", 2);
        AssertPips("Seat1", filled: 0, empty: 4);

        // Placing the second purple tile beside the first finishes a purple row, so the seat claims both of its purple-row blobbities.
        table.Place(1, 0);
        Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(2));
        Assert.That(table.Snapshot.View.Claims.Single(row => row.Seat.Value == "1").Blobbities.Count, Is.EqualTo(2));

        // The score shows as pips; the claimed blobbities themselves open on a tap, drawn with the same painter.
        AssertPips("Seat1", filled: 2, empty: 2);
        AssertPips("Seat2", filled: 0, empty: 4);
        Assert.That(FindRect("ClaimsPanel").gameObject.activeSelf, Is.False);
        LiveButton("Seat1").onClick.Invoke();
        AssertBlobbityCards("Claim", 2);
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
        table.Place(1, 0);
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        var panel = FindRect("ClaimsPanel");
        Assert.That(panel.gameObject.activeSelf, Is.False);

        LiveButton("Seat1").onClick.Invoke();

        Assert.That(panel.gameObject.activeSelf, Is.True);
        AssertBlobbityCards("Claim", 2);
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
    public void TheHandOffCover_SitsOverTheSecretBlobbitiesOnly()
    {
        var table = BeginHumanTable(SetupFrom(BlankTile("start"), BlankTile("one"), BlankTile("two"), BlankTile("three")));
        PlaceAt(LiveButtons("Highlight").First());
        Assert.That(table.Snapshot.ConcealVisible, Is.True);

        var conceal = FindRect("Conceal");
        var secrets = FindRect("Secrets");
        Assert.That(conceal.gameObject.activeSelf, Is.True);
        Assert.That(conceal.parent, Is.SameAs(secrets.parent));
        Assert.That(conceal.anchorMin, Is.EqualTo(secrets.anchorMin));
        Assert.That(conceal.anchorMax, Is.EqualTo(secrets.anchorMax));
        Assert.That(LiveButton("Spin"), Is.Not.Null);
        Assert.That(LiveButton("Confirm"), Is.Not.Null);
        Assert.That(_root.GetComponentsInChildren<RectTransform>(false).Any(rect => rect.name.StartsWith("Blobbity") && rect.name.Length > "Blobbity".Length && char.IsDigit(rect.name["Blobbity".Length])), Is.False, "no blobbity card is drawn under the cover");
    }

    [Test]
    public void WhenTheGameEnds_TheTrayNamesTheWinner_AndOffersPlayAgainAndNewSetup()
    {
        var table = BeginFinishedGame(TableStart.PassAndPlayDraft());

        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.Won), table.Snapshot.Status.Message);
        Assert.That(FindText("Heading").text, Is.EqualTo("Seat 1 wins!"));
        Assert.That(FindRect("Winner").gameObject.activeInHierarchy, Is.True);
        Assert.That(FindRect("Turn").gameObject.activeInHierarchy, Is.False);
        Assert.That(LiveButton("Spin"), Is.Null);
        Assert.That(LiveButton("PlayAgain"), Is.Not.Null);
        Assert.That(LiveButton("NewSetup"), Is.Not.Null);
        Assert.That(HighlightedSeats(), Is.EqualTo(new[] { "Seat1" }), "the winner is highlighted, not the seat that would be next");
        Assert.That(_root.GetComponentsInChildren<Text>(false).Any(text => text.text == "Pending tile" || text.text == "Blobbities hidden"), Is.False);
    }

    [Test]
    public void PlayAgain_StartsAFreshGame_WithTheSameSeatsAndTheFirstSeatMovedOn()
    {
        var finished = new TableStart(3, new[] { false, false, true }, 0, false);
        var table = BeginFinishedGame(finished);
        LiveButton("Seat1").onClick.Invoke();
        Assert.That(FindRect("ClaimsPanel").gameObject.activeSelf, Is.True);

        LiveButton("PlayAgain").onClick.Invoke();

        Assert.That(table.Snapshot.Status.Kind, Is.Not.EqualTo(TableStatusKind.Won));
        Assert.That(table.Snapshot.View.Claims.Count, Is.EqualTo(3));
        Assert.That(table.Snapshot.View.Claims.All(claims => claims.Blobbities.Count == 0), Is.True);
        Assert.That(table.Snapshot.View.CurrentSeat.Value, Is.EqualTo("2"));
        Assert.That(table.CurrentStart.Shuffle, Is.True, "Play again deals fresh even after an unshuffled game");
        Assert.That(FindRect("Winner").gameObject.activeInHierarchy, Is.False);
        Assert.That(FindRect("ClaimsPanel").gameObject.activeSelf, Is.False, "the claims panel does not carry over into the new game");

        table.NewSetup();
        AssertSetupShows(seats: 3, ai: new[] { false, false, true }, firstSeat: 1);
    }

    [Test]
    public void NewSetup_AfterAWin_ShowsSetupFilledWithTheFinishedGamesChoices()
    {
        BeginFinishedGame(new TableStart(4, new[] { false, true, false, true }, 0, true));

        LiveButton("NewSetup").onClick.Invoke();

        AssertSetupShows(seats: 4, ai: new[] { false, true, false, true }, firstSeat: 0);
        Assert.That(FindRect("Play").gameObject.activeSelf, Is.False);
        Assert.That(FindButton("Shuffle"), Is.Null, "the deck is always shuffled, so setup offers no choice");
    }

    [Test]
    public void NewSetup_AfterAnUnshuffledGame_StartsTheNextGameShuffled()
    {
        BeginFinishedGame(new TableStart(2, new[] { false, false }, 0, false));
        TableStart started = null;
        _root.GetComponent<TableView>().Started += draft => started ??= draft;

        LiveButton("NewSetup").onClick.Invoke();
        LiveButton("Start").onClick.Invoke();

        Assert.That(started, Is.Not.Null, "Start hands the draft on");
        Assert.That(started.Shuffle, Is.True, "setup shuffles the deck even when the finished game did not");
    }

    [Test]
    public void ARenderClosesTheClaimsPanel()
    {
        var table = BeginHumanTable(SetupFrom(BlankTile("start"), BlankTile("one"), BlankTile("two"), BlankTile("three")));
        LiveButton("Seat1").onClick.Invoke();
        Assert.That(FindRect("ClaimsPanel").gameObject.activeSelf, Is.True);

        table.SetQuarterTurns(0);

        Assert.That(FindRect("ClaimsPanel").gameObject.activeSelf, Is.False);
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
        table.SetQuarterTurns(0);
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
        table.Place(left.TileX, left.TileY);
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        Canvas.ForceUpdateCanvases();
        table.ConfirmIncomingSeat();
        Canvas.ForceUpdateCanvases();

        Assert.That(TileCentreInView(rightmost, view).x, Is.EqualTo(before.x).Within(1f));
    }

    // Driven through the real input module with a virtual mouse: a drag on a highlight scrolls the board and does not place, a tap places.
    [UnityTest]
    public IEnumerator DraggingAHighlightScrollsTheBoard_AndATapStillPreviewsAndPlaceStillPlaces()
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
            table.SetQuarterTurns(0);
            yield return null;
            Canvas.ForceUpdateCanvases();
            table.SetQuarterTurns(0);
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

            Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(before), "a tap only previews");
            LiveButton("Place").onClick.Invoke();
            Assert.That(table.Snapshot.View.Board.Tiles.Count, Is.EqualTo(before + 1), "Place places");
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
}
}
