namespace MissionSplat.Player
{

using System;
using MissionSplat.App;
using UnityEngine;
using UnityEngine.UI;

internal sealed class TableSetupView
{
    private const float PanelOutlineUnits = 8f;
    private const float PanelDropUnits = 14f;

    // The panel's rows, top to bottom, in canvas units, so it is as tall as its seat rows need and sits in the middle of the screen.
    private const float PaddingUnits = 40f;
    private const float TitleUnits = 110f;
    private const float SectionGapUnits = 30f;
    private const float ChoiceRowUnits = 110f;
    private const float SeatRowUnits = 120f;
    private const float SeatRowGapUnits = 14f;
    private const float FooterUnits = 120f;

    // A chosen option is yellow, an unchosen one white; Start is a different colour so it reads as the way forward.
    private static readonly Color Chosen = SplatPalette.Sunny;
    private static readonly Color Unchosen = SplatPalette.Paper;

    private readonly RectTransform _root;
    private TableStart _draft;

    public event Action<TableStart> Started;

    public TableSetupView(RectTransform root)
    {
        _root = root;
    }

    public void Show(TableStart draft)
    {
        // The deck is always shuffled for a player, so a draft that was not is shown, and started, as shuffled.
        ReplaceDraft(draft.SeatCount, CopyFlags(draft), draft.FirstSeatIndex);
    }

    private void Rebuild()
    {
        Ui.Clear(_root);
        var seatRowsUnits = _draft.SeatCount * (SeatRowUnits + SeatRowGapUnits);
        var height = PaddingUnits + TitleUnits + SectionGapUnits + ChoiceRowUnits + SectionGapUnits + seatRowsUnits + SectionGapUnits + FooterUnits + PaddingUnits + PanelDropUnits;
        var panel = Ui.Toy("Panel", _root, SplatPalette.Paper, Ui.Rounded, PanelOutlineUnits, PanelDropUnits);
        Ui.Anchored(panel.rectTransform, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.5f), new Vector2(0f, -height / 2f), new Vector2(0f, height / 2f));
        var face = Ui.ToyFace(panel);

        var top = PaddingUnits;
        var title = Ui.Label("Title", face, 80, SplatPalette.Ink, TextAnchor.MiddleCenter, FontWeight.ExtraBold);
        Band(title.rectTransform, 0.05f, 0.95f, top, TitleUnits);
        title.text = "Mission Splat";
        top += TitleUnits + SectionGapUnits;
        PaintSeatCount(face, top);
        top += ChoiceRowUnits + SectionGapUnits;
        for (var i = 0; i < _draft.SeatCount; i++)
        {
            PaintSeatRow(face, i, top);
            top += SeatRowUnits + SeatRowGapUnits;
        }

        top += SectionGapUnits;
        PaintFooter(face, top);
    }

    private void PaintSeatCount(Transform face, float top)
    {
        var seats = Ui.Label("Seats", face, 40, SplatPalette.Ink, TextAnchor.MiddleLeft, FontWeight.SemiBold);
        Band(seats.rectTransform, 0.08f, 0.40f, top, ChoiceRowUnits);
        seats.text = "Seats";
        for (var count = 2; count <= 4; count++)
        {
            var chosen = count;
            var button = Ui.Button("Seats" + count, face, count.ToString(), chosen == _draft.SeatCount ? Chosen : Unchosen);
            var x = 0.42f + ((count - 2) * 0.16f);
            Band(button.GetComponent<RectTransform>(), x, x + 0.14f, top, ChoiceRowUnits);
            button.onClick.AddListener(() => ReplaceDraft(chosen, ClampFlags(chosen), Mathf.Min(_draft.FirstSeatIndex, chosen - 1)));
        }
    }

    private void PaintSeatRow(Transform face, int i, float top)
    {
        var critter = Ui.Rect("Critter" + i, face);
        Band(critter, 0.03f, 0.115f, top, SeatRowUnits);
        CritterPainter.PaintFitted(critter, i);
        var label = Ui.Label("Seat" + i, face, 36, SplatPalette.Ink, TextAnchor.MiddleLeft, FontWeight.SemiBold);
        Band(label.rectTransform, 0.125f, 0.29f, top, SeatRowUnits);
        label.text = Seats.NameOf(_draft.SeatAt(i));
        var human = Ui.Button("Human" + i, face, "Human", _draft.IsAi(i) ? Unchosen : Chosen);
        Band(human.GetComponent<RectTransform>(), 0.31f, 0.51f, top, SeatRowUnits);
        human.onClick.AddListener(() => SetAi(i, false));
        var ai = Ui.Button("Ai" + i, face, "AI", _draft.IsAi(i) ? Chosen : Unchosen);
        Band(ai.GetComponent<RectTransform>(), 0.52f, 0.72f, top, SeatRowUnits);
        ai.onClick.AddListener(() => SetAi(i, true));
        var first = Ui.Button("First" + i, face, "First", _draft.FirstSeatIndex == i ? Chosen : Unchosen);
        Band(first.GetComponent<RectTransform>(), 0.74f, 0.92f, top, SeatRowUnits);
        first.onClick.AddListener(() => ReplaceDraft(_draft.SeatCount, CopyFlags(_draft), i));
    }

    private void PaintFooter(Transform face, float top)
    {
        var start = Ui.Button("Start", face, "Start", SplatPalette.Go);
        Band(start.GetComponent<RectTransform>(), 0.08f, 0.92f, top, FooterUnits);
        start.onClick.AddListener(() => Started?.Invoke(_draft));
    }

    // A band of the panel's face: between two fractions of its width, height units tall, top units below its top edge.
    private static void Band(RectTransform rect, float minX, float maxX, float top, float height)
    {
        Ui.Anchored(rect, new Vector2(minX, 1f), new Vector2(maxX, 1f), new Vector2(0f, -(top + height)), new Vector2(0f, -top));
    }

    private void SetAi(int index, bool ai)
    {
        var flags = CopyFlags(_draft);
        flags[index] = ai;
        ReplaceDraft(_draft.SeatCount, flags, _draft.FirstSeatIndex);
    }

    private void ReplaceDraft(int seatCount, bool[] flags, int first)
    {
        _draft = new TableStart(seatCount, flags, first, shuffle: true);
        Rebuild();
    }

    private static bool[] CopyFlags(TableStart draft)
    {
        var flags = new bool[draft.SeatCount];
        for (var i = 0; i < flags.Length; i++)
        {
            flags[i] = draft.IsAi(i);
        }

        return flags;
    }

    private bool[] ClampFlags(int seatCount)
    {
        var flags = new bool[seatCount];
        for (var i = 0; i < seatCount; i++)
        {
            flags[i] = i < _draft.SeatCount && _draft.IsAi(i);
        }

        return flags;
    }
}
}
