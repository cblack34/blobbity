namespace MissionSplat.Player
{

using UnityEngine;
using UnityEngine.UI;

// Baloo 2 in the three weights the player uses: body text Regular, names SemiBold, headings and buttons ExtraBold.
internal enum FontWeight
{
    Regular,
    SemiBold,
    ExtraBold,
}

internal static class Ui
{
    // The canvas is laid out this many units wide on any screen (it matches width), so sizes in units are the same on every phone.
    public const float ReferenceWidthUnits = 1080f;

    // A toy box's default rim thickness and how far its hard shadow reaches down and right, in canvas units.
    private const float ToyOutlineUnits = 5f;
    private const float ToyDropUnits = 8f;

    // Where a toy box's face sits under its root, and what a disabled button's cover is called.
    private const string ToyFacePath = "Body/Face";
    private const string DisabledName = "Disabled";
    
    private static readonly Font RegularFont = LoadFont("Baloo2-Regular");
    private static readonly Font SemiBoldFont = LoadFont("Baloo2-SemiBold");
    private static readonly Font ExtraBoldFont = LoadFont("Baloo2-ExtraBold");

    public static Sprite Pixel { get; } = WhitePixel();

    public static Sprite Circle { get; } = CircleSprite(256);

    // A white rounded rectangle that keeps its corner radius at any size (nine-sliced), for buttons, boxes and panels.
    public static Sprite Rounded { get; } = RoundedSprite(128, 52);

    public static Sprite Splat { get; } = LoadSprite("Splat");

    // A white rounded square, tinted by the caller.
    public static Sprite Card { get; } = LoadSprite("TileCard");

    // A white rounded rectangle, 5:7 like a playing card.
    public static Sprite MissionCard { get; } = LoadSprite("MissionCard");

    // A dashed rounded square with a faint plus, its colours baked in: a place the drawn tile may go.
    public static Sprite LegalSpot { get; } = LoadSprite("LegalSpot");

    // A white rounded-square ring that hugs a tile card from outside, tinted by the caller. Nine-sliced, so its line keeps its thickness.
    public static Sprite TileRing { get; } = LoadSprite("TileRing");

    // The thickness of the ring's line in canvas units, which is its art's, because the sprite is nine-sliced at 1 pixel to 1 unit.
    public const float RingUnits = 8f;

    // The white rotate arrow, tinted by the caller: a power's mark on a badge, and the turn hint under Spin.
    public static Sprite RotateArrow { get; } = LoadSprite("RotateArrow");

    // The tray's wavy top edge, white below an ink line, to stretch across the screen.
    public static Sprite TrayWave { get; } = LoadSprite("TrayWave");

    public static Font FontOf(FontWeight weight) =>
        weight switch
        {
            FontWeight.Regular => RegularFont,
            FontWeight.SemiBold => SemiBoldFont,
            FontWeight.ExtraBold => ExtraBoldFont,
            _ => throw new System.ArgumentOutOfRangeException(nameof(weight), weight, "Unknown font weight."),
        };

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    // A rect of the given size centred on its parent's middle, pivoting there.
    public static RectTransform Centered(string name, Transform parent, Vector2 size)
    {
        var rect = Rect(name, parent);
        Center(rect, size);
        return rect;
    }

    public static void Center(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
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

    public static Text Label(string name, Transform parent, int size, Color color, TextAnchor align, FontWeight weight = FontWeight.Regular)
    {
        var rect = Rect(name, parent);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = FontOf(weight);
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    // A toy box: a hard ink shadow, an ink rim, and a face in fill, the same look as the tile cards. The returned root takes no taps
    // until made tappable, and placing it places all of it, shadow included. Put content on ToyFace(root). sprite shapes it (rounded by default).
    public static Image Toy(string name, Transform parent, Color fill, Sprite sprite = null, float outlineUnits = ToyOutlineUnits, float dropUnits = ToyDropUnits)
    {
        var shape = sprite != null ? sprite : Rounded;
        var root = Image(name, parent, Color.clear);
        var shadow = Image("Shadow", root.transform, SplatPalette.Ink, shape);
        Stretch(shadow.rectTransform);
        var body = Outlined("Body", root.transform, SplatPalette.Ink, fill, outlineUnits, shape);
        Anchored(body.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, dropUnits), new Vector2(-dropUnits, 0f));
        return root;
    }

    // The face of a toy box, or of a button made by Button: the place to draw its content.
    public static RectTransform ToyFace(Component toy) => (RectTransform)toy.transform.Find(ToyFacePath);

    // A chunky button: a toy box with the caption in ink on its face, and the whole rect taking the tap.
    public static Button Button(string name, Transform parent, string caption, Color fill, Sprite sprite = null, int fontSize = 36)
    {
        var toy = Toy(name, parent, fill, sprite);
        var button = Tappable(toy);
        var label = Label("Label", ToyFace(toy), fontSize, SplatPalette.Ink, TextAnchor.MiddleCenter, FontWeight.ExtraBold);
        Stretch(label.rectTransform);
        label.text = caption;
        return button;
    }

    // Turns a button on or off and shows it: a gray cover goes over its look (a toy button's face, or a plain button's first child), opaque
    // so nothing shows through it. Safe to call again with the other value.
    public static void SetEnabled(Button button, bool enabled)
    {
        button.interactable = enabled;
        var face = button.transform.Find(ToyFacePath);
        var host = face != null ? face : button.transform;
        var cover = host.Find(DisabledName);
        if (cover != null)
        {
            cover.gameObject.SetActive(!enabled);
            return;
        }

        if (enabled)
        {
            return;
        }

        if (face != null)
        {
            // First, so the caption and marks stay drawn over the gray.
            var grayed = Image(DisabledName, face, SplatPalette.Gray, face.GetComponent<Image>().sprite);
            Stretch(grayed.rectTransform);
            grayed.transform.SetAsFirstSibling();
        }
        else if (button.transform.childCount > 0)
        {
            var copy = Object.Instantiate(button.transform.GetChild(0).gameObject, button.transform);
            copy.name = DisabledName;
            copy.GetComponent<Image>().color = SplatPalette.Gray;
        }
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

    // A box in the edge colour with a face inset by outlineUnits, both in sprite (rounded by default); returns the edge, the face is its child "Face".
    public static Image Outlined(string name, Transform parent, Color edge, Color face, float outlineUnits, Sprite sprite = null)
    {
        var shape = sprite != null ? sprite : Rounded;
        var edgeImage = Image(name, parent, edge, shape);
        var faceImage = Image("Face", edgeImage.transform, face, shape);
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

    private static Font LoadFont(string name)
    {
        var font = Resources.Load<Font>("Fonts/" + name);
        if (font == null)
        {
            throw new System.InvalidOperationException("Missing font Resources/Fonts/" + name + ". Run art/fonts/instance.py and import the files.");
        }

        return font;
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
