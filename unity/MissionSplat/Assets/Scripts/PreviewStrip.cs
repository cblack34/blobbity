namespace MissionSplat.Player
{

using System;
using Blobbity.Rules;
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

    // Where each button sits across the strip, as fractions of its width. Cancel takes the left; the primary button takes the rest.
    private const float PlaceCancelRight = 0.38f;
    private const float PlaceLeft = 0.42f;
    private const float RotateCancelRight = 0.28f;
    private const float TurnLeft = 0.31f;
    private const float TurnRight = 0.58f;
    private const float RotateConfirmLeft = 0.62f;

    private readonly RectTransform _strip;

    public PreviewStrip(RectTransform play)
    {
        _strip = Ui.Rect("PreviewStrip", play);
        Ui.Anchored(_strip, Vector2.zero, new Vector2(1f, 0f), new Vector2(EdgeUnits, TurnTray.ReachUnits), new Vector2(-EdgeUnits, TurnTray.ReachUnits + HeightUnits));
        Show(null);
    }

    public event Action Placed;

    public event Action Turned;

    // Confirm on a rotate, or Bounce on a bounce.
    public event Action PowerConfirmed;

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
            case PreviewKind.Placement:
                AddCancel(PlaceCancelRight);
                AddButton("Place", "Place", SplatPalette.Go, PlaceLeft, 1f, 60, () => Placed?.Invoke());
                break;
            case PreviewKind.Rotate:
                AddCancel(RotateCancelRight);
                var turn = AddButton("Turn", string.Empty, SplatPalette.Sunny, TurnLeft, TurnRight, 0, () => Turned?.Invoke());
                PowerBadge.PaintMark(Ui.ToyFace(turn), OrdinaryCatalog.Rotate);
                Ui.SetEnabled(AddButton("RotateConfirm", "Confirm", SplatPalette.Go, RotateConfirmLeft, 1f, 52, () => PowerConfirmed?.Invoke()), preview.CanConfirm);
                break;
            case PreviewKind.Bounce:
                AddCancel(PlaceCancelRight);
                AddButton("Bounce", "Bounce", SplatPalette.BounceBadge, PlaceLeft, 1f, 60, () => PowerConfirmed?.Invoke());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(preview), preview.Kind, "Unknown kind of preview.");
        }
    }

    private void AddCancel(float right) => AddButton("Cancel", "Cancel", SplatPalette.Paper, 0f, right, 44, () => Cancelled?.Invoke());

    // A button across the strip from left to right (fractions of its width).
    private Button AddButton(string name, string caption, Color fill, float left, float right, int fontSize, Action pressed)
    {
        var button = Ui.Button(name, _strip, caption, fill, Ui.Rounded, fontSize);
        Ui.Anchored(button.GetComponent<RectTransform>(), new Vector2(left, 0f), new Vector2(right, 1f), new Vector2(0f, AirUnits), new Vector2(0f, -AirUnits));
        button.onClick.AddListener(() => pressed());
        return button;
    }
}
}
