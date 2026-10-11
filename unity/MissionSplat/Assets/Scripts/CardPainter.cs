namespace MissionSplat.Player
{

using UnityEngine;

internal static class CardPainter
{
    // The thinnest the edge line and the shadow drop may be, in canvas units, so small cards keep them when the window is scaled down.
    private const float MinimumEdgeUnits = 3f;
    private const float MinimumShadowUnits = 2f;

    // The shadow's drop as a fraction of the card's height.
    private const float ShadowDropFraction = 0.02f;

    // The shadow, edge and face of a card, shared by tiles and mission cards. inset and edge are fractions of the block's width;
    // blockWidth is the width in canvas units that block will have, which sets the edge and shadow thickness in units.
    // The same fraction of the height is as many pixels on a portrait sprite, so the border stays even whatever the sprite's shape.
    public static void Paint(RectTransform block, Sprite sprite, float inset, float edge, float blockWidth)
    {
        var widthPerHeight = sprite.rect.width / sprite.rect.height;
        var edgeUnits = Mathf.Max(edge * blockWidth, MinimumEdgeUnits);
        var dropUnits = Mathf.Max(ShadowDropFraction * (blockWidth / widthPerHeight), MinimumShadowUnits);
        PaintLayer(block, "Shadow", SplatPalette.CardShadow, sprite, inset, widthPerHeight, new Vector2(0f, -dropUnits), Vector2.zero);
        PaintLayer(block, "Edge", SplatPalette.CardEdge, sprite, inset, widthPerHeight, Vector2.zero, Vector2.zero);
        PaintLayer(block, "Face", SplatPalette.CardFace, sprite, inset, widthPerHeight, Vector2.zero, new Vector2(edgeUnits, edgeUnits));
    }

    // One layer of the card: the block inset on every side, moved by shift and then shrunk by shrink units on every side.
    private static void PaintLayer(RectTransform block, string name, Color color, Sprite sprite, float inset, float widthPerHeight, Vector2 shift, Vector2 shrink)
    {
        var image = Ui.Image(name, block, color, sprite);
        var insetY = inset * widthPerHeight;
        Ui.Anchored(image.rectTransform, new Vector2(inset, insetY), new Vector2(1f - inset, 1f - insetY), shift + shrink, shift - shrink);
    }
}
}
