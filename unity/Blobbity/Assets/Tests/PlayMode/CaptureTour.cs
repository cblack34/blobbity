namespace Blobbity.Player.Tests
{
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Blobbity.App;
using Blobbity.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// Retakes every capture in Assets/Captures. Opt-in: unity test unity/Blobbity --mode PlayMode --filter CaptureTour
[Explicit, Category("Capture")]
public class CaptureTour : TableSessionFixture
{
    private const int W = 1179;
    private const int H = 2556;
    private static readonly string Out = Path.Combine(Application.dataPath, "Captures");

    private Camera _cam;
    private RenderTexture _rt;
    private TableSession _table;
    private TableView _view;
    private System.Random _rng;

    [TearDown]
    public void Cleanup()
    {
        if (_cam != null) { Object.DestroyImmediate(_cam.gameObject); }
        if (_rt != null) { _rt.Release(); }
    }

    private static TableStart FourHumans() => new TableStart(4, new[] { false, false, false, false }, 0, true);

    private static GameSetup Deal(int seed)
    {
        var start = FourHumans();
        return TableDeck.Ordinary(start.SeatsInTurnOrder(), start.SeatAt(0), seed);
    }

    // The canvas is laid out for the render texture's size, 1179x2556, not the editor's Screen, so it must be set up before the game is drawn.
    private IEnumerator Open(int seed, bool instant, GameSetup setup = null)
    {
        _rng = new System.Random(seed);
        UseScreen();
        _table = NewTable();
        _view = _root.GetComponent<TableView>();
        _view.Recap.PlaysInstantly = instant;
        var canvas = _root.GetComponentInChildren<Canvas>();
        _rt = new RenderTexture(W, H, 24);
        _cam = new GameObject("CaptureCam").AddComponent<Camera>();
        _cam.targetTexture = _rt;
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = SplatPalette.Background;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = _cam;
        canvas.planeDistance = 1f;
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();
        var size = ((RectTransform)canvas.transform).rect.size;
        Assert.That(size.x, Is.EqualTo(1080f).Within(0.5f));
        Assert.That(size.y, Is.EqualTo(1080f * H / W).Within(1f), "canvas follows the render texture");
        if (setup != null)
        {
            _table.Begin(FourHumans(), setup);
        }
        else
        {
            _table.Begin(FourHumans(), Deal(seed));
        }
    }

    private void Shoot(string name)
    {
        Canvas.ForceUpdateCanvases();
        _cam.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = _rt;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    // The scripts below run against either the real table on screen or a bare App table, which is much faster for searching seeds.
    private Func<TableSnapshot> _snap;
    private Action _confirm;
    private Action<int, int> _place;
    private Action _settle;

    private void UseScreen()
    {
        _snap = () => _table.Snapshot;
        _confirm = () => _table.ConfirmIncomingSeat();
        _place = (x, y) => _table.Place(x, y);
        _settle = SkipTheWholeRecap;
    }

    private void UseBare(Table t)
    {
        _snap = () => t.Snapshot;
        _confirm = t.Confirm;
        _place = (x, y) => t.Place(x, y);
        _settle = () => { if (t.Snapshot.Recap.Count > 0) { t.AcknowledgeRecap(); } };
    }

    private void Ready()
    {
        if (_snap().ConcealVisible)
        {
            _confirm();
            _settle();
        }
    }

    private void PlaceCompact()
    {
        var tiles = _snap().View.Board.Tiles;
        var minX = tiles.Min(t => t.TileX);
        var maxX = tiles.Max(t => t.TileX);
        var minY = tiles.Min(t => t.TileY);
        var maxY = tiles.Max(t => t.TileY);
        var spot = _snap().LegalPlacements
            .Where(p => !tiles.Any(t => t.TileX == p.TileX && t.TileY == p.TileY))
            .OrderBy(p =>
            {
                var w = Math.Max(maxX, p.TileX) - Math.Min(minX, p.TileX) + 1;
                var h = Math.Max(maxY, p.TileY) - Math.Min(minY, p.TileY) + 1;
                return (Math.Max(w, h) * 100) + (w * h);
            })
            .ThenBy(_ => _rng.Next())
            .First();
        _place(spot.TileX, spot.TileY);
        _settle();
    }

    private void Step()
    {
        Ready();
        PlaceCompact();
    }

    private void Steps(int count)
    {
        for (var i = 0; i < count; i++) { Step(); }
    }

    private bool PlayUntil(Func<TableSnapshot, bool> ready, int max = 40)
    {
        for (var i = 0; i < max; i++)
        {
            Ready();
            if (_snap().CanAct && ready(_snap())) { return true; }
            if (_snap().LegalPlacements.Count == 0) { return false; }
            Step();
        }
        return false;
    }

    // Deals are seeded, so a scene is the same every run: the first seed whose deal reaches the wanted state (found on a bare table) is dealt and played on screen.
    private IEnumerator Try(bool instant, int firstSeed, Func<bool> script)
    {
        for (var seed = firstSeed; seed < firstSeed + 3000; seed++)
        {
            var start = FourHumans();
            var bare = new Table(new LocalSession(), start, Deal(seed));
            bare.Start();
            UseBare(bare);
            _rng = new System.Random(seed);
            bool found;
            try { found = script(); } catch (InvalidOperationException) { found = false; }
            if (!found) { continue; }
            Debug.Log("SEED " + seed);
            yield return Open(seed, instant);
            UseScreen();
            Assert.That(script(), Is.True);
            yield break;
        }
        Assert.Fail("no seed reaches the state");
    }

    private static bool Has(Tile tile, SymbolId symbol)
    {
        for (var x = 0; x < 2; x++)
        {
            for (var y = 0; y < 2; y++)
            {
                if (tile.Local(x, y).TryGetSymbol(out var found) && found.Equals(symbol)) { return true; }
            }
        }
        return false;
    }

    private static bool Targets(TableSnapshot s, SymbolId power, int atLeast) =>
        s.LegalTargets.Any(t => t.Power.Equals(power) && t.Targets.Count >= atLeast);

    private static bool Plain(TableSnapshot s) =>
        s.LegalTargets.All(t => t.Targets.Count == 0) && !Has(s.View.PendingMatchTile, OrdinaryCatalog.Stack) && !Has(s.View.PendingMatchTile, OrdinaryCatalog.Rotate) && !Has(s.View.PendingMatchTile, OrdinaryCatalog.Bounce);

    private static bool PlainTile(Tile t) => !Has(t, OrdinaryCatalog.Stack) && !Has(t, OrdinaryCatalog.Rotate) && !Has(t, OrdinaryCatalog.Bounce);

    private static int PlainSeed(int tiles) =>
        Enumerable.Range(0, 5000).First(seed => Deal(seed).MatchDeck.Skip(1).Take(tiles).All(PlainTile));

    private static Tile Pad(string id) => new Tile(new TileId(id), Cell.Color(OrdinaryCatalog.Blue), Cell.Color(OrdinaryCatalog.Blue), Cell.Color(OrdinaryCatalog.Green), Cell.Color(OrdinaryCatalog.Red));

    private static string Seat(TableSnapshot s) => s.View.CurrentSeat.Value;

    private void TapFirstTarget() => LiveButtons("Target").First().onClick.Invoke();

    [UnityTest] public IEnumerator Board()
    {
        yield return Try(true, 11, () => PlayUntil(s => s.View.Board.Tiles.Count >= 20 && Plain(s)));
        Shoot("board");
    }

    [UnityTest] public IEnumerator BoardScrolled()
    {
        yield return Try(true, 12, () =>
        {
            for (var i = 0; i < 9; i++)
            {
                Ready();
                var spot = _snap().LegalPlacements.OrderByDescending(p => p.TileX).First();
                _place(spot.TileX, spot.TileY);
                _settle();
            }
            Ready();
            return Plain(_snap());
        });
        Shoot("board-scrolled");
    }

    [UnityTest] public IEnumerator AfterBounce()
    {
        yield return Try(true, 13, () => PlayUntil(s => Targets(s, OrdinaryCatalog.Bounce, 8) && s.View.Board.Tiles.Count >= 11));
        TapFirstTarget();
        LiveButton("Bounce").onClick.Invoke();
        Assert.That(LiveButton("Bounce"), Is.Null);
        Shoot("after-bounce");
    }

    [UnityTest] public IEnumerator BounceSelect()
    {
        yield return Try(true, 13, () => PlayUntil(s => Targets(s, OrdinaryCatalog.Bounce, 8) && s.View.Board.Tiles.Count >= 11));
        TapFirstTarget();
        Assert.That(LiveButton("Bounce"), Is.Not.Null);
        Shoot("bounce-select");
    }

    [UnityTest] public IEnumerator ConcealBetweenHumans()
    {
        yield return Open(PlainSeed(4), true);
        Steps(3);
        Assert.That(_table.Snapshot.ConcealVisible, Is.True);
        Shoot("conceal-between-humans");
    }

    [UnityTest] public IEnumerator GameEnd()
    {
        var purple = Cell.Color(OrdinaryCatalog.Purple);
        var seats = FourHumans().SeatsInTurnOrder();
        var setup = new GameSetup(
            seats, seats[0], OrdinaryCatalog.Colors, OrdinaryCatalog.NonScoringSymbols, OrdinaryCatalog.Patterns, 1,
            Enumerable.Range(0, 16).Select(id => new BlobbityCard(new BlobbityId("m" + id), BlobbityPattern.Row, OrdinaryCatalog.Purple)).ToArray(),
            new[] { new Tile(new TileId("start"), purple, purple, purple, purple), new Tile(new TileId("next"), purple, purple, purple, purple), BlankTile("pad") });
        yield return Open(15, true, setup);
        _table.ConfirmIncomingSeat();
        _table.Place(1, 0);
        Assert.That(_table.Snapshot.Status.Kind, Is.EqualTo(TableStatusKind.Won));
        Shoot("game-end");
    }

    [UnityTest] public IEnumerator Highlights()
    {
        yield return Try(true, 16, () => PlayUntil(s => Has(s.View.PendingMatchTile, OrdinaryCatalog.Stack) && s.View.Board.Tiles.Count >= 10));
        Shoot("highlights");
    }

    [UnityTest] public IEnumerator PlacementPreview()
    {
        yield return Try(true, 17, () => PlayUntil(s => s.View.Board.Tiles.Count >= 16 && Plain(s)));
        LiveButtons("Highlight").First().onClick.Invoke();
        Assert.That(LiveButton("Place"), Is.Not.Null);
        Shoot("placement-preview");
    }

    [UnityTest] public IEnumerator PowerSplash()
    {
        // The first seat draws the tile after the start tile.
        var seed = Enumerable.Range(0, 400).First(s => Has(Deal(s).MatchDeck[1], OrdinaryCatalog.Rotate));
        yield return Open(seed, false);
        _table.ConfirmIncomingSeat();
        Assert.That(_view.Recap.IsPlaying, Is.True);
        Assert.That(_view.Recap.Caption, Is.EqualTo("You got a rotate!"));
        Shoot("power-splash");
    }

    [UnityTest] public IEnumerator RecapClaim()
    {
        var purple = Cell.Color(OrdinaryCatalog.Purple);
        var seats = FourHumans().SeatsInTurnOrder();
        BlobbityCard Card(string id, BlobbityPattern p, ColorId c) => new BlobbityCard(new BlobbityId(id), p, c);
        var cards = new[]
        {
            Card("a1", BlobbityPattern.Row, OrdinaryCatalog.Purple), Card("a2", BlobbityPattern.Square, OrdinaryCatalog.Purple),
            Card("b1", BlobbityPattern.Row, OrdinaryCatalog.Red), Card("b2", BlobbityPattern.Square, OrdinaryCatalog.Blue),
            Card("c1", BlobbityPattern.L, OrdinaryCatalog.Green), Card("c2", BlobbityPattern.Row, OrdinaryCatalog.Blue),
            Card("d1", BlobbityPattern.Square, OrdinaryCatalog.Green), Card("d2", BlobbityPattern.L, OrdinaryCatalog.Red),
            Card("r1", BlobbityPattern.Row, OrdinaryCatalog.Green), Card("r2", BlobbityPattern.Square, OrdinaryCatalog.Red),
            Card("r3", BlobbityPattern.L, OrdinaryCatalog.Blue), Card("r4", BlobbityPattern.Row, OrdinaryCatalog.Red),
        };
        var tile = new Func<string, Tile>(id => new Tile(new TileId(id), purple, purple, purple, purple));
        var setup = new GameSetup(seats, seats[0], OrdinaryCatalog.Colors, OrdinaryCatalog.NonScoringSymbols, OrdinaryCatalog.Patterns, 4, cards,
            new[] { tile("start"), tile("next"), Pad("pad1"), Pad("pad2"), Pad("pad3") });
        yield return Open(18, false, setup);
        _table.ConfirmIncomingSeat();
        _table.Place(1, 0);
        Debug.Log("CLAIM after place playing=" + _view.Recap.IsPlaying + " caption=" + _view.Recap.Caption + " conceal=" + _table.Snapshot.ConcealVisible);
        var guard = 0;
        while (_view.Recap.IsPlaying && _view.Recap.Caption != "Seat 1 claims!" && guard++ < 10) { _view.Recap.Skip(); Debug.Log("CLAIM skip -> " + _view.Recap.Caption); }
        Assert.That(_view.Recap.Caption, Is.EqualTo("Seat 1 claims!"));
        _view.Recap.Tick(0.5f);
        Shoot("recap-claim");
    }

    [UnityTest] public IEnumerator RecapPlacement()
    {
        yield return Open(PlainSeed(2), false);
        Steps(1);
        Ready();
        Shoot("recap-placement");
    }

    [UnityTest] public IEnumerator ReshuffleNotice()
    {
        yield return Open(PlainSeed(4), false);
        Steps(3);
        Ready();
        Shoot("reshuffle-notice");
    }

    [UnityTest] public IEnumerator RotatePreview()
    {
        yield return Try(false, 21, () => PlayUntil(s => Seat(s) == "2" && Targets(s, OrdinaryCatalog.Rotate, 8) && s.View.Board.Tiles.Count >= 10));
        TapFirstTarget();
        LiveButton("Turn").onClick.Invoke();
        Shoot("rotate-preview");
    }

    [UnityTest] public IEnumerator SeatClaims()
    {
        var purple = Cell.Color(OrdinaryCatalog.Purple);
        var seats = FourHumans().SeatsInTurnOrder();
        BlobbityCard Card(string id, BlobbityPattern p, ColorId c) => new BlobbityCard(new BlobbityId(id), p, c);
        var cards = new[]
        {
            Card("a1", BlobbityPattern.Row, OrdinaryCatalog.Purple), Card("a2", BlobbityPattern.Square, OrdinaryCatalog.Purple),
            Card("b1", BlobbityPattern.Row, OrdinaryCatalog.Red), Card("b2", BlobbityPattern.Square, OrdinaryCatalog.Blue),
            Card("c1", BlobbityPattern.L, OrdinaryCatalog.Green), Card("c2", BlobbityPattern.Row, OrdinaryCatalog.Blue),
            Card("d1", BlobbityPattern.Square, OrdinaryCatalog.Green), Card("d2", BlobbityPattern.L, OrdinaryCatalog.Red),
            Card("r1", BlobbityPattern.Row, OrdinaryCatalog.Green), Card("r2", BlobbityPattern.Square, OrdinaryCatalog.Red),
            Card("r3", BlobbityPattern.L, OrdinaryCatalog.Blue), Card("r4", BlobbityPattern.Row, OrdinaryCatalog.Red),
        };
        var tile = new Func<string, Tile>(id => new Tile(new TileId(id), purple, purple, purple, purple));
        var setup = new GameSetup(seats, seats[0], OrdinaryCatalog.Colors, OrdinaryCatalog.NonScoringSymbols, OrdinaryCatalog.Patterns, 4, cards,
            new[] { tile("start"), tile("next"), Pad("pad1"), Pad("pad2"), Pad("pad3") });
        yield return Open(22, false, setup);
        _table.ConfirmIncomingSeat();
        _table.Place(1, 0);
        SkipTheWholeRecap();
        Assert.That(_table.Snapshot.ConcealVisible, Is.True);
        LiveButton("Seat1").onClick.Invoke();
        Shoot("seat-claims");
    }

    [UnityTest] public IEnumerator Secrets()
    {
        yield return Try(false, 23, () => PlayUntil(s => Seat(s) == "2" && s.View.Board.Tiles.Count >= 10 && Plain(s)));
        Shoot("secrets");
    }
}
}
