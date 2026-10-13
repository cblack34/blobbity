namespace MissionSplat.Player.Tests
{

using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using MissionSplat.App;
using MissionSplat.Player;
using MissionSplat.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// The recap: what plays after an action, what it blocks, and how a tap moves through it. Recaps play for real here.
public class TableSessionRecapPlayModeTests : TableSessionFixture
{
    protected override bool RecapsPlayInstantly => false;

    [Test]
    public void AfterAHumanPlacesAgainstAnAi_TheRecapPlaysOverABlockedTable_AndEachTapSkipsExactlyOneItem()
    {
        var table = NewTable();
        table.Begin(TableStart.HumanThenAi());
        table.ConfirmIncomingSeat();
        var recap = _root.GetComponent<TableView>().Recap;
        Assert.That(recap.IsPlaying, Is.False, "nothing has happened yet");
        Assert.That(TheSkipLayerCoversAndTakesEveryTap(), Is.False);

        table.Place(1, 0);

        var items = table.Snapshot.Recap;
        Assert.That(items.Count, Is.GreaterThanOrEqualTo(2), "the human's placement, then the AI's turn");
        Assert.That(items[0], Is.InstanceOf<RecapPlaced>());
        Assert.That(items[0].Seat.Value, Is.EqualTo("1"));
        Assert.That(recap.IsPlaying, Is.True);
        Assert.That(recap.Count, Is.EqualTo(items.Count));
        Assert.That(LiveRect("RecapOverlay"), Is.Not.Null);
        Assert.That(LiveRect("RecapPlaced"), Is.Not.Null, "the placement drops in");
        Assert.That(TheSkipLayerCoversAndTakesEveryTap(), Is.True, "nothing underneath can be tapped");
        Assert.That(FindText("Heading").text, Is.EqualTo("Seat 1 places a tile"), "the heading says what is playing");
        Assert.That(FindText("Status").text, Is.Empty, "the instruction waits");

        for (var tap = 1; tap < items.Count; tap++)
        {
            LiveButton("RecapSkip").onClick.Invoke();
            Assert.That(recap.Index, Is.EqualTo(tap), "one tap moves on one item");
            Assert.That(recap.IsPlaying, Is.True);
            Assert.That(table.Snapshot.Recap.Count, Is.EqualTo(items.Count), "nothing is acknowledged before the last item ends");
        }

        LiveButton("RecapSkip").onClick.Invoke();

        Assert.That(recap.IsPlaying, Is.False);
        Assert.That(table.Snapshot.Recap, Is.Empty, "the table counts the recap seen");
        Assert.That(LiveRect("RecapOverlay"), Is.Null);
        Assert.That(TheSkipLayerCoversAndTakesEveryTap(), Is.False);
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty, "the table takes input again");
        Assert.That(table.Snapshot.CanAct, Is.True);
        Assert.That(FindText("Heading").text, Is.EqualTo("Seat 1’s turn!"));
        Assert.That(FindText("Status").text, Is.Not.Empty);
        var summary = FindText("Notice");
        Assert.That(summary.gameObject.activeInHierarchy, Is.True);
        Assert.That(summary.text, Does.StartWith("Seat 1 placed a tile. Seat 2 "));
    }

    [UnityTest]
    public IEnumerator ARecapLeftAlone_PlaysThroughOnItsOwn_ThenAcknowledges()
    {
        var table = NewTable();
        table.Begin(TableStart.HumanThenAi());
        table.ConfirmIncomingSeat();
        var recap = _root.GetComponent<TableView>().Recap;
        table.Place(1, 0);
        Assert.That(recap.IsPlaying, Is.True);

        var started = Time.realtimeSinceStartup;
        while (recap.IsPlaying && Time.realtimeSinceStartup - started < 10f)
        {
            yield return null;
        }

        Assert.That(recap.IsPlaying, Is.False, "the items end by themselves");
        Assert.That(Time.realtimeSinceStartup - started, Is.GreaterThan(0.4f), "a placement takes about half a second");
        Assert.That(table.Snapshot.Recap, Is.Empty);
        Assert.That(FindText("Notice").text, Does.StartWith("Seat 1 placed a tile."));
    }

    [Test]
    public void BetweenTwoHumans_TheOutgoingPlayersClaimPlaysBeforeTheCover_AndTheIncomingPlayersRecapAfterConfirm()
    {
        var table = BeginHumanTable(PurpleRowSetup());

        table.Place(1, 0);

        var claims = table.Snapshot.Recap.OfType<RecapClaimed>().ToArray();
        Assert.That(claims.Length, Is.EqualTo(2), "the placement finishes both purple rows");
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        var recap = _root.GetComponent<TableView>().Recap;
        Assert.That(recap.IsPlaying, Is.True);
        Assert.That(FindRect("Conceal").gameObject.activeSelf, Is.False, "the device is still the outgoing player's");
        Assert.That(LiveButton("Confirm"), Is.Null);
        Assert.That(FindText("Heading").text, Is.EqualTo("Seat 1 places a tile"), "not yet Seat 2's turn");
        Assert.That(FindText("Status").text, Is.Empty, "the hand-off instruction waits");

        // Nothing of the incoming player's is on the table while the outgoing player watches.
        Assert.That(LiveRect("Mission0"), Is.Null, "no secret mission card is drawn");
        Assert.That(LiveText("Title").text, Is.EqualTo("Missions hidden"));

        table.ConfirmIncomingSeat();
        Assert.That(recap.IsPlaying, Is.True, "a confirm during the recap is ignored");
        Assert.That(table.Snapshot.ConcealVisible, Is.True);

        LiveButton("RecapSkip").onClick.Invoke();
        Assert.That(recap.Index, Is.EqualTo(1));
        Assert.That(LiveRect("RecapClaim"), Is.Not.Null, "the first claim pops up");
        Assert.That(FindText("Heading").text, Is.EqualTo("Seat 1 claims!"));
        Assert.That(FindRect("Conceal").gameObject.activeSelf, Is.False);
        LiveButton("RecapSkip").onClick.Invoke();
        Assert.That(recap.Index, Is.EqualTo(2));
        LiveButton("RecapSkip").onClick.Invoke();

        Assert.That(recap.IsPlaying, Is.False);
        Assert.That(FindRect("Conceal").gameObject.activeSelf, Is.True, "now the cover goes up");
        Assert.That(LiveButton("Confirm"), Is.Not.Null);
        Assert.That(FindText("Heading").text, Is.EqualTo("Seat 2’s turn!"));
        Assert.That(FindText("Status").text, Does.Contain("Hand the device to Seat 2"));
        Assert.That(FindText("Notice").text, Is.EqualTo("Seat 1 placed a tile, claimed a row and a row."));

        table.ConfirmIncomingSeat();

        Assert.That(recap.IsPlaying, Is.True, "the incoming player sees what happened");
        Assert.That(recap.Count, Is.EqualTo(3));
        Assert.That(table.Snapshot.Recap.Count, Is.EqualTo(3));
        SkipTheWholeRecap();

        Assert.That(table.Snapshot.Recap, Is.Empty);
        Assert.That(LiveButtons("Highlight"), Is.Not.Empty);
    }

    [Test]
    public void TheSummaryStaysUntilThePlayersNextAction_ThenGoes()
    {
        var table = BeginHumanTable(SetupFrom(BlankTile("start"), BlankTile("one"), BlankTile("two"), BlankTile("three")));
        Assert.That(FindText("Notice").gameObject.activeInHierarchy, Is.False);
        table.Place(1, 0);
        LiveButton("RecapSkip").onClick.Invoke();
        Assert.That(FindText("Notice").text, Is.EqualTo("Seat 1 placed a tile."));
        table.ConfirmIncomingSeat();
        Assert.That(FindText("Notice").gameObject.activeInHierarchy, Is.False, "the incoming recap is still to play");
        LiveButton("RecapSkip").onClick.Invoke();
        Assert.That(FindText("Notice").text, Is.EqualTo("Seat 1 placed a tile."));

        table.SelectPower(null);
        Assert.That(FindText("Notice").text, Is.EqualTo("Seat 1 placed a tile."), "picking a power is not the next action");

        PlaceAt(LiveButtons("Highlight").First());
        Assert.That(FindText("Notice").gameObject.activeInHierarchy, Is.False, "the action clears it");
        LiveButton("RecapSkip").onClick.Invoke();
        Assert.That(FindText("Notice").text, Is.EqualTo("Seat 2 placed a tile."), "the new action's own recap replaces it");
    }

    [Test]
    public void EachKindOfItemHasItsOwnPicture_AndTheEndOfTheGameIsAnnounced()
    {
        var rotate = BeginHumanTable(RotateSetup());
        rotate.SelectPower(OrdinaryCatalog.Rotate);
        rotate.SetQuarterTurns(2);
        rotate.UseAt(0, 0);
        Assert.That(rotate.Snapshot.Recap.Single(), Is.InstanceOf<RecapRotated>());
        Assert.That(LiveRect("RecapRotated"), Is.Not.Null);
        Assert.That(FindText("Heading").text, Is.EqualTo("Seat 1 rotates a tile"));
        Object.DestroyImmediate(_root);

        var bounce = BeginHumanTable(BounceSetup());
        bounce.SelectPower(OrdinaryCatalog.Bounce);
        bounce.UseAt(0, 0);
        Assert.That(bounce.Snapshot.Recap.Single(), Is.InstanceOf<RecapBounced>());
        Assert.That(LiveRect("RecapBounced"), Is.Not.Null);
        Assert.That(FindText("Heading").text, Is.EqualTo("Seat 1 bounces a tile"));
        Object.DestroyImmediate(_root);

        var reshuffle = BeginHumanTable(SetupFrom(BlankTile("start"), BlankTile("one"), BlankTile("two")));
        PlaceAt(LiveButtons("Highlight").First());
        SkipTheWholeRecap();
        reshuffle.ConfirmIncomingSeat();
        SkipTheWholeRecap();
        PlaceAt(LiveButtons("Highlight").First());
        var reshuffleItems = reshuffle.Snapshot.Recap;
        Assert.That(reshuffleItems.Last(), Is.InstanceOf<RecapReshuffled>());
        var recap = _root.GetComponent<TableView>().Recap;
        while (recap.Index < reshuffleItems.Count - 1)
        {
            LiveButton("RecapSkip").onClick.Invoke();
        }

        Assert.That(FindText("Message", LiveRect("RecapBanner")).text, Is.EqualTo("Board reshuffled"));
        Assert.That(FindText("Heading").text, Is.EqualTo("Board reshuffled"));
        Object.DestroyImmediate(_root);

        var won = BeginFinishedGame(TableStart.PassAndPlayDraft());
        var wonItems = won.Snapshot.Recap;
        Assert.That(wonItems.Last(), Is.InstanceOf<RecapWon>());
        recap = _root.GetComponent<TableView>().Recap;
        while (recap.Index < wonItems.Count - 1)
        {
            LiveButton("RecapSkip").onClick.Invoke();
        }

        Assert.That(FindText("Message", LiveRect("RecapBanner")).text, Is.EqualTo("Seat 1 wins!"));
        Assert.That(LiveButton("PlayAgain"), Is.Null, "the result waits for the recap that led to it");
        LiveButton("RecapSkip").onClick.Invoke();
        Assert.That(LiveButton("PlayAgain"), Is.Not.Null);
        Assert.That(FindText("Heading").text, Is.EqualTo("Seat 1 wins!"));
        Assert.That(FindText("Notice").text, Does.EndWith("won the game."));
    }

    [Test]
    public void EachClaimFliesToItsOwnPipInTheClaimingSeatsEntry()
    {
        var table = BeginHumanTable(PurpleRowSetup());
        table.Place(1, 0);
        var recap = _root.GetComponent<TableView>().Recap;
        var pips = LiveButton("Seat1").GetComponentsInChildren<RectTransform>(false).Where(rect => rect.name == "PipFilled").ToArray();
        Assert.That(pips.Length, Is.EqualTo(2));
        var screen = LiveRect("RecapScreen");

        for (var claim = 0; claim < 2; claim++)
        {
            LiveButton("RecapSkip").onClick.Invoke();
            var card = LiveRect("RecapClaim");
            var rest = card.anchoredPosition;
            recap.Tick(0.5f);
            Assert.That(card.anchoredPosition, Is.EqualTo(rest), "the card holds at the middle first");
            recap.Tick(0.49f);
            var pip = (Vector2)screen.InverseTransformPoint(pips[claim].position);
            Assert.That(Vector2.Distance(card.anchoredPosition, pip), Is.LessThan(15f), "claim " + claim + " lands on pip " + claim);
        }
    }

    [Test]
    public void AnItemThatCannotBeDrawn_IsLogged_AndTheRecapStillEndsAndIsAcknowledged()
    {
        var table = BeginHumanTable(SetupFrom(BlankTile("start"), BlankTile("one"), BlankTile("two")));
        var recap = _root.GetComponent<TableView>().Recap;
        recap.Begin = (_, _) => throw new InvalidOperationException("cannot draw this");
        LogAssert.Expect(LogType.Exception, new Regex("cannot draw this"));

        table.Place(1, 0);

        Assert.That(recap.IsPlaying, Is.True, "the item is held briefly, unseen");
        Assert.That(LiveRect("RecapPlaced"), Is.Null);
        Assert.That(TheSkipLayerCoversAndTakesEveryTap(), Is.True);
        recap.Tick(1f);

        Assert.That(recap.IsPlaying, Is.False);
        Assert.That(table.Snapshot.Recap, Is.Empty, "the recap was acknowledged");
        Assert.That(LiveRect("RecapOverlay"), Is.Null, "the overlay closed");
        Assert.That(table.Snapshot.ConcealVisible, Is.True);
        Assert.That(LiveButton("Confirm"), Is.Not.Null, "the table carries on");
    }

    [Test]
    public void ATileOutOfView_IsScrolledIntoView_BeforeItsItemPlays()
    {
        var table = BeginLongRowTable();
        var scroll = _root.GetComponentInChildren<ScrollRect>(true);
        var view = scroll.GetComponent<RectTransform>();
        scroll.content.anchoredPosition = new Vector2((scroll.content.rect.width - view.rect.width) / 2f, 0f);
        Canvas.ForceUpdateCanvases();
        var spot = table.Snapshot.LegalPlacements.OrderByDescending(slot => slot.TileX).First();

        table.Place(spot.TileX, spot.TileY);

        var tile = LiveRect("RecapPlaced");
        Assert.That(tile, Is.Not.Null);
        var local = view.InverseTransformPoint(tile.position);
        Assert.That(Mathf.Abs(local.x), Is.LessThanOrEqualTo((view.rect.width - tile.sizeDelta.x) / 2f + 1f), "the tile is wholly in the board's view");
        var after = scroll.content.anchoredPosition;
        SkipTheWholeRecap();
        Assert.That(scroll.content.anchoredPosition.x, Is.EqualTo(after.x).Within(1f), "the board stays where the item took it");
    }

    // A bounce or a reshuffle can leave a recap item outside the board as it now is, so the board reaches each pending item's tile.
    [TestCase(12, 0)]
    [TestCase(-12, 3)]
    public void ARecapItemBeyondTheBoardsEdge_IsStillReachedByTheBoard(int tileX, int tileY)
    {
        var now = BeginHumanTable(SetupFrom(BlankTile("start"), BlankTile("one"), BlankTile("two"))).Snapshot;
        var seat = now.View.CurrentSeat;
        var cells = new OccupiedCell[0];
        var items = new RecapItem[]
        {
            new RecapPlaced(seat, new TileId("one"), tileX, tileY, 0, cells, false),
            new RecapBounced(seat, -tileX, -tileY, cells, cells),
        };
        var snapshot = new TableSnapshot(now.View, false, false, true, now.LegalPlacements, now.LegalTargets, 0, now.Status, null, now.Notice, seat, items);
        var view = Ui.Rect("View", _root.transform);
        view.anchorMin = view.anchorMax = new Vector2(0.5f, 0.5f);
        view.sizeDelta = new Vector2(600f, 600f);
        var board = new BoardPanel(view);

        board.Paint(snapshot);

        foreach (var (x, y) in new[] { (tileX, tileY), (-tileX, -tileY) })
        {
            board.RevealTile(x, y);
            var slot = board.TileSlot(x, y).Value;
            var local = view.InverseTransformPoint(slot.Centre);
            var reach = (view.rect.size / 2f) - new Vector2(slot.Side / 2f, slot.Side / 2f);
            Assert.That(Mathf.Abs(local.x), Is.LessThanOrEqualTo(reach.x + 1f), "tile " + x + "," + y + " is wholly in view horizontally");
            Assert.That(Mathf.Abs(local.y), Is.LessThanOrEqualTo(reach.y + 1f), "tile " + x + "," + y + " is wholly in view vertically");
        }
    }

    [Test]
    public void TheRecapSummary_NamesWhatHappenedInPlainWords()
    {
        var seat1 = new SeatId("1");
        var seat2 = new SeatId("2");
        var row = new Mission(new MissionId("a"), MissionPattern.Row, OrdinaryCatalog.Purple);
        var ell = new Mission(new MissionId("b"), MissionPattern.L, OrdinaryCatalog.Red);
        var square = new Mission(new MissionId("c"), MissionPattern.Square, OrdinaryCatalog.Blue);
        var cells = new OccupiedCell[0];

        Assert.That(RecapSummary.Of(new RecapItem[0]), Is.Null);
        Assert.That(
            RecapSummary.Of(new RecapItem[] { new RecapPlaced(seat2, new TileId("t"), 0, 0, 0, cells, false), new RecapClaimed(seat1, ell) }),
            Is.EqualTo("Seat 2 placed a tile. Seat 1 claimed an L."));
        Assert.That(
            RecapSummary.Of(new RecapItem[] { new RecapRotated(seat1, 0, 0, 1, cells), new RecapPlaced(seat1, new TileId("t"), 1, 0, 0, cells, true), new RecapClaimed(seat1, row), new RecapClaimed(seat1, square) }),
            Is.EqualTo("Seat 1 rotated a tile, placed a tile, claimed a row and a square."), "one \"and\" only");
        Assert.That(
            RecapSummary.Of(new RecapItem[] { new RecapBounced(seat2, 0, 0, cells, cells), new RecapReshuffled(seat2), new RecapClaimed(seat2, row), new RecapWon(seat2, 4) }),
            Is.EqualTo("Seat 2 bounced a tile, reshuffled the board, claimed a row and won the game."));
        Assert.That(RecapSummary.Caption(new RecapPlaced(seat2, new TileId("t"), 0, 0, 0, cells, false)), Is.EqualTo("Seat 2 places a tile"));
        Assert.That(RecapSummary.Caption(new RecapClaimed(seat1, ell)), Is.EqualTo("Seat 1 claims!"));
        Assert.That(RecapSummary.Caption(new RecapReshuffled(seat1)), Is.EqualTo("Board reshuffled"));
    }

    // Two humans: the first seat's second tile finishes a purple row beside the start, which claims both of its purple-row missions.
    private static GameSetup PurpleRowSetup()
    {
        var purple = Cell.Color(OrdinaryCatalog.Purple);
        return SetupFrom(
            new[] { "a1", "a2", "b1", "b2", "r1", "r2", "r3", "r4" },
            new Tile(new TileId("start"), purple, purple, purple, purple),
            new Tile(new TileId("next"), purple, purple, purple, purple),
            BlankTile("pad"));
    }

    // Whether the recap's skip layer is the top of the table: drawn last, stretched over all of it, the only thing in the overlay that takes a tap,
    // so a tap anywhere lands on it and nothing underneath. (The raycaster itself needs a real display, which a headless run lacks.)
    private bool TheSkipLayerCoversAndTakesEveryTap()
    {
        var skip = LiveButton("RecapSkip");
        var overlay = LiveRect("RecapOverlay");
        if (skip == null || overlay == null)
        {
            return false;
        }

        var stretched = skip.GetComponent<RectTransform>();
        return overlay.GetSiblingIndex() == overlay.parent.childCount - 1
            && overlay.anchorMin == Vector2.zero && overlay.anchorMax == Vector2.one
            && stretched.anchorMin == Vector2.zero && stretched.anchorMax == Vector2.one
            && stretched.offsetMin == Vector2.zero && stretched.offsetMax == Vector2.zero
            && skip.GetComponent<Image>().raycastTarget
            && overlay.GetComponentsInChildren<Graphic>(false).Where(graphic => graphic.raycastTarget).All(graphic => graphic.gameObject == skip.gameObject);
    }

    // Live (active) rects only: a render's replaced objects stay in the tree, inactive, until the frame ends.
    private Text LiveText(string name) =>
        _root.GetComponentsInChildren<Text>(false).First(text => text.name == name);
}
}
