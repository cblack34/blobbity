namespace MissionSplat.Player
{

using System;
using System.Collections.Generic;
using MissionSplat.App;
using MissionSplat.Rules;
using UnityEngine;

// Forwards the view's input to the App table and draws its snapshot. It starts games, and builds the next game's start from the last
// (same seats, the first seat moved on); it never decides a move.
[DefaultExecutionOrder(-100)]
public sealed class TableSession : MonoBehaviour
{
    private Table _table;
    private TableStart _start;
    private TableView _view;
    private string _summary;

    public bool IsStarted => _table != null && _table.IsStarted;

    public TableSnapshot Snapshot => _table.Snapshot;

    private void Awake()
    {
        _view = GetComponent<TableView>();
        if (_view == null)
        {
            _view = gameObject.AddComponent<TableView>();
        }

        _view.PlaceRequested += Place;
        _view.Confirmed += ConfirmIncomingSeat;
        _view.QuarterTurnsSelected += SetQuarterTurns;
        _view.PowerRequested += Submit;
        _view.Started += Begin;
        _view.PlayAgainRequested += PlayAgain;
        _view.NewSetupRequested += NewSetup;
        _view.RecapFinished += FinishRecap;
    }

    // The shuffle seed is drawn here, at the edge, so the App table stays deterministic.
    public void Begin(TableStart start)
    {
        if (start is null)
        {
            throw new ArgumentNullException(nameof(start));
        }

        Adopt(start, new Table(new LocalSession(), start, start.Shuffle ? new System.Random().Next() : (int?)null));
    }

    public void Begin(TableStart start, GameSetup setup) => Adopt(start, new Table(new LocalSession(), start, setup));

    // The start the current game began from; New setup pre-fills from it.
    internal TableStart CurrentStart => _start;

    // Same seats and choices, the first seat moved on, and always shuffled so each game is a fresh deal,
    // even after a game that began unshuffled.
    public void PlayAgain()
    {
        if (_start is null)
        {
            return;
        }

        Begin(new TableStart(_start.SeatCount, _start.AiSeats, (_start.FirstSeatIndex + 1) % _start.SeatCount, shuffle: true));
    }

    // Setup comes back filled with the finished game's choices.
    public void NewSetup()
    {
        if (_start is not null)
        {
            _view.ShowSetup(_start);
        }
    }

    public void Place(int tileX, int tileY) => Commit(() => _table.Place(tileX, tileY));

    public void Submit(GameAction action) => Commit(() => _table.Submit(action));

    public void ConfirmIncomingSeat() => Commit(() => _table.Confirm());

    public void SetQuarterTurns(int quarterTurns) => Forward(() => _table.SetQuarterTurns(quarterTurns));

    private void Adopt(TableStart start, Table table)
    {
        table.Start();
        _table = table;
        _start = start;
        _summary = null;
        _view.RenderNewGame(_table.Snapshot);
    }

    private bool IsBlocked => !IsStarted || _view.Recap.IsPlaying;

    // The recap's line stays until the next thing the player does that moves the game on; Spin keeps it.
    private void Commit(Action act)
    {
        if (!IsBlocked)
        {
            _summary = null;
        }

        Forward(act);
    }

    // The view has played what the snapshot carried; this is the one place that acknowledges it, so the table counts it seen.
    private void FinishRecap(IReadOnlyList<RecapItem> shown)
    {
        _summary = RecapSummary.Of(shown);
        Forward(() => _table.AcknowledgeRecap());
    }

    // Input while a recap plays is dropped here too, not only by the view's skip layer, so the table cannot move under the recap.
    private void Forward(Action act)
    {
        if (IsBlocked)
        {
            return;
        }

        act();
        _view.Render(_table.Snapshot, _summary);
    }
}
}
