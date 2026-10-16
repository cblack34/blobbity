namespace MissionSplat.Player
{

using UnityEngine;

internal static class CardPainter
{
    // The thinnest the edge line and the shadow's reach may be, in canvas units, so small cards keep them when the window is scaled down.
    private const float MinimumEdgeUnits = 3f;
    private const float MinimumShadowUnits = 3f;

    // The thickest they get, so a big card (the drawn tile) keeps the line weight of a small one, as the design reference does.
    private const float MaximumEdgeUnits = 8.3f;
    private const float MaximumShadowRightUnits = 11f;
    private const float MaximumShadowDownUnits = 14f;

    // How far the hard shadow reaches right and down, as fractions of the card's width.
    private const float ShadowRightFraction = 0.04f;
    private const float ShadowDownFraction = 0.05f;

    // The gap kept round a card, as a fraction of the block's width: more than the shadow reaches (the larger fraction above),
    // so the shadow stays inside the block and neighbours never touch it.
    public const float InsetFraction = 0.06f;

    // The shadow, edge and face of a card, shared by tiles and blobbity cards: a white face in a thick ink edge over a hard ink shadow that
    // reaches down and right. edge is a fraction of the block's width; blockWidth is the width in canvas units that block will have,
    // which sets the edge and shadow thickness in units. The same fraction of the height is as many pixels on a portrait sprite,
    // so the border stays even whatever the sprite's shape.
    public static void Paint(RectTransform block, Sprite sprite, float edge, float blockWidth)
    {
        var inset = InsetFraction;
        var widthPerHeight = sprite.rect.width / sprite.rect.height;
        var edgeUnits = EdgeUnits(edge, blockWidth);
        var reachRight = Mathf.Clamp(ShadowRightFraction * blockWidth, MinimumShadowUnits, MaximumShadowRightUnits);
        var reachDown = Mathf.Clamp(ShadowDownFraction * blockWidth, MinimumShadowUnits, MaximumShadowDownUnits);
        PaintLayer(block, "Shadow", SplatPalette.CardEdge, sprite, inset, widthPerHeight, new Vector2(reachRight, -reachDown), Vector2.zero);
        PaintLayer(block, "Edge", SplatPalette.CardEdge, sprite, inset, widthPerHeight, Vector2.zero, Vector2.zero);
        PaintLayer(block, "Face", SplatPalette.CardFace, sprite, inset, widthPerHeight, Vector2.zero, new Vector2(edgeUnits, edgeUnits));
    }

    // The edge line's thickness in canvas units: edge is a fraction of the block's width, kept between the minimum and the maximum.
    public static float EdgeUnits(float edge, float blockWidth) => Mathf.Clamp(edge * blockWidth, MinimumEdgeUnits, MaximumEdgeUnits);

    // The gap kept round a card, in canvas units.
    public static float InsetUnits(float blockWidth) => InsetFraction * blockWidth;

    // One layer of the card: the block inset on every side, moved by shift and then shrunk by shrink units on every side.
    private static void PaintLayer(RectTransform block, string name, Color color, Sprite sprite, float inset, float widthPerHeight, Vector2 shift, Vector2 shrink)
    {
        var image = Ui.Image(name, block, color, sprite);
        var insetY = inset * widthPerHeight;
        Ui.Anchored(image.rectTransform, new Vector2(inset, insetY), new Vector2(1f - inset, 1f - insetY), shift + shrink, shift - shrink);
    }
}
}
