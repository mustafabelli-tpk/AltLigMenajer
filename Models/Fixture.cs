using System.ComponentModel.DataAnnotations.Schema;

namespace AltLigMenajer.Models;

public class Fixture
{
    public int Id { get; set; }

    public int HomeTeamId { get; set; }
    [ForeignKey("HomeTeamId")]
    public Team? HomeTeam { get; set; }

    public int AwayTeamId { get; set; }
    [ForeignKey("AwayTeamId")]
    public Team? AwayTeam { get; set; }

    public DateTime MatchDate { get; set; }

    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }

    /// <summary>Which matchweek this fixture belongs to (1-34).</summary>
    public int Matchweek { get; set; }

    public bool IsPlayed { get; set; }

    /// <summary>League tier this fixture belongs to (1 = Süper Lig, 2 = TFF 1. Lig).</summary>
    public int LeagueTier { get; set; } = 2;
}
