using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AltLigMenajer.Models;

public class Message
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Sender { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime DateSent { get; set; }
    public bool IsRead { get; set; }

    public int ManagerId { get; set; }

    [ForeignKey("ManagerId")]
    public Manager? Manager { get; set; }
}
