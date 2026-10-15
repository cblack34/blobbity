namespace MissionSplat.Player.Tests
{

using System.Linq;
using MissionSplat.Player;
using MissionSplat.Rules;
using NUnit.Framework;
using UnityEngine;

// The symbols that mark each splat colour for players who cannot tell them by sight.
public class SplatSymbolPlayModeTests
{
    private const float Tolerance = 0.002f;

    private GameObject _root;

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_root);

    // A square 100 x 100 slot, which is what every real caller hands the painters.
    private RectTransform NewSlot()
    {
        _root = new GameObject("Slot", typeof(RectTransform));
        var slot = (RectTransform)_root.transform;
        slot.sizeDelta = new Vector2(100f, 100f);
        return slot;
    }

    private static Transform[] SymbolsIn(Transform root) =>
        root.GetComponentsInChildren<Transform>(true).Where(t => t.name == CellPainter.SymbolName).ToArray();

    [TestCase("red", 70f)]
    [TestCase("blue", 130f)]
    [TestCase("green", 250f)]
    [TestCase("purple", 333f)]
    public void AColourCellTurnedOnItsSplat_HasItsSymbolUprightOnTheBody(string colour, float angle)
    {
        var slot = NewSlot();

        CellPainter.Paint(slot, Cell.Color(new ColorId(colour)), angle);

        var splat = slot.Find("Splat");
        var symbol = splat.Find(CellPainter.SymbolName);
        Assert.That(Mathf.DeltaAngle(splat.eulerAngles.z, angle), Is.EqualTo(0f).Within(0.01f), "the splat turns");
        Assert.That(Mathf.DeltaAngle(symbol.eulerAngles.z, 0f), Is.EqualTo(0f).Within(0.01f), "the symbol stays upright");
        var rect = (RectTransform)symbol;
        var centre = (rect.anchorMin + rect.anchorMax) / 2f;
        Assert.That(centre.x, Is.EqualTo(0.483f).Within(Tolerance));
        Assert.That(centre.y, Is.EqualTo(0.480f).Within(Tolerance));
        SplatPalette.TrySymbolOf(new ColorId(colour), out var sprite, out var deep);
        var image = symbol.GetComponent<UnityEngine.UI.Image>();
        Assert.That(image.sprite, Is.SameAs(sprite));
        Assert.That(image.color, Is.EqualTo(deep));
    }

    [Test]
    public void AColourWithNoSymbol_GetsNone()
    {
        var slot = NewSlot();

        CellPainter.Paint(slot, Cell.Color(new ColorId("teal")), 40f);

        Assert.That(SymbolsIn(slot), Is.Empty);
    }

    [Test]
    public void AWildCell_HasASparkleInEachQuarterInThatQuartersDeepColour()
    {
        var slot = NewSlot();

        CellPainter.Paint(slot, Cell.Wild(), 0f);

        var deepColours = slot.Cast<Transform>()
            .Select(quarter => quarter.Find("Splat").Find(CellPainter.SymbolName).GetComponent<UnityEngine.UI.Image>())
            .Select(image => (image.sprite, image.color))
            .ToArray();
        Assert.That(deepColours.Select(d => d.sprite), Is.All.SameAs(Ui.SymbolSparkle));
        Assert.That(
            deepColours.Select(d => d.color),
            Is.EquivalentTo(new[] { SplatPalette.DeepRed, SplatPalette.DeepBlue, SplatPalette.DeepGreen, SplatPalette.DeepPurple }));
    }

    [Test]
    public void ACritter_HasNoSymbol()
    {
        var slot = NewSlot();

        CritterPainter.Paint(slot, 0);

        Assert.That(SymbolsIn(slot), Is.Empty);
    }

    [Test]
    public void ABlobbityCard_MarksEachOfItsSplatsWithTheColoursSymbol()
    {
        var slot = NewSlot();

        MissionCardPainter.Paint(slot, new Mission(new MissionId("m"), MissionPattern.Square, OrdinaryCatalog.Blue));

        var symbols = SymbolsIn(slot);
        Assert.That(symbols, Has.Length.EqualTo(4));
        Assert.That(symbols.Select(s => s.GetComponent<UnityEngine.UI.Image>().sprite), Is.All.SameAs(Ui.SymbolTriangle));
    }
}
}
