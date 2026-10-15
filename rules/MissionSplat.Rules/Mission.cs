namespace MissionSplat.Rules;

public enum BlobbityPattern
{
    Row,
    Square,
    L,
}

public sealed record Mission(MissionId Id, BlobbityPattern Pattern, ColorId Color);
