using AltLigMenajer.Models;

namespace AltLigMenajer.Data;

/// <summary>
/// Seeds the database with 36 teams across two tiers, auto-generates 20-man rosters,
/// and creates two separate 34-matchweek double round-robin fixture schedules.
/// Runs once on startup — skips if data already exists.
/// </summary>
public static class DbInitializer
{
    private static readonly string[] FirstNames =
    {
        "Ali", "Mehmet", "Mustafa", "Ahmet", "Emre", "Burak", "Arda", "Kerem", "Yusuf", "Mert",
        "Enes", "Hakan", "Serkan", "Oğuz", "Berkay", "Umut", "Batuhan", "Ferdi", "İsmail", "Onur",
        "Tolga", "Cenk", "Deniz", "Furkan", "Gökhan", "Halil", "İbrahim", "Kaan", "Ozan", "Selim",
        "Tarık", "Uğur", "Volkan", "Yasin", "Barış", "Can", "Doruk", "Erdem", "Güven", "Hamza",
        "Serdar", "Salih", "Rıdvan", "Necip", "Taylan", "Atakan", "Berke", "Çağlar", "Doğukan", "Emir"
    };

    private static readonly string[] LastNames =
    {
        "Yılmaz", "Kaya", "Demir", "Çelik", "Şahin", "Yıldız", "Aydın", "Özdemir", "Arslan", "Doğan",
        "Kılıç", "Aslan", "Çetin", "Koç", "Kurt", "Özkan", "Şimşek", "Korkmaz", "Yavuz", "Aktaş",
        "Polat", "Acar", "Tekin", "Güneş", "Ergin", "Başaran", "Kaplan", "Erdoğan", "Tunç", "Karaca",
        "Uçar", "Yalçın", "Bilgin", "Sönmez", "Aksoy", "Öztürk", "Karaman", "Bulut", "Taş", "Bayrak",
        "Gül", "Pekçetin", "Kurtaran", "Toprak", "Balcı", "Ekinci", "Dikmen", "Çakır", "Sarı", "Usta"
    };

    private static readonly string[] Nationalities =
    {
        "Türkiye", "Türkiye", "Türkiye", "Türkiye", "Türkiye", "Türkiye", "Türkiye", "Türkiye",
        "Brezilya", "Arjantin", "Nijerya", "Gana", "Senegal", "Kamerun", "Fransa", "Portekiz",
        "Hırvatistan", "Bosna-Hersek", "Sırbistan", "İran"
    };

    private static readonly (string Position, int Count)[] RosterTemplate =
    {
        ("KL",  2), ("STP", 3), ("SLB", 2), ("SAB", 2),
        ("DOS", 2), ("MO",  2), ("OOS", 1), ("SLO", 1), ("SAO", 1), ("SNT", 4),
    };

    public static void Seed(ApplicationDbContext db)
    {
        SeedTeams(db);
        SeedGameSettings(db);
        SeedFixtures(db);
    }

