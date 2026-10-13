namespace MissionSplat.Player
{

using System;
using UnityEngine;

// The Place and Cancel buttons that wait over the bottom edge of the board, just above the tray, while a placement is being previewed.
internal sealed class PlacementStrip
{
    // Each button is at least the 120-unit tap size (see BoardPanel); the strip leaves a little air round them.
    private const float HeightUnits = 150f;
    private const float EdgeUnits = 24f;
    private const float AirUnits = 10f;

    private readonly RectTransform _strip;

    public PlacementStrip(RectTransform play)
    {
        _strip = Ui.Rect("PlacementStrip", play);
        Ui.Anchored(_strip, Vector2.zero, new Vector2(1f, 0f), new Vector2(EdgeUnits, TurnTray.ReachUnits), new Vector2(-EdgeUnits, TurnTray.ReachUnits + HeightUnits));
        var cancel = Ui.Button("Cancel", _strip, "Cancel", SplatPalette.Paper, Ui.Rounded, 44);
        Ui.Anchored(cancel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0.38f, 1f), new Vector2(0f, AirUnits), new Vector2(0f, -AirUnits));
        cancel.onClick.AddListener(() => Cancelled?.Invoke());
        var place = Ui.Button("Place", _strip, "Place", SplatPalette.Go, Ui.Rounded, 60);
        Ui.Anchored(place.GetComponent<RectTransform>(), new Vector2(0.42f, 0f), Vector2.one, new Vector2(0f, AirUnits), new Vector2(0f, -AirUnits));
        place.onClick.AddListener(() => Placed?.Invoke());
        Show(false);
    }

    public event Action Placed;

    public event Action Cancelled;

    public void Show(bool shown) => _strip.gameObject.SetActive(shown);
}
}
