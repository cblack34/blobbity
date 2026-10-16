namespace Blobbity.Player
{

using System.Collections.Generic;
using Blobbity.Rules;
using UnityEngine;

// A power's badge, on a tile cell: a coloured disc with an ink rim and the power's mark in ink
// (rotate yellow with its arrow, stack blue with +, bounce pink with -).
internal static class PowerBadge
{
    private enum Mark
    {
        Arrow,
        Plus,
        Minus,
        Initial,
    }

    private readonly struct Look
    {
        public Look(Color fill, Mark mark)
        {
            Fill = fill;
            Mark = mark;
        }

        public Color Fill { get; }

        public Mark Mark { get; }
    }

    // Each power's look, in one place. A symbol with no entry gets a neutral gray badge showing its initial.
    private static readonly Dictionary<string, Look> Looks = new()
    {
        [OrdinaryCatalog.Rotate.Value] = new Look(SplatPalette.RotateBadge, Mark.Arrow),
        [OrdinaryCatalog.Stack.Value] = new Look(SplatPalette.StackBadge, Mark.Plus),
        [OrdinaryCatalog.Bounce.Value] = new Look(SplatPalette.BounceBadge, Mark.Minus),
    };

    private static readonly Look Neutral = new(SplatPalette.Gray, Mark.Initial);

    // The rim's share of the badge's width on each side.
    private const float RimFraction = 0.07f;

    // The arrow's margin inside the face, as a fraction of it.
    private const float ArrowMargin = 0.1f;

    // A bar of the plus or minus: its length and thickness as fractions of the face.
    private const float BarLength = 0.56f;
    private const float BarThickness = 0.13f;

    // True for the symbols that have a badge of their own: rotate, stack, and bounce.
    public static bool IsPower(SymbolId symbol) => Looks.ContainsKey(symbol.Value);

    private static Color FillOf(SymbolId power) => LookOf(power).Fill;

    // Draws the whole badge filling parent, which should be square. A spent one has a gray face.
    public static void Paint(RectTransform parent, SymbolId power, bool spent = false)
    {
        var rim = Ui.Image("Rim", parent, SplatPalette.Ink, Ui.Circle);
        Ui.Stretch(rim.rectTransform);
        var face = Ui.Image("Face", rim.transform, spent ? SplatPalette.Gray : FillOf(power), Ui.Circle);
        Ui.Anchored(face.rectTransform, new Vector2(RimFraction, RimFraction), new Vector2(1f - RimFraction, 1f - RimFraction), Vector2.zero, Vector2.zero);
        PaintMark(face.rectTransform, power);
    }

    // The power's mark on its own, in ink, centred in face: the rotate arrow, or a plus or minus drawn as bars
    // (the glyphs of a font would sit off-centre in a disc). The preview strip's turn button draws the rotate arrow with it.
    public static void PaintMark(RectTransform face, SymbolId power)
    {
        switch (LookOf(power).Mark)
        {
            case Mark.Arrow:
                var arrow = Ui.Image("Rotate", face, SplatPalette.Ink, Ui.RotateArrow);
                arrow.preserveAspect = true;
                Ui.Anchored(arrow.rectTransform, new Vector2(ArrowMargin, ArrowMargin), new Vector2(1f - ArrowMargin, 1f - ArrowMargin), Vector2.zero, Vector2.zero);
                break;
            case Mark.Plus:
                PaintBar(face, BarLength, BarThickness);
                PaintBar(face, BarThickness, BarLength);
                break;
            case Mark.Minus:
                PaintBar(face, BarLength, BarThickness);
                break;
            default:
                var letter = Ui.Label("Mark", face, 40, SplatPalette.Ink, TextAnchor.MiddleCenter, FontWeight.ExtraBold);
                letter.resizeTextForBestFit = true;
                letter.resizeTextMinSize = 1;
                letter.resizeTextMaxSize = 200;
                Ui.Stretch(letter.rectTransform);
                letter.text = power.Value.Substring(0, 1).ToUpperInvariant();
                break;
        }
    }

    private static Look LookOf(SymbolId power) => Looks.TryGetValue(power.Value, out var look) ? look : Neutral;

    // A bar centred in the face, width and height as fractions of it.
    private static void PaintBar(RectTransform face, float width, float height)
    {
        var bar = Ui.Image("Bar", face, SplatPalette.Ink);
        Ui.Anchored(bar.rectTransform, new Vector2(0.5f - (width / 2f), 0.5f - (height / 2f)), new Vector2(0.5f + (width / 2f), 0.5f + (height / 2f)), Vector2.zero, Vector2.zero);
    }
}
}