    private static void SeedTeams(ApplicationDbContext db)
    {
        if (db.Teams.Any()) return;

        var rand = new Random(42);

        // ── TIER 1: Süper Lig (18 teams, OVR 75-90) ──
        var tier1Teams = new List<(string Name, int Stars, int Ovr, decimal Budget)>
        {
            ("GALATASARAY",         5, 88, 65_000_000m),
            ("FENERBAHÇE",          5, 87, 60_000_000m),
            ("BEŞİKTAŞ",           5, 85, 50_000_000m),
            ("TRABZONSPOR",         4, 83, 35_000_000m),
            ("BAŞAKŞEHİR",         4, 81, 28_000_000m),
            ("ANTALYASPOR",         4, 80, 22_000_000m),
            ("KASIMPAŞA",           4, 79, 20_000_000m),
            ("ADANA DEMİRSPOR",     3, 78, 18_000_000m),
            ("SAMSUNSPOR",          3, 78, 18_000_000m),
            ("ANKARAGÜCÜ",          3, 77, 17_000_000m),
            ("HATAYSPOR",           3, 77, 16_000_000m),
            ("KAYSERİSPOR",         3, 76, 15_000_000m),
            ("SİVASSPOR",           3, 76, 15_000_000m),
            ("KONYASPOR",           3, 76, 15_000_000m),
            ("RİZESPOR",            3, 75, 15_000_000m),
            ("GAZİANTEP FK",        3, 75, 15_000_000m),
            ("ALANYASPOR",          3, 75, 15_000_000m),
            ("GÖZTEPE",             3, 75, 15_000_000m),
        };

        // ── TIER 2: TFF 1. Lig (18 teams, OVR 55-70) ──
        var tier2Teams = new List<(string Name, int Stars, int Ovr, decimal Budget)>
        {
            ("KOCAELİSPOR",         2, 70, 5_000_000m),
            ("SAKARYASPOR",         2, 69, 4_800_000m),
            ("EYÜPSPOR",            2, 68, 4_500_000m),
            ("KARAGÜMRÜK",          2, 67, 4_200_000m),
            ("ÜMRANİYESPOR",        2, 66, 4_000_000m),
            ("BOLUSPOR",            2, 65, 3_800_000m),
            ("MANİSASPOR",          2, 64, 3_500_000m),
            ("BANDIRMASPOR",        2, 63, 3_200_000m),
            ("PENDİKSPOR",          1, 62, 3_000_000m),
            ("KEÇİÖRENGÜCÜ",       1, 62, 3_000_000m),
            ("ALTAY",               1, 61, 2_800_000m),
            ("ALTINORDU",           1, 60, 2_500_000m),
            ("GİRESUNSPOR",         1, 59, 2_200_000m),
            ("ERZURUMSPOR",         1, 58, 2_000_000m),
            ("TUZLASPOR",           1, 58, 2_000_000m),
            ("SARIYER",             1, 57, 1_800_000m),
            ("ESENLER EROKSPOR",    1, 56, 1_500_000m),
            ("BAĞCILAR FK",         1, 55, 1_500_000m),
        };

        // Seed Tier 1
        foreach (var (name, stars, ovr, budget) in tier1Teams)
        {
            var team = new Team
            {
                Name = name,
                Ovr = ovr,
                RequiredStarRating = stars,
                Budget = budget,
                Points = 0,
                LeagueTier = 1
            };
            db.Teams.Add(team);
            db.SaveChanges();
            GenerateRoster(db, team, rand);
        }

        // Seed Tier 2
        foreach (var (name, stars, ovr, budget) in tier2Teams)
        {
            var team = new Team
            {
                Name = name,
                Ovr = ovr,
                RequiredStarRating = stars,
                Budget = budget,
                Points = 0,
                LeagueTier = 2
            };
            db.Teams.Add(team);
            db.SaveChanges();

            if (name == "BAĞCILAR FK")
                SeedBagcilarPlayers(db, team);
            else
                GenerateRoster(db, team, rand);
        }
    }

