using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AltLigMenajer.Models;

public class Manager
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Manager's coaching trait chosen during onboarding (e.g. Motivator, Tactician).</summary>
    [MaxLength(50)]
    public string CoachTrait { get; set; } = string.Empty;

    /// <summary>Reputation star rating (1-5). Determines which teams the manager can manage.</summary>
    public int StarRating { get; set; } = 1;

    /// <summary>Manager license level (1-5). Determines coaching tier label.</summary>
    public int LicenseLevel { get; set; } = 1;

    /// <summary>Experience points accumulated from match results and milestones.</summary>
    public int ExperiencePoints { get; set; } = 0;

    public int? ManagedTeamId { get; set; }

    [ForeignKey("ManagedTeamId")]
    public Team? ManagedTeam { get; set; }
}
