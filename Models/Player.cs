using System.ComponentModel.DataAnnotations;

namespace AltLigMenajer.Models;

public class Player
{
    public int Id { get; set; }

    /// <summary>Foreign key to Team.</summary>
    public int TeamId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Position code (e.g. SNT, OOS, SLO, SAO, DOS, STP, SLB, SAB, KL).</summary>
    [Required, MaxLength(10)]
    public string Position { get; set; } = string.Empty;

    /// <summary>Overall rating 0-99.</summary>
    public int Ovr { get; set; }

    public int Age { get; set; }
    public int Potential { get; set; }
    
    [MaxLength(50)]
    public string Nationality { get; set; } = string.Empty;
    
    public int Value { get; set; }

    public bool IsTransferListed { get; set; }
    public bool IsLoanListed { get; set; }
    public decimal AskingPrice { get; set; }

    public int Wage { get; set; } = 500000;
    public DateTime ContractEndDate { get; set; } = new DateTime(2028, 6, 30);

    public int Fatigue { get; set; } = 0;

    /// <summary>Whether the player is currently injured and unavailable.</summary>
    public bool IsInjured { get; set; } = false;

    /// <summary>Date when the injury recovery ends (null if not injured).</summary>
    public DateTime? InjuryEndDate { get; set; }

    /// <summary>Total goals scored this season.</summary>
    public int Goals { get; set; } = 0;

    /// <summary>Total assists this season.</summary>
    public int Assists { get; set; } = 0;

    /// <summary>Is in the starting 11.</summary>
    public bool IsStarter { get; set; }

    /// <summary>Is on the substitute bench.</summary>
    public bool IsSubstitute { get; set; }

    /// <summary>Pitch row order for formation display (0 = top / striker, 4 = bottom / goalkeeper).</summary>
    public int FormationRow { get; set; }

    /// <summary>Exact visual index on the pitch (0-10).</summary>
    public int? PitchPosition { get; set; }

    // Navigation
    public Team Team { get; set; } = null!;
}
