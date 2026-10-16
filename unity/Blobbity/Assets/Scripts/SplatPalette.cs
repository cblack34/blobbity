namespace Blobbity.Player
{

using System;
using Blobbity.Rules;
using UnityEngine;

public static class SplatPalette
{
    // The page, and the ink every outline, shadow and text is drawn in.
    public static readonly Color Background = Hex("f2f7ff");
    public static readonly Color Ink = Hex("1d1d1b");
    public static readonly Color Muted = Hex("4a5b6b");

    // The white of paper, panels, cards and plain buttons.
    public static readonly Color Paper = Color.white;
    public static readonly Color CardFace = Paper;
    public static readonly Color CardEdge = Ink;
    public static readonly Color Red = Hex("e03131");
    public static readonly Color Blue = Hex("1c7ed6");
    public static readonly Color Green = Hex("2f9e44");
    public static readonly Color Purple = Hex("9c36b5");
    public static readonly Color Gray = Hex("adb5bd");

    // The mark on each colour is drawn in a deeper shade of it: the hue's OKLCH lightness minus 0.16, chroma kept, so it reads as a darker
    // red, blue and so on and not as off-black. Computed once offline; recompute if the hue changes.
    public static readonly Color DeepRed = Hex("9a0110");
    public static readonly Color DeepBlue = Hex("04508e");
    public static readonly Color DeepGreen = Hex("006a21");
    public static readonly Color DeepPurple = Hex("66007b");

    // Outlines drawn around a tile: a stack target (a tile the drawn tile would cover) and a power target.
    public static readonly Color StackRing = Hex("2f9bd8");
    public static readonly Color TargetRing = Hex("fb8500");
    public static readonly Color Overlay = new Color(0.11f, 0.11f, 0.11f, 0.78f);

    // Buttons and badges: the power badges on tiles, the strip's Bounce, and Start, the way forward.
    public static readonly Color Sunny = Hex("ffd166");
    public static readonly Color Go = Hex("8fe3a0");
    public static readonly Color RotateBadge = Sunny;
    public static readonly Color StackBadge = Hex("8ecae6");
    public static readonly Color BounceBadge = Hex("ffafcc");

    // Critters are coloured by seat order and never by a game colour, so a seat is not mistaken for a splat colour.
    private static readonly Color[] Critters = { Hex("ffb703"), Hex("fb8500"), Hex("2ec4b6"), Hex("ff70a6") };

    public static Color CritterOf(int seatIndex) => Critters[seatIndex % Critters.Length];

    public static Color Of(ColorId color)
    {
        switch (color.Value)
        {
            case "red":
                return Red;
            case "blue":
                return Blue;
            case "green":
                return Green;
            case "purple":
                return Purple;
            default:
                return HashColor(color.Value);
        }
    }

    public static Color Hex(string rgb)
    {
        var value = Convert.ToInt32(rgb, 16);
        return new Color(
            ((value >> 16) & 255) / 255f,
            ((value >> 8) & 255) / 255f,
            (value & 255) / 255f,
            1f);
    }

    private static Color HashColor(string id)
    {
        unchecked
        {
            var hash = 23;
            for (var i = 0; i < id.Length; i++)
            {
                hash = (hash * 31) + id[i];
            }

            return Color.HSVToRGB((hash & 255) / 255f, 0.55f, 0.8f);
        }
    }
}
}
