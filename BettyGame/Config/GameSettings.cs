namespace BettyGame.Config;

public class GameSettings
{
    public decimal MinBet { get; set; }
    public decimal MaxBet { get; set; }
    public double LossChance { get; set; }
    public double WinX2Chance { get; set; }
    public double WinX2ToX10Chance { get; set; }
}