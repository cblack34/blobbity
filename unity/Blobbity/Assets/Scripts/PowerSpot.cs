namespace Blobbity.Player
{

using Blobbity.Rules;
using UnityEngine;

// Where a power's badge sits on screen, as the world corners of its rect, taken when the splash is queued so it holds no UI object.
internal readonly struct PowerSpot
{
    public PowerSpot(SymbolId power, Vector3 min, Vector3 max)
    {
        Power = power;
        Min = min;
        Max = max;
    }

    public SymbolId Power { get; }

    public Vector3 Min { get; }

    public Vector3 Max { get; }
}
}
