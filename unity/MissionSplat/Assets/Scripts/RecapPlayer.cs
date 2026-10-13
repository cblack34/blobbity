namespace MissionSplat.Player
{

using System;
using System.Collections.Generic;
using MissionSplat.App;

// Plays a snapshot's recap one item at a time on the stage and says when it is done. It does not acknowledge the recap: its owner does that.
// A tap (RecapStage.Skipped) finishes only the current item and goes on to the next.
internal sealed class RecapPlayer
{
    // Tests flip this so a recap is over the moment it starts and nothing is drawn, as if every item had been skipped before it was seen.
    // Playing for real is the default.
    internal static bool PlaysInstantly { get; set; }

    private readonly RecapStage _stage;
    private readonly RecapAnimations _animations;
    private TableSnapshot _snapshot;
    private RecapStep _step;
    private float _elapsedSeconds;

    public RecapPlayer(RecapStage stage)
    {
        _stage = stage;
        _animations = new RecapAnimations(stage);
        stage.Skipped += Skip;
    }

    // Raised with every item that was shown, once the last one ends (an instant recap shows none).
    public event Action<IReadOnlyList<RecapItem>> Finished;

    public bool IsPlaying => _snapshot != null;

    // The item on screen, from 0; the count of items played so far while a recap is playing.
    public int Index { get; private set; }

    public int Count => _snapshot?.Recap.Count ?? 0;

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

        _snapshot = snapshot;
        _stage.Open();
        Start(0);
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

        _step.Apply(_elapsedSeconds / _step.Seconds);
    }

    // Finishes the current item at once and starts the next; after the last, the recap is over.
    public void Skip()
    {
        if (!IsPlaying)
        {
            return;
        }

        _step.Apply(1f);
        _stage.Clear();
        if (Index + 1 < _snapshot.Recap.Count)
        {
            Start(Index + 1);
            return;
        }

        var shown = _snapshot.Recap;
        _snapshot = null;
        _stage.Close();
        Finished?.Invoke(shown);
    }

    // Drops what is playing without saying it finished, for a new game or the setup screen.
    public void Cancel()
    {
        if (!IsPlaying)
        {
            return;
        }

        _snapshot = null;
        _stage.Close();
    }

    private void Start(int index)
    {
        Index = index;
        _elapsedSeconds = 0f;
        _step = _animations.Begin(_snapshot, index);
        _step.Apply(0f);
    }
}
}
