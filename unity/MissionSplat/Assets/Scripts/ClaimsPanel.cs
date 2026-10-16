namespace MissionSplat.Player
{

using System;
using MissionSplat.App;
using UnityEngine;
using UnityEngine.UI;

// A seat's claimed blobbities over a dimmed table. A tap anywhere closes it, and so does any render of the table
// (TablePlayView.Render calls Hide), because an action may have changed the claims it shows.
internal sealed class ClaimsPanel
{
    private const float PanelHeightUnits = 620f;
    private const float OutlineUnits = 8f;
    private const float DropUnits = 14f;

    private readonly RectTransform _root;

    public ClaimsPanel(RectTransform play)
    {
        _root = Ui.Rect("ClaimsPanel", play);
        Ui.Stretch(_root);
        _root.gameObject.SetActive(false);
    }

    public void Show(int seatIndex, SeatClaims claims)
    {
        Ui.Clear(_root);
        // Shown first: the cards measure their slots, and a slot under an inactive parent has no size to measure.
        _root.gameObject.SetActive(true);
        var dim = Ui.Tappable(Ui.Image("Dim", _root, SplatPalette.Overlay));
        Ui.Stretch(dim.GetComponent<RectTransform>());
        dim.onClick.AddListener(Hide);

        var panel = Ui.Toy("Panel", _root, SplatPalette.Paper, Ui.Rounded, OutlineUnits, DropUnits);
        PlacePanel(panel.rectTransform);
        var face = Ui.ToyFace(panel);

        var critterSlot = Ui.Rect("CritterSlot", face);
        Ui.Anchored(critterSlot, new Vector2(0.04f, 0.74f), new Vector2(0.16f, 0.97f), Vector2.zero, Vector2.zero);
        CritterPainter.PaintFitted(critterSlot, seatIndex);
        var title = Ui.Label("Title", face, 52, SplatPalette.Ink, TextAnchor.MiddleLeft, FontWeight.ExtraBold);
        Ui.Anchored(title.rectTransform, new Vector2(0.18f, 0.76f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero);
        title.text = Seats.NameOf(claims.Seat) + "\u2019s claims";

        var cards = Ui.Rect("Cards", face);
        Ui.Anchored(cards, new Vector2(0f, 0.04f), new Vector2(1f, 0.72f), Vector2.zero, Vector2.zero);
        if (claims.Blobbities.Count == 0)
        {
            var none = Ui.Label("None", cards, 44, SplatPalette.Muted, TextAnchor.MiddleCenter);
            Ui.Stretch(none.rectTransform);
            none.text = "No claims yet.";
        }

        for (var claimIndex = 0; claimIndex < claims.Blobbities.Count; claimIndex++)
        {
            var slot = Ui.Rect("Claim" + claimIndex, cards);
            var (min, max) = CardAnchors(claimIndex, claims.Blobbities.Count);
            Ui.Anchored(slot, min, max, Vector2.zero, Vector2.zero);
            BlobbityCardPainter.Paint(slot, claims.Blobbities[claimIndex]);
        }
    }

    public void Hide() => _root.gameObject.SetActive(false);

    private static void PlacePanel(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.03f, 0.5f);
        rect.anchorMax = new Vector2(0.97f, 0.5f);
        rect.offsetMin = new Vector2(0f, -PanelHeightUnits / 2f);
        rect.offsetMax = new Vector2(0f, PanelHeightUnits / 2f);
    }

    // A row holds the claims needed to win. One placement can claim both held blobbities, so a row one short of the win count can end one over it and must stay inside the panel.
    private static (Vector2 Min, Vector2 Max) CardAnchors(int index, int blobbityCount)
    {
        var slots = Math.Max(Seats.ClaimsToWin, blobbityCount);
        const float margin = 0.04f;
        const float gap = 0.02f;
        var width = (1f - (2f * margin) - ((slots - 1) * gap)) / slots;
        var left = margin + (index * (width + gap));
        return (new Vector2(left, 0.02f), new Vector2(left + width, 0.98f));
    }
}
}