    private static void SeedBagcilarPlayers(ApplicationDbContext db, Team team)
    {
        var rand = new Random();
        var starters = new List<Player>
        {
            new() { TeamId = team.Id, Name = "C. Forlan",       Position = "SNT", Ovr = 70, Age = 28, Potential = 72, Nationality = "Uruguay", Value = 1500000, Wage = 187500, ContractEndDate = new DateTime(2026 + rand.Next(2, 5), 6, 30), IsStarter = true, FormationRow = 0 },
            new() { TeamId = team.Id, Name = "K. Aktürkoğlu",   Position = "SLO", Ovr = 64, Age = 24, Potential = 75, Nationality = "Türkiye", Value = 800000, Wage = 100000, ContractEndDate = new DateTime(2026 + rand.Next(2, 5), 6, 30), IsStarter = true, FormationRow = 1 },
            new() { TeamId = team.Id, Name = "Arda Güler",      Position = "OOS", Ovr = 68, Age = 20, Potential = 92, Nationality = "Türkiye", Value = 5000000, Wage = 625000, ContractEndDate = new DateTime(2026 + rand.Next(2, 5), 6, 30), IsStarter = true, FormationRow = 1 },
            new() { TeamId = team.Id, Name = "Y. Sarı",         Position = "SAO", Ovr = 62, Age = 25, Potential = 68, Nationality = "Türkiye", Value = 600000, Wage = 100000, ContractEndDate = new DateTime(2026 + rand.Next(2, 5), 6, 30), IsStarter = true, FormationRow = 1 },
            new() { TeamId = team.Id, Name = "Salih Ö.",        Position = "DOS", Ovr = 65, Age = 25, Potential = 74, Nationality = "Türkiye", Value = 900000, Wage = 112500, ContractEndDate = new DateTime(2026 + rand.Next(2, 5), 6, 30), IsStarter = true, FormationRow = 2 },
            new() { TeamId = team.Id, Name = "İ. Yüksek",       Position = "DOS", Ovr = 63, Age = 24, Potential = 72, Nationality = "Türkiye", Value = 750000, Wage = 100000, ContractEndDate = new DateTime(2026 + rand.Next(2, 5), 6, 30), IsStarter = true, FormationRow = 2 },
            new() { TeamId = team.Id, Name = "Ferdi K.",        Position = "SLB", Ovr = 61, Age = 23, Potential = 78, Nationality = "Türkiye", Value = 1200000, Wage = 150000, ContractEndDate = new DateTime(2026 + rand.Next(2, 5), 6, 30), IsStarter = true, FormationRow = 3 },
            new() { TeamId = team.Id, Name = "Samet A.",        Position = "STP", Ovr = 64, Age = 28, Potential = 65, Nationality = "Türkiye", Value = 500000, Wage = 100000, ContractEndDate = new DateTime(2026 + rand.Next(2, 5), 6, 30), IsStarter = true, FormationRow = 3 },
            new() { TeamId = team.Id, Name = "Merih D.",        Position = "STP", Ovr = 60, Age = 26, Potential = 70, Nationality = "Türkiye", Value = 800000, Wage = 100000, ContractEndDate = new DateTime(2026 + rand.Next(2, 5), 6, 30), IsStarter = true, FormationRow = 3 },
            new() { TeamId = team.Id, Name = "Zeki Ç.",         Position = "SAB", Ovr = 62, Age = 26, Potential = 68, Nationality = "Türkiye", Value = 600000, Wage = 100000, ContractEndDate = new DateTime(2026 + rand.Next(2, 5), 6, 30), IsStarter = true, FormationRow = 3 },
            new() { TeamId = team.Id, Name = "Ali Yılmaz",      Position = "KL",  Ovr = 66, Age = 22, Potential = 75, Nationality = "Türkiye", Value = 900000, Wage = 112500, ContractEndDate = new DateTime(2026 + rand.Next(2, 5), 6, 30), IsStarter = true, FormationRow = 4 },
        };

        var subs = new List<Player>
        {
            new() { TeamId = team.Id, Name = "M. Günok",      Position = "KL",  Ovr = 68, Age = 34, Potential = 68, Nationality = "Türkiye", Value = 300000, Wage = 100000, ContractEndDate = new DateTime(2026 + rand.Next(1, 4), 6, 30), IsSubstitute = true, FormationRow = -1 },
            new() { TeamId = team.Id, Name = "B. Yılmaz",     Position = "SNT", Ovr = 66, Age = 23, Potential = 76, Nationality = "Türkiye", Value = 1100000, Wage = 137500, ContractEndDate = new DateTime(2026 + rand.Next(1, 4), 6, 30), IsSubstitute = true, FormationRow = -1 },
            new() { TeamId = team.Id, Name = "C. Söyüncü",    Position = "STP", Ovr = 69, Age = 27, Potential = 74, Nationality = "Türkiye", Value = 1300000, Wage = 162500, ContractEndDate = new DateTime(2026 + rand.Next(1, 4), 6, 30), IsSubstitute = true, FormationRow = -1 },
            new() { TeamId = team.Id, Name = "E. Topçu",      Position = "MO",  Ovr = 58, Age = 21, Potential = 72, Nationality = "Türkiye", Value = 450000, Wage = 100000, ContractEndDate = new DateTime(2026 + rand.Next(1, 4), 6, 30), IsSubstitute = true, FormationRow = -1 },
            new() { TeamId = team.Id, Name = "R. Akbaba",     Position = "OOS", Ovr = 57, Age = 30, Potential = 57, Nationality = "Türkiye", Value = 300000, Wage = 100000, ContractEndDate = new DateTime(2026 + rand.Next(1, 4), 6, 30), IsSubstitute = true, FormationRow = -1 },
            new() { TeamId = team.Id, Name = "T. Antalyalı",  Position = "DOS", Ovr = 60, Age = 27, Potential = 62, Nationality = "Türkiye", Value = 500000, Wage = 100000, ContractEndDate = new DateTime(2026 + rand.Next(1, 4), 6, 30), IsSubstitute = true, FormationRow = -1 },
            new() { TeamId = team.Id, Name = "H. Bayraktar",  Position = "SAB", Ovr = 55, Age = 19, Potential = 74, Nationality = "Türkiye", Value = 350000, Wage = 100000, ContractEndDate = new DateTime(2026 + rand.Next(1, 4), 6, 30), IsSubstitute = true, FormationRow = -1 },
            new() { TeamId = team.Id, Name = "K. Demirbay",   Position = "SLO", Ovr = 59, Age = 22, Potential = 70, Nationality = "Türkiye", Value = 400000, Wage = 100000, ContractEndDate = new DateTime(2026 + rand.Next(1, 4), 6, 30), IsSubstitute = true, FormationRow = -1 },
            new() { TeamId = team.Id, Name = "A. Karaman",    Position = "SNT", Ovr = 56, Age = 20, Potential = 71, Nationality = "Türkiye", Value = 380000, Wage = 100000, ContractEndDate = new DateTime(2026 + rand.Next(1, 4), 6, 30), IsSubstitute = true, FormationRow = -1 },
        };

        db.Players.AddRange(starters);
        db.Players.AddRange(subs);
        db.SaveChanges();
    }

