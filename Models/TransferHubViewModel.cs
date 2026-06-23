namespace AltLigMenajer.Models;

/// <summary>
/// View model for the Transfer Hub page.
/// </summary>
public class TransferHubViewModel
{
    public Team MyTeam { get; set; } = null!;
    public Manager Manager { get; set; } = null!;
    public List<Player> AvailablePlayers { get; set; } = new();
    public List<ScoutReport> ScoutReports { get; set; } = new();
    public int PendingScoutCount { get; set; }
    public string? ActiveFilter { get; set; }
    public string? SearchQuery { get; set; }
    public DateTime CurrentDate { get; set; }
}
