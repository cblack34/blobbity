namespace MissionSplat.Player
{

using MissionSplat.App;
using MissionSplat.Rules;

// Rotate reads the quarter-turns as its amount, so the view treats a selected rotate differently from placing. The questions live here once.
internal static class RotateAmount
{
    public static bool IsSelected(TableSnapshot snapshot) =>
        snapshot.SelectedPower is { } power && power.Equals(OrdinaryCatalog.Rotate);

    // Selected, but no amount chosen: Rules would refuse a rotate by zero, so its targets are not offered yet.
    public static bool IsPending(TableSnapshot snapshot) => IsSelected(snapshot) && snapshot.QuarterTurns == 0;
}
}
