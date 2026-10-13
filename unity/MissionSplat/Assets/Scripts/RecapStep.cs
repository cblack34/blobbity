namespace MissionSplat.Player
{

using System;

// One recap item being shown: how long it lasts and how to draw it at progress 0..1. Apply(1) is the finished picture.
internal sealed class RecapStep
{
    public RecapStep(float seconds, Action<float> apply)
    {
        Seconds = seconds;
        Apply = apply;
    }

    // Nothing drawn, for the time an item would take.
    public static RecapStep Idle(float seconds) => new RecapStep(seconds, _ => { });

    public float Seconds { get; }

    public Action<float> Apply { get; }
}
}
