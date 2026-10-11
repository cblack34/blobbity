namespace MissionSplat.Player
{

using UnityEngine;

internal static class CardPainter
{
    // The shadow, edge and face of a card, shared by tiles and mission cards. inset and edge are fractions of the block's width.
    // The same fraction of the height is as many pixels on a portrait sprite, so the border stays even whatever the sprite's shape.
    public static void Paint(RectTransform block, Sprite sprite, float inset, float edge)
    {
        var widthPerHeight = sprite.rect.width / sprite.rect.height;
        PaintLayer(block, "Shadow", SplatPalette.CardShadow, sprite, inset, widthPerHeight, -0.02f);
        PaintLayer(block, "Edge", SplatPalette.CardEdge, sprite, inset, widthPerHeight, 0f);
        PaintLayer(block, "Face", SplatPalette.CardFace, sprite, inset + edge, widthPerHeight, 0f);
    }

    // One layer of the card: the block inset on every side, then moved up by liftFraction of the block's height.
    private static void PaintLayer(RectTransform block, string name, Color color, Sprite sprite, float inset, float widthPerHeight, float liftFraction)
    {
        var image = Ui.Image(name, block, color, sprite);
        var insetY = inset * widthPerHeight;
        Ui.Anchored(image.rectTransform, new Vector2(inset, insetY + liftFraction), new Vector2(1f - inset, 1f - insetY + liftFraction), Vector2.zero, Vector2.zero);
    }
}
}
