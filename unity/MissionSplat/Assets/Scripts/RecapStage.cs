namespace MissionSplat.Player
{

using System;
using MissionSplat.Rules;
using UnityEngine;
using UnityEngine.UI;

// Where a recap is drawn: a layer over the whole table that also takes every tap (so nothing underneath can be reached and a tap skips),
// with a board layer clipped to the board's view and a screen layer for what flies across the table.
internal sealed class RecapStage
{
    private readonly RectTransform _root;
    private readonly RectTransform _boardLayer;
    private readonly RectTransform _screenLayer;
    private readonly BoardPanel _board;
    private readonly SeatBar _seatBar;

    public RecapStage(RectTransform play, RectTransform boardView, BoardPanel board, SeatBar seatBar)
    {
        _board = board;
        _seatBar = seatBar;
        _root = Ui.Rect("RecapOverlay", play);
        Ui.Stretch(_root);
        var blocker = Ui.Tappable(Ui.Image("RecapSkip", _root, Color.clear));
        Ui.Stretch(blocker.GetComponent<RectTransform>());
        blocker.onClick.AddListener(() => Skipped?.Invoke());
        // Same place on the table as the board, so a tile drawn dropping in does not show outside it.
        _boardLayer = Ui.Rect("RecapBoard", _root);
        Ui.Anchored(_boardLayer, boardView.anchorMin, boardView.anchorMax, boardView.offsetMin, boardView.offsetMax);
        _boardLayer.gameObject.AddComponent<RectMask2D>();
        _screenLayer = Ui.Rect("RecapScreen", _root);
        Ui.Stretch(_screenLayer);
        _root.gameObject.SetActive(false);
    }

    public event Action Skipped;

    public RectTransform ScreenLayer => _screenLayer;

    public RectTransform BoardLayer => _boardLayer;

    // Shown last, so it is over everything else the play view drew.
    public void Open()
    {
        _root.gameObject.SetActive(true);
        _root.SetAsLastSibling();
    }

    public void Close()
    {
        Clear();
        _root.gameObject.SetActive(false);
    }

    public void Clear()
    {
        Ui.Clear(_boardLayer);
        Ui.Clear(_screenLayer);
    }

    // A block one tile wide on the board layer, centred on where the board draws the tile at (tileX, tileY); null when the board draws nothing.
    // The board scrolls first if the tile is out of view, so the animation is seen.
    public RectTransform TileBlock(string name, int tileX, int tileY)
    {
        Canvas.ForceUpdateCanvases();
        _board.RevealTile(tileX, tileY);
        Canvas.ForceUpdateCanvases();
        if (_board.TileSlot(tileX, tileY) is not { } slot)
        {
            return null;
        }

        var block = Ui.Centered(name, _boardLayer, new Vector2(slot.Side, slot.Side));
        block.position = slot.Centre;
        return block;
    }

    // Where a claimed mission lands: its seat-bar pip, in the screen layer's space; null when the bar does not draw it.
    public Vector2? PipPosition(SeatId seat, BlobbityId mission)
    {
        var pip = _seatBar.PipOf(seat, mission);
        return pip == null ? null : (Vector2)_screenLayer.InverseTransformPoint(pip.position);
    }

    // Where a rect given by its world corners sits in the screen layer's space: its centre, and its width.
    public (Vector2 Centre, float Size) ScreenSpan(Vector3 min, Vector3 max)
    {
        Vector2 low = _screenLayer.InverseTransformPoint(min);
        Vector2 high = _screenLayer.InverseTransformPoint(max);
        return ((low + high) / 2f, Mathf.Abs(high.x - low.x));
    }
}
}
