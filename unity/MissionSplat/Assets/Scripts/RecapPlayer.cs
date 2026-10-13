namespace MissionSplat.Player
{

using System;
using System.Collections.Generic;
using MissionSplat.App;
using UnityEngine;

// Plays a snapshot's recap one item at a time on the stage and says when it is done. It does not acknowledge the recap: its owner does that.
// A tap (RecapStage.Skipped) finishes only the current item and goes on to the next. An item that cannot be drawn is logged and waits out
// its time unseen, so a recap always reaches Finished and the table never stays blocked.
internal sealed class RecapPlayer
{
    // How long an item that failed to draw holds the recap.
    private const float FailedItemSeconds = 0.2f;

    private readonly RecapStage _stage;
    private TableSnapshot _snapshot;
    private RecapStep _step;
    private float _elapsedSeconds;

    public RecapPlayer(RecapStage stage)
    {
        _stage = stage;
        Begin = new RecapAnimations(stage).Begin;
        stage.Skipped += Skip;
    }

    // Raised with every item that was shown, once the last one ends (an instant recap shows none).
    public event Action<IReadOnlyList<RecapItem>> Finished;

    // Raised as each item starts, so the heading can say what is playing.
    public event Action<RecapItem> ItemStarted;

    // Tests set this so a recap is over the moment it starts and nothing is drawn, as if every item had been skipped before it was seen.
    // Finished then fires inside the render that carried the recap, so the acknowledge re-renders from within it.
    internal bool PlaysInstantly { get; set; }

    // How an item is drawn; tests swap it to make drawing fail.
    internal Func<TableSnapshot, int, RecapStep> Begin { get; set; }

    public bool IsPlaying => _snapshot != null;

    // The item on screen, from 0.
    public int Index { get; private set; }

    public int Count => _snapshot?.Recap.Count ?? 0;

    public RecapItem Current => _snapshot?.Recap[Index];

    // A recap already playing keeps playing: input is blocked, so a snapshot cannot change under it.
    public void Play(TableSnapshot snapshot)
    {
        if (IsPlaying || snapshot.Recap.Count == 0)
        {
            return;
        }

        if (PlaysInstantly)
        {
            Finished?.Invoke(Array.Empty<RecapItem>());
            return;
        }

        _stage.Open();
        Start(snapshot, 0);
        _snapshot = snapshot;
    }

    public void Tick(float unscaledSeconds)
    {
        if (!IsPlaying)
        {
            return;
        }

        _elapsedSeconds += unscaledSeconds;
        if (_elapsedSeconds >= _step.Seconds)
        {
            Skip();
            return;
        }

        Draw(_elapsedSeconds / _step.Seconds);
    }

    // Finishes the current item at once and starts the next; after the last, the recap is over.
    public void Skip()
    {
        if (!IsPlaying)
        {
            return;
        }

        Draw(1f);
        _stage.Clear();
        if (Index + 1 < _snapshot.Recap.Count)
        {
            Start(_snapshot, Index + 1);
            return;
        }

        var shown = _snapshot.Recap;
        End();
        Finished?.Invoke(shown);
    }

    // Drops what is playing without saying it finished, for a new game or the setup screen.
    public void Cancel()
    {
        if (IsPlaying)
        {
            End();
        }
    }

    private void End()
    {
        _snapshot = null;
        _step = null;
        _stage.Close();
    }

    private void Start(TableSnapshot snapshot, int index)
    {
        Index = index;
        _elapsedSeconds = 0f;
        try
        {
            _step = Begin(snapshot, index);
        }
        catch (Exception exception)
        {
            Failed(exception, FailedItemSeconds);
        }

        Draw(0f);
        ItemStarted?.Invoke(snapshot.Recap[index]);
    }

    private void Draw(float progress)
    {
        try
        {
            _step.Apply(progress);
        }
        catch (Exception exception)
        {
            Failed(exception, _step.Seconds);
        }
    }

    // Whatever the item had drawn is wiped, and it idles out its time.
    private void Failed(Exception exception, float seconds)
    {
        Debug.LogException(exception);
        _stage.Clear();
        _step = RecapStep.Idle(seconds);
    }
}
}
