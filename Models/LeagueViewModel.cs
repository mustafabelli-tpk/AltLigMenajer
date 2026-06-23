namespace AltLigMenajer.Models;

/// <summary>View model for the League standings page.</summary>
public class LeagueViewModel
{
    public List<TeamStanding> Standings { get; set; } = new();
    public int CurrentWeek { get; set; }
    public int? ManagerTeamId { get; set; }
    public int SelectedTier { get; set; } = 1;
    public int UserTeamTier { get; set; } = 2;
}

/// <summary>Computed standings row for a single team.</summary>
public class TeamStanding
{
    public Team Team { get; set; } = null!;
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int GoalDifference => GoalsFor - GoalsAgainst;
    public int Points => Won * 3 + Drawn;
}

/// <summary>View model for the Fixtures tab.</summary>
public class FixtureWeekViewModel
{
    public int SelectedWeek { get; set; }
    public int TotalWeeks { get; set; } = 34;
    public List<Fixture> Matches { get; set; } = new();
    public int? ManagerTeamId { get; set; }
}

/// <summary>View model for the Krallık (top performers) tab.</summary>
public class StatsViewModel
{
    public string StatType { get; set; } = "goals"; // "goals" or "assists"
    public List<Player> TopPlayers { get; set; } = new();
}

/// <summary>View model for the read-only team profile.</summary>
public class TeamProfileViewModel
{
    public Team Team { get; set; } = null!;
    public List<Player> Players { get; set; } = new();
}
