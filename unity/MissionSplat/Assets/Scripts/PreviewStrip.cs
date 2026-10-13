namespace MissionSplat.Player
{

using System;
using UnityEngine;
using UnityEngine.UI;

// The buttons that wait over the bottom edge of the board, just above the tray, while something is being tried on it:
// Cancel and Place for a placement, Cancel, a turn button and Confirm for a rotate, Cancel and Bounce for a bounce.
internal sealed class PreviewStrip
{
    // Each button is at least the 120-unit tap size (see BoardPanel); the strip leaves a little air round them.
    public const float HeightUnits = 150f;
    private const float EdgeUnits = 24f;
    private const float AirUnits = 10f;

    // The turn button's arrow, as a fraction of the button's face on each side.
    private const float ArrowMargin = 0.14f;

    private readonly RectTransform _strip;

    public PreviewStrip(RectTransform play)
    {
        _strip = Ui.Rect("PreviewStrip", play);
        Ui.Anchored(_strip, Vector2.zero, new Vector2(1f, 0f), new Vector2(EdgeUnits, TurnTray.ReachUnits), new Vector2(-EdgeUnits, TurnTray.ReachUnits + HeightUnits));
        Show(null);
    }

    public event Action Placed;

    public event Action Turned;

    public event Action RotateConfirmed;

    public event Action Bounced;

    public event Action Cancelled;

    // The buttons for preview's kind, or no strip for null.
    public void Show(TilePreview preview)
    {
        Ui.Clear(_strip);
        _strip.gameObject.SetActive(preview != null);
        if (preview == null)
        {
            return;
        }

        switch (preview.Kind)
        {
            case PreviewKind.Rotate:
                AddCancel(0.28f);
                var turn = AddButton("Turn", string.Empty, SplatPalette.Sunny, 0.31f, 0.58f, 0, Turned);
                PaintArrow(Ui.ToyFace(turn));
                // A rotate by no turns is no rotate, so there is nothing yet to confirm.
                Ui.SetEnabled(AddButton("RotateConfirm", "Confirm", SplatPalette.Go, 0.62f, 1f, 52, RotateConfirmed), preview.Turns > 0);
                break;
            case PreviewKind.Bounce:
                AddCancel(0.38f);
                AddButton("Bounce", "Bounce", SplatPalette.BounceBadge, 0.42f, 1f, 60, Bounced);
                break;
            default:
                AddCancel(0.38f);
                AddButton("Place", "Place", SplatPalette.Go, 0.42f, 1f, 60, Placed);
                break;
        }
    }

    private void AddCancel(float right) => AddButton("Cancel", "Cancel", SplatPalette.Paper, 0f, right, 44, Cancelled);

    // A button across the strip from left to right (fractions of its width). The handler is read when the button is pressed, not now.
    private Button AddButton(string name, string caption, Color fill, float left, float right, int fontSize, Action pressed)
    {
        var button = Ui.Button(name, _strip, caption, fill, Ui.Rounded, fontSize);
        Ui.Anchored(button.GetComponent<RectTransform>(), new Vector2(left, 0f), new Vector2(right, 1f), new Vector2(0f, AirUnits), new Vector2(0f, -AirUnits));
        button.onClick.AddListener(() => pressed?.Invoke());
        return button;
    }

    private static void PaintArrow(RectTransform face)
    {
        var arrow = Ui.Image("Arrow", face, SplatPalette.Ink, Ui.RotateArrow);
        arrow.preserveAspect = true;
        Ui.Anchored(arrow.rectTransform, new Vector2(ArrowMargin, ArrowMargin), new Vector2(1f - ArrowMargin, 1f - ArrowMargin), Vector2.zero, Vector2.zero);
    }
}
}
