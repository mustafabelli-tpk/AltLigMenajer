namespace AltLigMenajer.Models;

/// <summary>View model for the live Match Engine UI.</summary>
public class MatchViewModel
{
    public Fixture Fixture { get; set; } = null!;

    public Team HomeTeam { get; set; } = null!;
    public Team AwayTeam { get; set; } = null!;

    /// <summary>Home team starting 11.</summary>
    public List<Player> HomeStarters { get; set; } = new();

    /// <summary>Home team bench players.</summary>
    public List<Player> HomeBench { get; set; } = new();

    /// <summary>Away team starting 11.</summary>
    public List<Player> AwayStarters { get; set; } = new();

    /// <summary>Away team bench players.</summary>
    public List<Player> AwayBench { get; set; } = new();

    /// <summary>Average OVR of home starters.</summary>
    public int HomeOvr { get; set; }

    /// <summary>Average OVR of away starters.</summary>
    public int AwayOvr { get; set; }
}

/// <summary>POST body sent from the Match Engine JS when the match ends.</summary>
public class SaveMatchResultRequest
{
    public int FixtureId { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public List<int> HomeGoalScorerIds { get; set; } = new();
    public List<int> AwayGoalScorerIds { get; set; } = new();
    public List<int> HomeAssistIds { get; set; } = new();
    public List<int> AwayAssistIds { get; set; } = new();
}
