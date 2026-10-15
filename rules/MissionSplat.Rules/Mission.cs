namespace MissionSplat.Rules;

public enum BlobbityPattern
{
    Row,
    Square,
    L,
}

public sealed record Mission(BlobbityId Id, BlobbityPattern Pattern, ColorId Color);
