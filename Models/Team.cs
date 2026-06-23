using System.ComponentModel.DataAnnotations;

namespace AltLigMenajer.Models;

public class Team
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Transfer budget in euros.</summary>
    public decimal Budget { get; set; }

    public int Points { get; set; }

    /// <summary>League tier: 1 = Süper Lig, 2 = TFF 1. Lig.</summary>
    public int LeagueTier { get; set; } = 2;

    /// <summary>Team's overall power rating used for player stat scaling.</summary>
    public int Ovr { get; set; } = 60;

    /// <summary>Minimum manager star rating required to select this team.</summary>
    public int RequiredStarRating { get; set; } = 1;

    [MaxLength(50)]
    public string TeamInstruction { get; set; } = "DENGELİ";

    [MaxLength(50)]
    public string PassingStyle { get; set; } = "KARIŞIK";

    [MaxLength(20)]
    public string Formation { get; set; } = "4-2-3-1";

    // Navigation
    public ICollection<Player> Players { get; set; } = new List<Player>();
    public Manager? Manager { get; set; }
}
