namespace MissionSplat.Player
{

using UnityEngine;
using UnityEngine.UI;

internal static class Ui
{
    // The canvas is laid out this many units wide on any screen (it matches width), so sizes in units are the same on every phone.
    public const float ReferenceWidthUnits = 1080f;

    public static Font Font { get; } = LoadFont();

    public static Sprite Pixel { get; } = WhitePixel();

    public static Sprite Circle { get; } = CircleSprite(256);

    // A white rounded rectangle that keeps its corner radius at any size (nine-sliced), for chips, boxes and panels.
    public static Sprite Rounded { get; } = RoundedSprite(128, 40);

    public static Sprite Splat { get; } = LoadSprite("Splat");

    // A white 128 px rounded square, tinted by the caller.
    public static Sprite Card { get; } = LoadSprite("TileCard");

    // A white 200 x 280 px rounded rectangle, 5:7 like a playing card.
    public static Sprite MissionCard { get; } = LoadSprite("MissionCard");

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    public static Image Image(string name, Transform parent, Color color, Sprite sprite = null)
    {
        var rect = Rect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite != null ? sprite : Pixel;
        image.type = image.sprite.border == Vector4.zero ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static Text Label(string name, Transform parent, int size, Color color, TextAnchor align)
    {
        var rect = Rect(name, parent);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = Font;
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    public static Button Button(string name, Transform parent, string caption, Color fill, Sprite sprite = null, int fontSize = 32)
    {
        var image = Image(name, parent, fill, sprite);
        var button = Tappable(image);
        var label = Label("Label", image.transform, fontSize, Color.white, TextAnchor.MiddleCenter);
        Stretch(label.rectTransform);
        label.text = caption;
        return button;
    }

    // Makes an image tappable: it takes raycasts, shows no transition, and is not reachable by keyboard navigation.
    public static Button Tappable(Image image)
    {
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        var nav = Navigation.defaultNavigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
        return button;
    }

    // A rounded box in the edge colour with a rounded face inset by outlineUnits; returns the edge, the face is its child "Face".
    public static Image Outlined(string name, Transform parent, Color edge, Color face, float outlineUnits)
    {
        var edgeImage = Image(name, parent, edge, Rounded);
        var faceImage = Image("Face", edgeImage.transform, face, Rounded);
        Anchored(faceImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(outlineUnits, outlineUnits), new Vector2(-outlineUnits, -outlineUnits));
        return edgeImage;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void Anchored(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    public static void PlaceCell(
        RectTransform rect,
        int cellX,
        int cellY,
        int cellsWide,
        int cellsHigh,
        int minX,
        int minY,
        float cell,
        float originX,
        float originY)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(cellsWide * cell, cellsHigh * cell);
        rect.anchoredPosition = new Vector2(originX + ((cellX - minX) * cell), originY + ((cellY - minY) * cell));
    }

    public static void Clear(Transform parent, int keepLeading = 0)
    {
        for (var i = parent.childCount - 1; i >= keepLeading; i--)
        {
            var child = parent.GetChild(i).gameObject;
            // Destroy waits until end of frame and would still take a click. DestroyImmediate is edit-mode only.
            child.SetActive(false);
            Object.Destroy(child);
        }
    }

    private static Font LoadFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        return Font.CreateDynamicFontFromOSFont("Helvetica", 16);
    }

    private static Sprite LoadSprite(string name)
    {
        var sprite = Resources.Load<Sprite>(name);
        if (sprite == null)
        {
            throw new System.InvalidOperationException("Missing sprite Resources/" + name + ". Run art/export.sh and import it as a Sprite.");
        }

        return sprite;
    }

    private static Sprite WhitePixel()
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private static Sprite CircleSprite(int size)
    {
        var half = size / 2f;
        return Bake(size, (x, y) => half - Mathf.Sqrt((((x + 0.5f) - half) * ((x + 0.5f) - half)) + (((y + 0.5f) - half) * ((y + 0.5f) - half))), Vector4.zero);
    }

    // A rounded square of the given corner radius, with nine-slice borders at the radius.
    private static Sprite RoundedSprite(int size, int radius)
    {
        return Bake(size, (x, y) =>
        {
            // Distance inside the shape: measured from the nearest corner centre when in a corner, else from the edge.
            var px = Mathf.Min(x + 0.5f, size - (x + 0.5f));
            var py = Mathf.Min(y + 0.5f, size - (y + 0.5f));
            if (px >= radius || py >= radius)
            {
                return Mathf.Min(px, py);
            }

            var dx = radius - px;
            var dy = radius - py;
            return radius - Mathf.Sqrt((dx * dx) + (dy * dy));
        }, new Vector4(radius, radius, radius, radius));
    }

    // Paints white where inside (distance from the edge, in pixels) is positive, with a one-pixel soft edge.
    private static Sprite Bake(int size, System.Func<int, int, float> inside, Vector4 border)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var alpha = Mathf.Clamp01(inside(x, y) + 0.5f);
                pixels[(y * size) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), border == Vector4.zero ? size : 100f, 0, SpriteMeshType.FullRect, border);
    }
}
}