    private static void GenerateRoster(ApplicationDbContext db, Team team, Random rand)
    {
        var players = new List<Player>();

        foreach (var (position, count) in RosterTemplate)
        {
            for (int i = 0; i < count; i++)
            {
                var firstName = FirstNames[rand.Next(FirstNames.Length)];
                var lastName = LastNames[rand.Next(LastNames.Length)];
                var fullName = $"{firstName[0]}. {lastName}";

                int playerOvr = Math.Clamp(team.Ovr + rand.Next(-6, 6), 40, 95);

                int age = rand.Next(100) switch
                {
                    < 20 => rand.Next(17, 21),
                    < 75 => rand.Next(21, 29),
                    _    => rand.Next(29, 36),
                };

                int potential = age switch
                {
                    <= 20 => Math.Clamp(playerOvr + rand.Next(8, 20), playerOvr, 95),
                    <= 25 => Math.Clamp(playerOvr + rand.Next(3, 12), playerOvr, 95),
                    _     => Math.Clamp(playerOvr + rand.Next(0, 4), playerOvr, 95),
                };

                int value = playerOvr switch
                {
                    >= 80 => rand.Next(8_000_000, 25_000_000),
                    >= 70 => rand.Next(2_000_000, 8_000_000),
                    >= 60 => rand.Next(400_000, 2_000_000),
                    _     => rand.Next(100_000, 500_000),
                };

                int wage = Math.Max(value / 8, 100_000);
                var nationality = Nationalities[rand.Next(Nationalities.Length)];

                players.Add(new Player
                {
                    TeamId = team.Id,
                    Name = fullName,
                    Position = position,
                    Ovr = playerOvr,
                    Age = age,
                    Potential = potential,
                    Nationality = nationality,
                    Value = value,
                    Wage = wage,
                    ContractEndDate = new DateTime(2026 + rand.Next(1, 6), 6, 30),
                    IsStarter = false,
                    IsSubstitute = true,
                    FormationRow = -1,
                    Fatigue = 0,
                    Goals = 0,
                    Assists = 0
                });
            }
        }

        db.Players.AddRange(players);
        db.SaveChanges();
    }

