namespace MissionSplat.Rules;

public enum BlobbityPattern
{
    Row,
    Square,
    L,
}

public sealed record BlobbityCard(BlobbityId Id, BlobbityPattern Pattern, ColorId Color);
