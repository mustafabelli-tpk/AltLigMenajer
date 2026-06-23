namespace AltLigMenajer.Helpers;

/// <summary>
/// Provides currency formatting and license label utilities.
/// </summary>
public static class CurrencyHelper
{
    public const decimal TlRate = 35m;

    /// <summary>Format a Euro-denominated amount according to the user's currency preference.</summary>
    public static string FormatMoney(decimal euroAmount, string currency)
    {
        if (currency == "TL")
        {
            var tlAmount = euroAmount * TlRate;
            return $"{tlAmount:N0} ₺";
        }
        return $"{euroAmount:N0} €";
    }

    /// <summary>Read the currency preference from the request cookies (defaults to EUR).</summary>
    public static string GetCurrency(HttpContext ctx)
    {
        return ctx.Request.Cookies["CurrencyPreference"] ?? "EUR";
    }

    /// <summary>Check if the transfer window is open on the given date.</summary>
    /// <remarks>
    /// Summer window: June 30 – September 15.
    /// Winter window: January 1 – January 31.
    /// </remarks>
    public static bool IsTransferWindowOpen(DateTime date)
    {
        // Summer window: June 30 to September 15
        var summerOpen = new DateTime(date.Year, 6, 30);
        var summerClose = new DateTime(date.Year, 9, 15);

        // Winter window: January 1 to January 31
        var winterOpen = new DateTime(date.Year, 1, 1);
        var winterClose = new DateTime(date.Year, 1, 31);

        return (date.Date >= summerOpen && date.Date <= summerClose)
            || (date.Date >= winterOpen && date.Date <= winterClose);
    }

    /// <summary>Map a position code to its position group for slot validation.</summary>
    /// <returns>GK, DEF, MID, or ATT</returns>
    public static string GetPositionGroup(string position)
    {
        return position switch
        {
            "KL" => "GK",
            "STP" or "SLB" or "SAB" => "DEF",
            "DOS" or "MO" or "OOS" => "MID",
            "SLO" or "SAO" or "SNT" => "ATT",
            _ => "MID" // fallback
        };
    }

    /// <summary>Get allowed position group for a pitch slot index in the given formation.</summary>
    public static string GetSlotPositionGroup(int slotIndex, string formation)
    {
        // Slot groups per formation (slot 0 to 10)
        return formation switch
        {
            "4-2-3-1" => slotIndex switch
            {
                0 => "ATT",           // SNT
                1 or 2 or 3 => "ATT", // AMs / wings
                4 or 5 => "MID",      // CDMs
                6 or 7 or 8 or 9 => "DEF", // DEFs
                10 => "GK",           // GK
                _ => "MID"
            },
            "4-3-3" => slotIndex switch
            {
                0 or 1 or 2 => "ATT",
                3 or 4 or 5 => "MID",
                6 or 7 or 8 or 9 => "DEF",
                10 => "GK",
                _ => "MID"
            },
            "4-4-2" => slotIndex switch
            {
                0 or 1 => "ATT",
                2 or 3 or 4 or 5 => "MID",
                6 or 7 or 8 or 9 => "DEF",
                10 => "GK",
                _ => "MID"
            },
            "3-5-2" => slotIndex switch
            {
                0 or 1 => "ATT",
                2 or 3 or 4 or 5 or 6 => "MID",
                7 or 8 or 9 => "DEF",
                10 => "GK",
                _ => "MID"
            },
            "5-3-2" => slotIndex switch
            {
                0 or 1 => "ATT",
                2 or 3 or 4 => "MID",
                5 or 6 or 7 or 8 or 9 => "DEF",
                10 => "GK",
                _ => "MID"
            },
            _ => "MID"
        };
    }

    /// <summary>Get the Turkish label for a manager license level.</summary>
    public static string GetLicenseLabel(int level) => level switch
    {
        1 => "C Lisans",
        2 => "B Lisans",
        3 => "A Lisans",
        4 => "Pro Lisans",
        5 => "UEFA Pro",
        _ => "C Lisans"
    };

    /// <summary>Get the XP threshold required to reach a given level.</summary>
    public static int GetXpThreshold(int level) => level switch
    {
        2 => 200,
        3 => 600,
        4 => 1500,
        5 => 4000,
        _ => int.MaxValue // Already max level
    };

    /// <summary>Calculate XP progress percentage toward the next level (0-100).</summary>
    public static int GetXpProgressPercent(int currentXp, int currentLevel)
    {
        if (currentLevel >= 5) return 100;

        int currentThreshold = currentLevel > 1 ? GetXpThreshold(currentLevel) : 0;
        int nextThreshold = GetXpThreshold(currentLevel + 1);
        int range = nextThreshold - currentThreshold;
        int progress = currentXp - currentThreshold;

        return range > 0 ? Math.Clamp((int)((double)progress / range * 100), 0, 100) : 0;
    }

    /// <summary>Check and apply level-up if XP exceeds threshold.</summary>
    public static bool TryLevelUp(AltLigMenajer.Models.Manager manager)
    {
        bool leveledUp = false;
        while (manager.LicenseLevel < 5)
        {
            int threshold = GetXpThreshold(manager.LicenseLevel + 1);
            if (manager.ExperiencePoints >= threshold)
            {
                manager.LicenseLevel++;
                leveledUp = true;
            }
            else break;
        }
        return leveledUp;
    }
}
