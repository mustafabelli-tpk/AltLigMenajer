using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AltLigMenajer.Models;

/// <summary>An incoming transfer offer from an AI team for a user's player.</summary>
public class TransferOffer
{
    public int Id { get; set; }

    public int PlayerId { get; set; }
    [ForeignKey("PlayerId")]
    public Player? Player { get; set; }

    public int OfferingTeamId { get; set; }
    [ForeignKey("OfferingTeamId")]
    public Team? OfferingTeam { get; set; }

    /// <summary>The fee offered in euros.</summary>
    public decimal OfferedFee { get; set; }

    /// <summary>Status: Pending, Accepted, Rejected.</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime DateOffered { get; set; }

    /// <summary>Manager who owns the player.</summary>
    public int ManagerId { get; set; }
}
