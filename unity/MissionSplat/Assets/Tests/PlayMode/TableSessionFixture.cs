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

// What the table session tests share: a root object the session lives on, and the tools to find what it drew.
public abstract class TableSessionFixture
{
    protected GameObject _root;

    // Most tests tap and assert straight away, so a recap is over the moment it starts; the recap fixture plays them for real.
    protected virtual bool RecapsPlayInstantly => true;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            Object.DestroyImmediate(_root);
        }
    }

    protected TableSession NewTable()
    {
        _root = new GameObject("Table");
        var table = _root.AddComponent<TableSession>();
        _root.GetComponent<TableView>().Recap.PlaysInstantly = RecapsPlayInstantly;
        return table;
    }


    // A chip's face colour, and whether it shows as grayed out: its power's badge colour while it can be used, gray when it cannot.
    protected Color ChipFace(string chip) => Ui.ToyFace(LiveButton(chip)).GetComponent<Image>().color;

    protected bool ChipGrayedOut(string chip)
    {
        var cover = Ui.ToyFace(LiveButton(chip)).Find("Disabled");
        return cover != null && cover.gameObject.activeSelf;
    }

    protected TableSession BeginLongRowTable()
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

    // The mouse at position with the left button down; held through each call it presses, and moved it drags.
    protected static IEnumerator Hold(Mouse mouse, Vector2 position)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
        yield return null;
        yield return null;
    }

    protected static IEnumerator Release(Mouse mouse, Vector2 position)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
        yield return null;
        yield return null;
    }

    protected void PlaceRightmost(TableSession table)
    {
        if (table.Snapshot.ConcealVisible)
        {
            table.ConfirmIncomingSeat();
            SkipTheWholeRecap();
        }

        var spot = table.Snapshot.LegalPlacements.OrderByDescending(slot => slot.TileX).First();
        table.Tap(spot.TileX, spot.TileY);
        SkipTheWholeRecap();
        if (table.Snapshot.ConcealVisible)
        {
            table.ConfirmIncomingSeat();
            SkipTheWholeRecap();
        }
    }

    // Taps through whatever recap is playing, as a player would; nothing when none is.
    protected void SkipTheWholeRecap()
    {
        var recap = _root.GetComponent<TableView>().Recap;
        while (recap.IsPlaying)
        {
            LiveButton("RecapSkip").onClick.Invoke();
        }
    }

    // Where a board tile's centre sits relative to the middle of the board view, in canvas units.
    protected Vector2 TileCentreInView(string tileName, RectTransform view)
    {
        var tile = _root.GetComponentsInChildren<RectTransform>(false).First(rect => rect.name == tileName);
        var local = view.InverseTransformPoint(tile.TransformPoint(tile.rect.center));
        return new Vector2(local.x, local.y);
    }

    protected void AssertPips(string seatButton, int filled, int empty)
    {
        var pips = LiveButton(seatButton).GetComponentsInChildren<RectTransform>(false);
        Assert.That(pips.Count(rect => rect.name == "PipFilled"), Is.EqualTo(filled), seatButton + " filled pips");
        Assert.That(pips.Count(rect => rect.name == "PipEmpty"), Is.EqualTo(empty), seatButton + " empty pips");
    }

    // Every live slot named prefix + index holds one 5:7 fit-in-parent Card whose pattern is four splats.
    protected void AssertMissionCards(string prefix, int expected)
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

    protected void AssertSetupShows(int seats, bool[] ai, int firstSeat)
    {
        Assert.That(LiveButton("Start"), Is.Not.Null, "setup is on screen");
        Assert.That(ButtonFace("Seats" + seats), Is.EqualTo(SplatPalette.Sunny));
        Assert.That(LiveButtons("Human").Length, Is.EqualTo(seats));
        for (var i = 0; i < seats; i++)
        {
            Assert.That(ButtonFace((ai[i] ? "Ai" : "Human") + i), Is.EqualTo(SplatPalette.Sunny), "seat " + i);
            Assert.That(ButtonFace("First" + i), Is.EqualTo(i == firstSeat ? SplatPalette.Sunny : SplatPalette.Paper), "first " + i);
        }
    }

    protected string[] HighlightedSeats() =>
        LiveButtons("Seat").Where(button => button.GetComponent<Image>().color.a > 0f).Select(button => button.name).ToArray();

    protected Color ButtonFace(string name) => Ui.ToyFace(LiveButton(name)).GetComponent<Image>().color;

    // Seat 1 is first, human, and places a tile that finishes its only claim needed to win.
    protected TableSession BeginFinishedGame(TableStart start)
    {
        var purple = Cell.Color(OrdinaryCatalog.Purple);
        var seats = start.SeatsInTurnOrder();
        var setup = new GameSetup(
            seats,
            seats[0],
            OrdinaryCatalog.Colors,
            OrdinaryCatalog.NonScoringSymbols,
            OrdinaryCatalog.Patterns,
            1,
            Enumerable.Range(0, 16).Select(id => new Mission(new MissionId("m" + id), MissionPattern.Row, OrdinaryCatalog.Purple)).ToArray(),
            new[] { new Tile(new TileId("start"), purple, purple, purple, purple), new Tile(new TileId("next"), purple, purple, purple, purple), BlankTile("pad") });
        var table = NewTable();
        table.Begin(start, setup);
        table.ConfirmIncomingSeat();
        table.Tap(1, 0);
        Assert.That(table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.Won), table.Snapshot.Status.Message);
        return table;
    }

    protected TableSession BeginHumanTable(GameSetup setup)
    {
        var table = NewTable();
        table.Begin(TableStart.PassAndPlayDraft(), setup);
        table.ConfirmIncomingSeat();
        return table;
    }

    // An asymmetric start tile under a drawn tile with one rotate cell, so a turn shows in the cells.
    protected static GameSetup RotateSetup() => SetupFrom(
        new Tile(new TileId("start"), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Blue), Cell.Color(OrdinaryCatalog.Green), Cell.Color(OrdinaryCatalog.Purple)),
        new Tile(new TileId("spinner"), Cell.Symbol(OrdinaryCatalog.Rotate), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red)),
        BlankTile("pad"));

    // The first seat places a plain tile beside the start; the second seat then draws the tile with a bounce cell.
    protected static GameSetup BounceBoardSetup() => SetupFrom(
        BlankTile("start"),
        new Tile(new TileId("side"), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red)),
        new Tile(new TileId("lifter"), Cell.Symbol(OrdinaryCatalog.Bounce), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Red)),
        BlankTile("pad"));

    protected static Tile BlankTile(string id)
    {
        var blank = Cell.Symbol(OrdinaryCatalog.Blank);
        return new Tile(new TileId(id), blank, blank, blank, blank);
    }

    protected static string CellsOf(SeatView view) =>
        string.Join(";", view.Board.Cells.Select(cell => cell.CellX + "," + cell.CellY + ":" + cell.Value));

    protected static GameSetup StackSetup() => SetupWith(
        new Tile(new TileId("lid"), Cell.Symbol(OrdinaryCatalog.Stack), Cell.Color(OrdinaryCatalog.Red), Cell.Color(OrdinaryCatalog.Blue), Cell.Symbol(OrdinaryCatalog.Blank)));

    protected static GameSetup BounceSetup() => SetupWith(
        new Tile(new TileId("lid"), Cell.Symbol(OrdinaryCatalog.Bounce), Cell.Symbol(OrdinaryCatalog.Blank), Cell.Symbol(OrdinaryCatalog.Blank), Cell.Symbol(OrdinaryCatalog.Blank)));

    protected static GameSetup MixedSetup() => SetupWith(
        new Tile(new TileId("mixed"), Cell.Symbol(OrdinaryCatalog.Rotate), Cell.Symbol(OrdinaryCatalog.Bounce), Cell.Symbol(OrdinaryCatalog.Blank), Cell.Symbol(OrdinaryCatalog.Blank)));

    protected static GameSetup SetupWith(Tile drawn) => SetupFrom(BlankTile("start"), drawn, BlankTile("pad"));

    protected static GameSetup SetupFrom(params Tile[] tiles) =>
        SetupFrom(new[] { "a1", "a2", "b1", "b2", "spare" }, tiles);

    // A seat that claims draws a replacement, so a test that claims needs missions beyond the opening hands.
    protected static GameSetup SetupFrom(string[] missionIds, params Tile[] tiles)
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
    protected Vector2 PendingCellPosition(string cell) =>
        _root.GetComponentsInChildren<RectTransform>(false)
            .First(rect => rect.name == cell && IsUnder(rect, "Pending"))
            .anchoredPosition;

    protected static bool IsUnder(Transform node, string ancestor)
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

    protected RectTransform FindRect(string name)
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
    protected Button LiveButton(string name) =>
        _root.GetComponentsInChildren<Button>(false).FirstOrDefault(button => button.name == name);

    protected RectTransform LiveRect(string name) =>
        _root.GetComponentsInChildren<RectTransform>(false).FirstOrDefault(rect => rect.name == name);

    // Taps a legal spot, which previews the drawn tile there, then presses Place.
    protected void PlaceAt(Button spot)
    {
        spot.onClick.Invoke();
        LiveButton("Place").onClick.Invoke();
    }

    protected Button[] LiveButtons(string prefix) =>
        _root.GetComponentsInChildren<Button>(false).Where(button => button.name.StartsWith(prefix)).ToArray();

    protected Button FindButton(string name)
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

    protected static Text FindText(string name, Component within)
    {
        return within.GetComponentsInChildren<Text>(true).First(text => text.name == name);
    }

    protected Text FindText(string name)
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
