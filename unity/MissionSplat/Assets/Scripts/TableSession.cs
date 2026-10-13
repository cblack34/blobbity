namespace MissionSplat.Player
{

using System;
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

    public bool IsStarted => _table != null && _table.IsStarted;

    public TableSnapshot Snapshot => _table.Snapshot;

    private void Awake()
    {
        _view = GetComponent<TableView>();
        if (_view == null)
        {
            _view = gameObject.AddComponent<TableView>();
        }

        _view.Tapped += Tap;
        _view.Confirmed += ConfirmIncomingSeat;
        _view.QuarterTurnsSelected += SetQuarterTurns;
        _view.PowerSelected += SelectPower;
        _view.TargetTapped += UseAt;
        _view.Started += Begin;
        _view.PlayAgainRequested += PlayAgain;
        _view.NewSetupRequested += NewSetup;
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

    // Same seats and choices, the first seat moved on. Every game started from setup is shuffled, so each one is a fresh deal.
    public void PlayAgain()
    {
        if (_start is null)
        {
            return;
        }

        Begin(new TableStart(_start.SeatCount, _start.AiSeats, (_start.FirstSeatIndex + 1) % _start.SeatCount, _start.Shuffle));
    }

    // Setup comes back filled with the finished game's choices.
    public void NewSetup()
    {
        if (_start is not null)
        {
            _view.ShowSetup(_start);
        }
    }

    public void Tap(int tileX, int tileY) => Forward(() => _table.Place(tileX, tileY));

    public void SelectPower(SymbolId? power) => Forward(() => _table.SelectPower(power));

    public void UseAt(int tileX, int tileY) => Forward(() => _table.UseAt(tileX, tileY));

    public void Submit(GameAction action) => Forward(() => _table.Submit(action));

    public void ConfirmIncomingSeat() => Forward(() => _table.Confirm());

    public void SetQuarterTurns(int quarterTurns) => Forward(() => _table.SetQuarterTurns(quarterTurns));

    private void Adopt(TableStart start, Table table)
    {
        table.Start();
        _table = table;
        _start = start;
        _view.RenderNewGame(_table.Snapshot);
    }

    private void Forward(Action act)
    {
        if (!IsStarted)
        {
            return;
        }

        act();
        _view.Render(_table.Snapshot);
    }
}
}
