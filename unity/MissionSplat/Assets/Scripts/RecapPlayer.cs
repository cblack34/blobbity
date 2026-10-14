namespace MissionSplat.Player
{

using System;
using System.Collections.Generic;
using MissionSplat.App;
using UnityEngine;

// Plays a snapshot's recap, or the power splashes of a drawn tile, one item at a time on the stage and says when it is done. It does not
// acknowledge the recap: its owner does that. The two share this one player so a single tap and a single "is something playing" cover both.
// A tap (RecapStage.Skipped) finishes only the current item and goes on to the next. An item that cannot be drawn is logged and waits out
// its time unseen, so what plays always ends and the table never stays blocked.
internal sealed class RecapPlayer
{
    // How long an item that failed to draw holds the recap.
    private const float FailedItemSeconds = 0.2f;

    private readonly RecapStage _stage;
    private readonly SplashAnimation _splash;
    private Reel _reel;
    private RecapStep _step;
    private float _elapsedSeconds;

    public RecapPlayer(RecapStage stage)
    {
        _stage = stage;
        _splash = new SplashAnimation(stage);
        Begin = new RecapAnimations(stage).Begin;
        stage.Skipped += Skip;
    }

    // Raised with every item that was shown, once the last one ends (an instant recap shows none).
    public event Action<IReadOnlyList<RecapItem>> Finished;

    // Raised once the last splash ends or is skipped (instant play shows none).
    public event Action SplashesFinished;

    // Raised with each item's caption as it starts, so the heading can say what is playing.
    public event Action<string> ItemStarted;

    // Tests set this so a recap is over the moment it starts and nothing is drawn, as if every item had been skipped before it was seen.
    // Finished then fires inside the render that carried the recap, so the acknowledge re-renders from within it.
    internal bool PlaysInstantly { get; set; }

    // How an item is drawn; tests swap it to make drawing fail.
    internal Func<TableSnapshot, int, RecapStep> Begin { get; set; }

    public bool IsPlaying => _reel != null;

    // The item on screen, from 0.
    public int Index { get; private set; }

    public int Count => _reel?.Count ?? 0;

    // What the heading says of the item on screen; null when nothing plays.
    public string Caption => _reel?.Caption(Index);

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

        var shown = snapshot.Recap;
        Start(new Reel(shown.Count, index => Begin(snapshot, index), index => RecapSummary.Caption(shown[index]), () => Finished?.Invoke(shown)));
    }

    // One splash per spot, for the drawn tile's powers. Nothing plays if something already does.
    public void PlaySplashes(IReadOnlyList<PowerSpot> spots)
    {
        if (IsPlaying || spots.Count == 0 || PlaysInstantly)
        {
            return;
        }

        Start(new Reel(spots.Count, index => _splash.Begin(spots[index]), index => SplashAnimation.Caption(spots[index].Power), () => SplashesFinished?.Invoke()));
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
        if (Index + 1 < _reel.Count)
        {
            StartItem(Index + 1);
            return;
        }

        var done = _reel.Done;
        End();
        done();
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
        _reel = null;
        _step = null;
        _stage.Close();
    }

    private void Start(Reel reel)
    {
        _stage.Open();
        _reel = reel;
        StartItem(0);
    }

    private void StartItem(int index)
    {
        Index = index;
        _elapsedSeconds = 0f;
        try
        {
            _step = _reel.Begin(index);
        }
        catch (Exception exception)
        {
            Failed(exception, FailedItemSeconds);
        }

        Draw(0f);
        ItemStarted?.Invoke(_reel.Caption(index));
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

    // What is playing: how many items, how each is drawn and captioned, and what happens once the last one ends.
    private sealed class Reel
    {
        public Reel(int count, Func<int, RecapStep> begin, Func<int, string> caption, Action done)
        {
            Count = count;
            Begin = begin;
            Caption = caption;
            Done = done;
        }

        public int Count { get; }

        public Func<int, RecapStep> Begin { get; }

        public Func<int, string> Caption { get; }

        public Action Done { get; }
    }
}
}
