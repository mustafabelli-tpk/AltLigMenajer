namespace AltLigMenajer.Models;

/// <summary>
/// View model passed to the Tactic page so the Razor view has
/// everything it needs: team info, starters grouped by formation row,
/// and substitutes.
/// </summary>
public class TacticViewModel
{
    public Team Team { get; set; } = null!;
    public List<Player> Starters { get; set; } = new();
    public List<Player> Substitutes { get; set; } = new();

    /// <summary>
    /// Computed average OVR of all starters.
    /// </summary>
    public int AverageOvr =>
        Starters.Count > 0
            ? (int)Math.Round(Starters.Average(p => p.Ovr))
            : 0;
}

public class SaveTacticRequest
{
    public List<PlayerPositionDto> PlayerPositions { get; set; } = new();
    public string Instruction { get; set; } = "";
    public string PassingStyle { get; set; } = "";
    public string Formation { get; set; } = "4-2-3-1";
}

public class PlayerPositionDto
{
    public int PlayerId { get; set; }
    public int PitchPosition { get; set; }
}