    private static void SeedGameSettings(ApplicationDbContext db)
    {
        if (!db.GameSettings.Any())
        {
            db.GameSettings.Add(new GameSetting { CurrentDate = new DateTime(2026, 6, 30) });
            db.SaveChanges();
        }
    }

    /// <summary>
    /// Generates TWO separate 34-matchweek double round-robin schedules:
    /// one for Tier 1 (Süper Lig) and one for Tier 2 (TFF 1. Lig).
    /// Uses the standard circle/rotation method.
    /// </summary>
    private static void SeedFixtures(ApplicationDbContext db)
    {
        if (db.Fixtures.Any()) return;

        var allTeams = db.Teams.OrderBy(t => t.Id).ToList();

        var tier1Teams = allTeams.Where(t => t.LeagueTier == 1).ToList();
        var tier2Teams = allTeams.Where(t => t.LeagueTier == 2).ToList();

        var fixtures = new List<Fixture>();

        // Generate fixtures for each tier independently
        GenerateLeagueFixtures(tier1Teams, 1, fixtures);
        GenerateLeagueFixtures(tier2Teams, 2, fixtures);

        db.Fixtures.AddRange(fixtures);
        db.SaveChanges();
    }

    private static void GenerateLeagueFixtures(List<Team> teams, int leagueTier, List<Fixture> fixtures)
    {
        int n = teams.Count; // 18
        if (n < 2) return;

        var startDate = new DateTime(2026, 9, 15); // Season starts Sept 15

        // Match time slots (hour, minute) to distribute across weekend
        var timeSlots = new (int hour, int dayOffset)[]
        {
            (20, 0), // Friday 20:00
            (14, 1), // Saturday 14:00
            (17, 1), // Saturday 17:00
            (19, 1), // Saturday 19:00
            (14, 2), // Sunday 14:00
            (16, 2), // Sunday 16:00
            (18, 2), // Sunday 18:00
            (20, 2), // Sunday 20:00
            (15, 1), // Saturday 15:00
        };

        // Circle method: fix team[0], rotate the rest
        var teamList = teams.ToList();
        var rotating = teamList.Skip(1).ToList(); // n-1 = 17 teams rotate

        for (int round = 0; round < n - 1; round++) // 17 rounds for first leg
        {
            int matchweek = round + 1;
            var weekStartDate = startDate.AddDays(round * 7);

            // Build pairings for this round
            var roundTeams = new List<Team> { teamList[0] };
            roundTeams.AddRange(rotating);

            for (int match = 0; match < n / 2; match++) // 9 matches per round
            {
                var home = roundTeams[match];
                var away = roundTeams[n - 1 - match];

                // Alternate home/away for team[0] to balance
                if (match == 0 && round % 2 == 1)
                    (home, away) = (away, home);

                var slot = timeSlots[match % timeSlots.Length];
                var matchDate = weekStartDate.AddDays(slot.dayOffset)
                    .AddHours(slot.hour);

                // First leg
                fixtures.Add(new Fixture
                {
                    HomeTeamId = home.Id,
                    AwayTeamId = away.Id,
                    MatchDate = matchDate,
                    Matchweek = matchweek,
                    LeagueTier = leagueTier
                });

                // Second leg (matchweek + 17)
                var returnWeekDate = startDate.AddDays((round + 17) * 7);
                var returnDate = returnWeekDate.AddDays(slot.dayOffset)
                    .AddHours(slot.hour);

                fixtures.Add(new Fixture
                {
                    HomeTeamId = away.Id,
                    AwayTeamId = home.Id,
                    MatchDate = returnDate,
                    Matchweek = matchweek + 17,
                    LeagueTier = leagueTier
                });
            }

            // Rotate: move last element to first position of rotating list
            var last = rotating[^1];
            rotating.RemoveAt(rotating.Count - 1);
            rotating.Insert(0, last);
        }
    }
}
