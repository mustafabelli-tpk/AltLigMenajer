using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AltLigMenajer.Models;

/// <summary>
/// Tracks scouting missions. A team can have up to 3 pending reports at once.
/// After CompletionDate is reached, IsCompleted flips to true and attributes are revealed.
/// </summary>
public class ScoutReport
{
    public int Id { get; set; }

    /// <summary>The team that initiated the scouting (the manager's team).</summary>
    public int TeamId { get; set; }

    /// <summary>The player being scouted.</summary>
    public int PlayerId { get; set; }

    /// <summary>The in-game date when the report will be ready.</summary>
    public DateTime CompletionDate { get; set; }

    /// <summary>True once the report is finished and attributes are revealed.</summary>
    public bool IsCompleted { get; set; }

    // Navigation
    [ForeignKey("TeamId")]
    public Team Team { get; set; } = null!;

    [ForeignKey("PlayerId")]
    public Player Player { get; set; } = null!;
}
