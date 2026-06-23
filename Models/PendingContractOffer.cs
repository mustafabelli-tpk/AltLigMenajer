using System;
using System.ComponentModel.DataAnnotations;

namespace AltLigMenajer.Models;

public class PendingContractOffer
{
    public int Id { get; set; }
    
    public int PlayerId { get; set; }
    public int ManagerId { get; set; }
    
    public int OfferedWage { get; set; }
    public int OfferedYears { get; set; }
    
    public int DaysUntilResponse { get; set; }
}
