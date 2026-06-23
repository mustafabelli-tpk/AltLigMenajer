using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AltLigMenajer.Models;
using AltLigMenajer.Helpers;

namespace AltLigMenajer.Controllers;

public class HomeController : Controller
{
    private readonly AltLigMenajer.Data.ApplicationDbContext _db;

    public HomeController(AltLigMenajer.Data.ApplicationDbContext db) => _db = db;

    public IActionResult Index()
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        var teamId = manager?.ManagedTeamId;
        var team = _db.Teams.FirstOrDefault(t => t.Id == teamId);

        var gameSetting = _db.GameSettings.FirstOrDefault();
        var currentDate = gameSetting?.CurrentDate ?? new DateTime(2025, 6, 30);

        var unreadMessagesCount = _db.Messages.Count(m => m.ManagerId == managerId && !m.IsRead);

        var upcomingFixtures = _db.Fixtures
            .Include(f => f.HomeTeam)
            .Include(f => f.AwayTeam)
            .Where(f => (f.HomeTeamId == teamId || f.AwayTeamId == teamId) && f.MatchDate >= currentDate)
            .OrderBy(f => f.MatchDate)
            .Take(3)
            .ToList();

        var recentMessages = _db.Messages
            .Where(m => m.ManagerId == managerId)
            .OrderByDescending(m => m.DateSent)
            .Take(3)
            .ToList();

        // ── Form Guide: last 5 played matches ──
        var formResults = new List<string>();
        if (teamId != null)
        {
            var last5 = _db.Fixtures
                .Where(f => f.IsPlayed && (f.HomeTeamId == teamId || f.AwayTeamId == teamId))
                .OrderByDescending(f => f.MatchDate)
                .Take(5)
                .ToList();

            foreach (var f in last5)
            {
                bool isHome = f.HomeTeamId == teamId;
                int myScore = isHome ? (f.HomeScore ?? 0) : (f.AwayScore ?? 0);
                int oppScore = isHome ? (f.AwayScore ?? 0) : (f.HomeScore ?? 0);

                if (myScore > oppScore) formResults.Add("G");
                else if (myScore == oppScore) formResults.Add("B");
                else formResults.Add("M");
            }
        }

        // ── Currency ──
        var currency = CurrencyHelper.GetCurrency(HttpContext);

        // ── Manager XP/License ──
        var licenseLabel = manager != null ? CurrencyHelper.GetLicenseLabel(manager.LicenseLevel) : "C Lisans";
        var xpProgress = manager != null ? CurrencyHelper.GetXpProgressPercent(manager.ExperiencePoints, manager.LicenseLevel) : 0;
        var xpCurrent = manager?.ExperiencePoints ?? 0;
        var xpNextThreshold = manager != null && manager.LicenseLevel < 5 ? CurrencyHelper.GetXpThreshold(manager.LicenseLevel + 1) : 0;

        ViewBag.CurrentDate = currentDate;
        ViewBag.UnreadMessagesCount = unreadMessagesCount;
        ViewBag.Team = team;
        ViewBag.UpcomingFixtures = upcomingFixtures;
        ViewBag.Manager = manager;
        ViewBag.RecentMessages = recentMessages;
        ViewBag.FormResults = formResults;
        ViewBag.Currency = currency;
        ViewBag.LicenseLabel = licenseLabel;
        ViewBag.XpProgress = xpProgress;
        ViewBag.XpCurrent = xpCurrent;
        ViewBag.XpNextThreshold = xpNextThreshold;
        ViewBag.IsTransferWindowOpen = CurrencyHelper.IsTransferWindowOpen(currentDate);

        return View();
    }

    [HttpPost]
    public IActionResult AdvanceDay()
    {
        var gameSetting = _db.GameSettings.FirstOrDefault();
        if (gameSetting == null) return RedirectToAction("Index");

        var currentDate = gameSetting.CurrentDate;
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        if (manager == null) return RedirectToAction("Index");
        var teamId = manager.ManagedTeamId;

        // Check if there's a match today for the managed team
        var matchToday = _db.Fixtures.FirstOrDefault(f => (f.HomeTeamId == teamId || f.AwayTeamId == teamId) && f.MatchDate.Date == currentDate.Date && !f.IsPlayed);
        if (matchToday != null)
        {
            TempData["Notification"] = "Bugün maç gününüz! İlerleyemezsiniz, maça çıkmalısınız.";
            return RedirectToAction("Index");
        }

        // Advance day
        gameSetting.CurrentDate = currentDate.AddDays(1);
        _db.SaveChanges();
        currentDate = gameSetting.CurrentDate;

        // ── Recover injured players whose recovery date has passed ──
        var recoveredPlayers = _db.Players
            .Where(p => p.IsInjured && p.InjuryEndDate != null && p.InjuryEndDate <= currentDate)
            .ToList();
        foreach (var p in recoveredPlayers)
        {
            p.IsInjured = false;
            p.InjuryEndDate = null;

            // Notify user for their team's players
            if (p.TeamId == teamId)
            {
                _db.Messages.Add(new AltLigMenajer.Models.Message
                {
                    ManagerId = manager.Id,
                    Sender = "Sağlık Ekibi",
                    Subject = "Oyuncu İyileşti!",
                    Content = $"{p.Name} sakatlığını atlattı ve artık kadroya dahil edilebilir.",
                    DateSent = currentDate,
                    IsRead = false
                });
            }
        }

        // Daily events
        var rand = new Random();

        // Resolve Pending Contract Offers
        var pendingOffers = _db.PendingContractOffers.Where(o => o.ManagerId == manager.Id).ToList();
        foreach (var offer in pendingOffers)
        {
            offer.DaysUntilResponse--;
            if (offer.DaysUntilResponse <= 0)
            {
                var player = _db.Players.FirstOrDefault(p => p.Id == offer.PlayerId);
                if (player != null)
                {
                    if (offer.OfferedWage >= player.Wage)
                    {
                        player.Wage = offer.OfferedWage;
                        player.ContractEndDate = player.ContractEndDate.AddYears(offer.OfferedYears);
                        
                        _db.Messages.Add(new AltLigMenajer.Models.Message
                        {
                            ManagerId = manager.Id,
                            Sender = player.Name,
                            Subject = "Sözleşme Kabul Edildi",
                            Content = $"Önerdiğiniz yeni sözleşmeyi kabul ediyorum. Teşekkürler patron!",
                            DateSent = currentDate,
                            IsRead = false
                        });
                    }
                    else
                    {
                        _db.Messages.Add(new AltLigMenajer.Models.Message
                        {
                            ManagerId = manager.Id,
                            Sender = player.Name,
                            Subject = "Sözleşme Reddedildi",
                            Content = $"Şu anki maaşımdan daha düşük bir teklifi kabul etmem mümkün değil.",
                            DateSent = currentDate,
                            IsRead = false
                        });
                    }
                }
                _db.PendingContractOffers.Remove(offer);
            }
        }

        // AI Transfer Offers (replace auto-sell)
        var listedPlayers = _db.Players.Where(p => p.TeamId == teamId && p.IsTransferListed).ToList();
        foreach (var p in listedPlayers)
        {
            if (rand.Next(100) < 10) // 10% chance per day an AI team bids
            {
                // Pick a random AI team that isn't the user's team
                var aiTeams = _db.Teams.Where(team => team.Id != teamId).ToList();
                if (aiTeams.Count == 0) continue;
                var biddingTeam = aiTeams[rand.Next(aiTeams.Count)];

                // Check if there's already a pending offer for this player
                var existingOffer = _db.TransferOffers.FirstOrDefault(o => o.PlayerId == p.Id && o.Status == "Pending");
                if (existingOffer != null) continue;

                // Offer between 80-120% of asking price
                var offerMultiplier = 0.8m + (decimal)(rand.NextDouble() * 0.4);
                var offeredFee = Math.Round(p.AskingPrice * offerMultiplier, 0);

                _db.TransferOffers.Add(new AltLigMenajer.Models.TransferOffer
                {
                    PlayerId = p.Id,
                    OfferingTeamId = biddingTeam.Id,
                    OfferedFee = offeredFee,
                    Status = "Pending",
                    DateOffered = currentDate,
                    ManagerId = manager.Id
                });

                _db.Messages.Add(new AltLigMenajer.Models.Message
                {
                    ManagerId = manager.Id,
                    Sender = biddingTeam.Name,
                    Subject = "Transfer Teklifi!",
                    Content = $"{biddingTeam.Name}, {p.Name} ({p.Position}, OVR {p.Ovr}) için {offeredFee:N0} € teklif ediyor! Transfer Hub'dan değerlendirin.",
                    DateSent = currentDate,
                    IsRead = false
                });
            }
        }

        // Complete Scout Reports
        var completedReports = _db.ScoutReports
            .Where(r => r.TeamId == teamId && !r.IsCompleted && r.CompletionDate <= currentDate)
            .ToList();
        foreach (var report in completedReports)
        {
            report.IsCompleted = true;
            var scoutedPlayer = _db.Players.FirstOrDefault(p => p.Id == report.PlayerId);
            if (scoutedPlayer != null)
            {
                _db.Messages.Add(new AltLigMenajer.Models.Message
                {
                    ManagerId = manager.Id,
                    Sender = "Gözlemci Ekibi",
                    Subject = "Gözlem Raporu Tamamlandı",
                    Content = $"{scoutedPlayer.Name} isimli oyuncunun gözlem raporu tamamlandı! Transfer Hub'dan detayları inceleyebilirsiniz.",
                    DateSent = currentDate,
                    IsRead = false
                });
            }
        }

        if (rand.Next(100) < 5) // 5% chance of injury
        {
            _db.Messages.Add(new AltLigMenajer.Models.Message
            {
                ManagerId = manager.Id,
                Sender = "Sağlık Ekibi",
                Subject = "Oyuncu Sakatlığı",
                Content = "Antrenman sırasında bir oyuncumuz hafif sakatlık geçirdi.",
                DateSent = currentDate,
                IsRead = false
            });
        }

        if (currentDate.Day == 1) // First of the month
        {
            _db.Messages.Add(new AltLigMenajer.Models.Message
            {
                ManagerId = manager.Id,
                Sender = "Yönetim Kurulu",
                Subject = "Aylık Beklentiler",
                Content = "Yeni ayda takımın finansal ve sportif durumunu yakından takip edeceğiz. Başarılar dileriz.",
                DateSent = currentDate,
                IsRead = false
            });

            // ── Player Progression & Decline ──
            ProcessPlayerGrowth(_db, rand, currentDate, manager.Id, teamId);
        }
        
        _db.SaveChanges();

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult SaveGame()
    {
        return Json(new { success = true, message = "Oyun Kaydedildi" });
    }

    [HttpPost]
    public IActionResult SetCurrency(string currency)
    {
        var cookieOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = false,
            IsEssential = true
        };
        Response.Cookies.Append("CurrencyPreference", currency == "TL" ? "TL" : "EUR", cookieOptions);
        return RedirectToAction("Index");
    }

    /// <summary>Process monthly player growth (young) and decline (old) for ALL players in the league.</summary>
    private static void ProcessPlayerGrowth(
        AltLigMenajer.Data.ApplicationDbContext db,
        Random rand,
        DateTime currentDate,
        int managerId,
        int? userTeamId)
    {
        var allPlayers = db.Players.ToList();
        int grewCount = 0, declinedCount = 0;
        var userTeamChanges = new List<string>();

        foreach (var player in allPlayers)
        {
            bool changed = false;
            int oldOvr = player.Ovr;

            // ── Growth: Age ≤ 27, Ovr < Potential ──
            if (player.Age <= 27 && player.Ovr < player.Potential)
            {
                int growChance = 30; // 30% base chance per month
                if (player.IsStarter) growChance += 10; // +10% for starters (match experience)

                if (rand.Next(100) < growChance)
                {
                    player.Ovr = Math.Min(player.Ovr + 1, player.Potential);
                    changed = true;
                    grewCount++;
                }
            }

            // ── Decline: Age ≥ 32 ──
            if (player.Age >= 32)
            {
                int declineChance = 15; // 15% chance per month
                if (player.Age >= 35) declineChance = 30; // Accelerated decline after 35

                if (rand.Next(100) < declineChance)
                {
                    player.Ovr = Math.Max(player.Ovr - 1, 30); // Min OVR = 30
                    changed = true;
                    declinedCount++;
                }
            }

            // ── Scale Value & Wage when Ovr changes ──
            if (changed && player.Ovr != oldOvr)
            {
                double scaleFactor = player.Ovr > oldOvr ? 1.08 : 0.92; // ±8%
                player.Value = Math.Max((int)(player.Value * scaleFactor), 50_000);
                player.Wage = Math.Max((int)(player.Wage * scaleFactor), 50_000);

                // Track changes for user's team
                if (player.TeamId == userTeamId)
                {
                    string arrow = player.Ovr > oldOvr ? "📈" : "📉";
                    userTeamChanges.Add($"{arrow} {player.Name}: OVR {oldOvr} → {player.Ovr}");
                }
            }
        }

        // Send progression report to manager
        if (userTeamChanges.Any())
        {
            db.Messages.Add(new AltLigMenajer.Models.Message
            {
                ManagerId = managerId,
                Sender = "Teknik Ekip",
                Subject = "Aylık Oyuncu Gelişim Raporu",
                Content = string.Join("\n", userTeamChanges),
                DateSent = currentDate,
                IsRead = false
            });
        }
    }
}

public class TacticController : Controller
{
    private readonly AltLigMenajer.Data.ApplicationDbContext _db;

    public TacticController(AltLigMenajer.Data.ApplicationDbContext db) => _db = db;

    public IActionResult Index()
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        var teamId = manager?.ManagedTeamId;

        var team = _db.Teams.FirstOrDefault(t => t.Id == teamId);
        if (team is null) return NotFound();

        var players = _db.Players.Where(p => p.TeamId == team.Id).ToList();

        var starterCount = players.Count(p => p.IsStarter);
        if (starterCount != 11 && players.Count >= 11)
        {
            if (starterCount > 11) 
            {
                foreach(var p in players) { p.IsStarter = false; p.IsSubstitute = true; p.PitchPosition = null; }
                int idx = 0;
                foreach(var p in players.OrderByDescending(p => p.Ovr).Take(11)) { p.IsStarter = true; p.IsSubstitute = false; p.PitchPosition = idx++; }
            }
            else 
            {
                var needed = 11 - starterCount;
                var toAdd = players.Where(p => !p.IsStarter).OrderByDescending(p => p.Ovr).Take(needed);
                int maxPos = players.Where(p => p.IsStarter).Max(p => p.PitchPosition) ?? -1;
                foreach(var p in toAdd) { p.IsStarter = true; p.IsSubstitute = false; p.PitchPosition = ++maxPos; }
            }
            _db.SaveChanges();
        }

        var vm = new AltLigMenajer.Models.TacticViewModel
        {
            Team = team,
            Starters = players.Where(p => p.IsStarter).OrderBy(p => p.PitchPosition ?? 999).ToList(),
            Substitutes = players.Where(p => !p.IsStarter).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult SaveTactic([FromBody] AltLigMenajer.Models.SaveTacticRequest request)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return Unauthorized();

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        var teamId = manager?.ManagedTeamId;

        var team = _db.Teams.FirstOrDefault(t => t.Id == teamId);
        if (team is null) return NotFound();

        var players = _db.Players.Where(p => p.TeamId == team.Id).ToList();
        var formation = request.Formation ?? "4-2-3-1";

        // ── Position group validation ──
        foreach (var posDto in request.PlayerPositions)
        {
            var player = players.FirstOrDefault(p => p.Id == posDto.PlayerId);
            if (player == null) continue;

            var playerGroup = Helpers.CurrencyHelper.GetPositionGroup(player.Position);
            var slotGroup = Helpers.CurrencyHelper.GetSlotPositionGroup(posDto.PitchPosition, formation);

            if (playerGroup != slotGroup)
            {
                return Json(new { success = false, error = $"{player.Name} ({player.Position}) bu mevki için uygun değil! {slotGroup} grubundan bir oyuncu gerekli." });
            }
        }

        team.TeamInstruction = request.Instruction;
        team.PassingStyle = request.PassingStyle;
        team.Formation = formation;

        // Block injured players from starting 11
        bool hadInjuredStarter = false;
        foreach (var p in players)
        {
            var posDto = request.PlayerPositions.FirstOrDefault(x => x.PlayerId == p.Id);
            if (posDto != null)
            {
                if (p.IsInjured)
                {
                    // Force injured player to bench
                    p.IsStarter = false;
                    p.IsSubstitute = true;
                    p.PitchPosition = null;
                    hadInjuredStarter = true;
                }
                else
                {
                    p.IsStarter = true;
                    p.IsSubstitute = false;
                    p.PitchPosition = posDto.PitchPosition;
                }
            }
            else
            {
                p.IsStarter = false;
                p.IsSubstitute = true;
                p.PitchPosition = null;
            }
        }

        _db.SaveChanges();

        if (hadInjuredStarter)
            return Json(new { success = true, warning = "Sakatlı oyuncular ilk 11'den çıkarıldı!" });

        return Json(new { success = true });
    }
}

public class LeagueController : Controller
{
    private readonly AltLigMenajer.Data.ApplicationDbContext _db;

    public LeagueController(AltLigMenajer.Data.ApplicationDbContext db) => _db = db;

    public IActionResult Index(int? tier)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        var manager = managerId != null ? _db.Managers.Include(m => m.ManagedTeam).FirstOrDefault(m => m.Id == managerId) : null;

        int userTeamTier = manager?.ManagedTeam?.LeagueTier ?? 2;
        int selectedTier = tier ?? userTeamTier;

        var teams = _db.Teams.Where(t => t.LeagueTier == selectedTier).ToList();
        var fixtures = _db.Fixtures.Where(f => f.IsPlayed && f.LeagueTier == selectedTier).ToList();

        // Compute current week for this tier
        var currentWeek = _db.Fixtures.Where(f => !f.IsPlayed && f.LeagueTier == selectedTier).OrderBy(f => f.Matchweek).Select(f => f.Matchweek).FirstOrDefault();
        if (currentWeek == 0) currentWeek = 34;

        var standings = teams.Select(t =>
        {
            var homeMatches = fixtures.Where(f => f.HomeTeamId == t.Id);
            var awayMatches = fixtures.Where(f => f.AwayTeamId == t.Id);

            int gf = homeMatches.Sum(f => f.HomeScore ?? 0) + awayMatches.Sum(f => f.AwayScore ?? 0);
            int ga = homeMatches.Sum(f => f.AwayScore ?? 0) + awayMatches.Sum(f => f.HomeScore ?? 0);

            int won = homeMatches.Count(f => f.HomeScore > f.AwayScore) + awayMatches.Count(f => f.AwayScore > f.HomeScore);
            int drawn = homeMatches.Count(f => f.HomeScore == f.AwayScore) + awayMatches.Count(f => f.HomeScore == f.AwayScore);
            int lost = homeMatches.Count(f => f.HomeScore < f.AwayScore) + awayMatches.Count(f => f.AwayScore < f.HomeScore);

            return new TeamStanding
            {
                Team = t,
                Played = won + drawn + lost,
                Won = won,
                Drawn = drawn,
                Lost = lost,
                GoalsFor = gf,
                GoalsAgainst = ga
            };
        })
        .OrderByDescending(s => s.Points)
        .ThenByDescending(s => s.GoalDifference)
        .ThenByDescending(s => s.GoalsFor)
        .ToList();

        var vm = new LeagueViewModel
        {
            Standings = standings,
            CurrentWeek = currentWeek,
            ManagerTeamId = manager?.ManagedTeamId,
            SelectedTier = selectedTier,
            UserTeamTier = userTeamTier
        };

        return View(vm);
    }

    public IActionResult Fixtures(int? week, int? tier)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        var manager = managerId != null ? _db.Managers.Include(m => m.ManagedTeam).FirstOrDefault(m => m.Id == managerId) : null;

        int userTeamTier = manager?.ManagedTeam?.LeagueTier ?? 2;
        int selectedTier = tier ?? userTeamTier;

        var selectedWeek = week ?? _db.Fixtures.Where(f => !f.IsPlayed && f.LeagueTier == selectedTier).OrderBy(f => f.Matchweek).Select(f => f.Matchweek).FirstOrDefault();
        if (selectedWeek == 0) selectedWeek = 1;

        var matches = _db.Fixtures
            .Include(f => f.HomeTeam)
            .Include(f => f.AwayTeam)
            .Where(f => f.Matchweek == selectedWeek && f.LeagueTier == selectedTier)
            .OrderBy(f => f.MatchDate)
            .ToList();

        var vm = new FixtureWeekViewModel
        {
            SelectedWeek = selectedWeek,
            Matches = matches,
            ManagerTeamId = manager?.ManagedTeamId
        };

        ViewBag.SelectedTier = selectedTier;
        ViewBag.UserTeamTier = userTeamTier;

        return View(vm);
    }

    public IActionResult Stats(string? type, int? tier)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        var manager = managerId != null ? _db.Managers.Include(m => m.ManagedTeam).FirstOrDefault(m => m.Id == managerId) : null;
        int userTeamTier = manager?.ManagedTeam?.LeagueTier ?? 2;
        int selectedTier = tier ?? userTeamTier;

        var statType = type ?? "goals";
        List<Player> topPlayers;

        // Filter by teams in the selected tier
        var tierTeamIds = _db.Teams.Where(t => t.LeagueTier == selectedTier).Select(t => t.Id).ToList();

        if (statType == "assists")
            topPlayers = _db.Players.Include(p => p.Team).Where(p => tierTeamIds.Contains(p.TeamId)).OrderByDescending(p => p.Assists).ThenByDescending(p => p.Ovr).Take(15).ToList();
        else
            topPlayers = _db.Players.Include(p => p.Team).Where(p => tierTeamIds.Contains(p.TeamId)).OrderByDescending(p => p.Goals).ThenByDescending(p => p.Ovr).Take(15).ToList();

        var vm = new StatsViewModel
        {
            StatType = statType,
            TopPlayers = topPlayers
        };

        ViewBag.SelectedTier = selectedTier;
        ViewBag.UserTeamTier = userTeamTier;

        return View(vm);
    }

    public IActionResult TeamProfile(int id)
    {
        var team = _db.Teams.FirstOrDefault(t => t.Id == id);
        if (team == null) return NotFound();

        var players = _db.Players.Where(p => p.TeamId == id).OrderByDescending(p => p.Ovr).ToList();

        var vm = new TeamProfileViewModel
        {
            Team = team,
            Players = players
        };

        return View(vm);
    }
}

public class TransferController : Controller
{
    private readonly AltLigMenajer.Data.ApplicationDbContext _db;

    public TransferController(AltLigMenajer.Data.ApplicationDbContext db) => _db = db;

    public IActionResult Index(string? search, string? filter)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        if (manager == null) return RedirectToAction("Index", "Onboarding");
        var teamId = manager.ManagedTeamId;

        var team = _db.Teams.FirstOrDefault(t => t.Id == teamId);
        if (team == null) return NotFound();

        var gameSetting = _db.GameSettings.FirstOrDefault();
        var currentDate = gameSetting?.CurrentDate ?? new DateTime(2025, 6, 30);

        // Get all scout reports for this team
        var scoutReports = _db.ScoutReports.Where(r => r.TeamId == teamId).ToList();
        var pendingCount = scoutReports.Count(r => !r.IsCompleted);

        // Get all players NOT on the manager's team
        IQueryable<Player> query = _db.Players.Where(p => p.TeamId != teamId);

        // Search by name
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));
        }

        // Apply filters
        if (filter == "young")
        {
            query = query.Where(p => p.Age <= 25);
        }
        else if (filter == "budget")
        {
            query = query.Where(p => p.Value <= (int)team.Budget);
        }
        else if (filter == "scouted")
        {
            var scoutedPlayerIds = scoutReports.Where(r => r.IsCompleted).Select(r => r.PlayerId).ToList();
            query = query.Where(p => scoutedPlayerIds.Contains(p.Id));
        }

        var players = query.OrderByDescending(p => p.Ovr).Take(50).ToList();

        var isWindowOpen = Helpers.CurrencyHelper.IsTransferWindowOpen(currentDate);

        var vm = new TransferHubViewModel
        {
            MyTeam = team,
            Manager = manager,
            AvailablePlayers = players,
            ScoutReports = scoutReports,
            PendingScoutCount = pendingCount,
            ActiveFilter = filter,
            SearchQuery = search,
            CurrentDate = currentDate
        };

        ViewBag.IsTransferWindowOpen = isWindowOpen;

        return View(vm);
    }

    [HttpPost]
    public IActionResult ScoutPlayer(int playerId)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        if (manager == null) return RedirectToAction("Index", "Onboarding");
        var teamId = manager.ManagedTeamId;

        // Check max 3 pending scouts
        var gameSetting0 = _db.GameSettings.FirstOrDefault();
        var curDate0 = gameSetting0?.CurrentDate ?? DateTime.Now;
        if (!Helpers.CurrencyHelper.IsTransferWindowOpen(curDate0))
        {
            TempData["TransferError"] = "Transfer dönemi kapalı! Yaz (30 Haz - 15 Eyl) veya Kış (1-31 Ocak) döneminde gelin.";
            return RedirectToAction("Index");
        }

        var pendingCount = _db.ScoutReports.Count(r => r.TeamId == teamId && !r.IsCompleted);
        if (pendingCount >= 3)
        {
            TempData["TransferError"] = "En fazla 3 aktif gözlem görevi olabilir! Bir gözlem tamamlanana kadar bekleyin.";
            return RedirectToAction("Index");
        }

        // Check if already scouting/scouted this player
        var existing = _db.ScoutReports.FirstOrDefault(r => r.TeamId == teamId && r.PlayerId == playerId);
        if (existing != null)
        {
            TempData["TransferError"] = "Bu oyuncu zaten gözlemleniyor veya gözlemlenmiş.";
            return RedirectToAction("Index");
        }

        var gameSetting = _db.GameSettings.FirstOrDefault();
        var currentDate = gameSetting?.CurrentDate ?? DateTime.Now;

        _db.ScoutReports.Add(new ScoutReport
        {
            TeamId = teamId ?? 0,
            PlayerId = playerId,
            CompletionDate = currentDate.AddDays(7),
            IsCompleted = false
        });
        _db.SaveChanges();

        TempData["TransferSuccess"] = "Gözlemci gönderildi! Rapor 7 gün içinde tamamlanacak.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult MakeOffer(int playerId, decimal offerAmount)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        if (manager == null) return RedirectToAction("Index", "Onboarding");
        var teamId = manager.ManagedTeamId;

        var myTeam = _db.Teams.FirstOrDefault(t => t.Id == teamId);
        if (myTeam == null) return NotFound();

        var player = _db.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null) return NotFound();

        var sellingTeam = _db.Teams.FirstOrDefault(t => t.Id == player.TeamId);
        if (sellingTeam == null) return NotFound();

        var gameSetting = _db.GameSettings.FirstOrDefault();
        var currentDate = gameSetting?.CurrentDate ?? DateTime.Now;

        // Transfer window check
        if (!Helpers.CurrencyHelper.IsTransferWindowOpen(currentDate))
        {
            TempData["TransferError"] = "Transfer dönemi kapalı! Yaz (30 Haz - 15 Eyl) veya Kış (1-31 Ocak) döneminde gelin.";
            return RedirectToAction("Index");
        }

        // Must be scouted first
        var report = _db.ScoutReports.FirstOrDefault(r => r.TeamId == teamId && r.PlayerId == playerId && r.IsCompleted);
        if (report == null)
        {
            TempData["TransferError"] = "Bu oyuncuyu önce gözlemlemelisiniz!";
            return RedirectToAction("Index");
        }

        // Player interest check: selling team star > buying team star + 1 → rejection
        if (sellingTeam.RequiredStarRating > myTeam.RequiredStarRating + 1)
        {
            _db.Messages.Add(new Message
            {
                ManagerId = manager.Id,
                Sender = player.Name,
                Subject = "Transfer Reddedildi",
                Content = "Daha düşük seviyeli bir takıma gitmek istemiyorum. Kariyer hedeflerim için daha üst düzey bir kulüpte kalmayı tercih ediyorum.",
                DateSent = currentDate,
                IsRead = false
            });
            _db.SaveChanges();
            TempData["TransferError"] = $"{player.Name} daha düşük seviyeli bir takıma gelmek istemiyor.";
            return RedirectToAction("Index");
        }

        // AI Valuation: young + high potential → premium
        decimal aiAskingPrice = player.Value;
        if (player.Age <= 23 && player.Potential > 80)
        {
            var rand = new Random();
            var multiplier = 1.5m + (decimal)(rand.NextDouble() * 0.5); // 1.5x to 2.0x
            aiAskingPrice = player.Value * multiplier;
        }
        else
        {
            aiAskingPrice = player.Value * 1.1m; // Standard 10% markup
        }

        // Check if offer meets AI asking price
        if (offerAmount < aiAskingPrice)
        {
            _db.Messages.Add(new Message
            {
                ManagerId = manager.Id,
                Sender = sellingTeam.Name,
                Subject = "Teklif Reddedildi",
                Content = $"{player.Name} için teklifiniz ({offerAmount:N0} €) yetersiz bulunmuştur. Minimum {aiAskingPrice:N0} € bekliyoruz.",
                DateSent = currentDate,
                IsRead = false
            });
            _db.SaveChanges();
            TempData["TransferError"] = $"Teklif reddedildi. {sellingTeam.Name} minimum {aiAskingPrice:N0} € istiyor.";
            return RedirectToAction("Index");
        }

        // Budget check
        if (offerAmount > myTeam.Budget)
        {
            TempData["TransferError"] = "Transfer bütçeniz yetersiz!";
            return RedirectToAction("Index");
        }

        // Execute transfer!
        myTeam.Budget -= offerAmount;
        sellingTeam.Budget += offerAmount;

        player.TeamId = myTeam.Id;
        player.IsStarter = false;
        player.IsSubstitute = true;
        player.IsTransferListed = false;
        player.IsLoanListed = false;
        player.PitchPosition = null;
        player.FormationRow = -1;

        _db.Messages.Add(new Message
        {
            ManagerId = manager.Id,
            Sender = "Yönetim",
            Subject = "Transfer Tamamlandı!",
            Content = $"{player.Name} ({player.Position}, OVR {player.Ovr}) takımımıza {offerAmount:N0} € karşılığında katıldı. Hoş geldin!",
            DateSent = currentDate,
            IsRead = false
        });

        _db.SaveChanges();
        TempData["TransferSuccess"] = $"{player.Name} başarıyla transfer edildi!";
        return RedirectToAction("Index");
    }

    /// <summary>View pending transfer offers for the user's players.</summary>
    public IActionResult Offers()
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var offers = _db.TransferOffers
            .Include(o => o.Player)
            .Include(o => o.OfferingTeam)
            .Where(o => o.ManagerId == managerId && o.Status == "Pending")
            .OrderByDescending(o => o.DateOffered)
            .ToList();

        return View(offers);
    }

    [HttpPost]
    public IActionResult AcceptOffer(int offerId)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");
        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        if (manager == null) return RedirectToAction("Index", "Onboarding");

        var offer = _db.TransferOffers
            .Include(o => o.Player)
            .Include(o => o.OfferingTeam)
            .FirstOrDefault(o => o.Id == offerId && o.ManagerId == managerId);
        if (offer == null) return NotFound();

        var player = offer.Player!;
        var buyingTeam = offer.OfferingTeam!;
        var myTeam = _db.Teams.FirstOrDefault(t => t.Id == manager.ManagedTeamId);
        if (myTeam == null) return NotFound();

        // Execute transfer
        myTeam.Budget += offer.OfferedFee;
        buyingTeam.Budget -= offer.OfferedFee;

        player.TeamId = buyingTeam.Id;
        player.IsTransferListed = false;
        player.IsLoanListed = false;
        player.IsStarter = false;
        player.IsSubstitute = false;
        player.PitchPosition = null;
        player.FormationRow = -1;

        offer.Status = "Accepted";

        var gameSetting = _db.GameSettings.FirstOrDefault();
        var currentDate = gameSetting?.CurrentDate ?? DateTime.Now;

        _db.Messages.Add(new Message
        {
            ManagerId = manager.Id,
            Sender = "Yönetim",
            Subject = "Transfer Tamamlandı!",
            Content = $"{player.Name} isimli oyuncumuz {buyingTeam.Name} takımına {offer.OfferedFee:N0} € karşılığında satılmıştır.",
            DateSent = currentDate,
            IsRead = false
        });

        _db.SaveChanges();
        TempData["TransferSuccess"] = $"{player.Name} başarıyla {buyingTeam.Name} takımına satıldı! +{offer.OfferedFee:N0} €";
        return RedirectToAction("Offers");
    }

    [HttpPost]
    public IActionResult RejectOffer(int offerId)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var offer = _db.TransferOffers.FirstOrDefault(o => o.Id == offerId && o.ManagerId == managerId);
        if (offer == null) return NotFound();

        offer.Status = "Rejected";
        _db.SaveChanges();

        TempData["TransferSuccess"] = "Teklif reddedildi.";
        return RedirectToAction("Offers");
    }

    [HttpPost]
    public IActionResult CounterOffer(int offerId, decimal counterAmount)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");
        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        if (manager == null) return RedirectToAction("Index", "Onboarding");

        var offer = _db.TransferOffers
            .Include(o => o.Player)
            .Include(o => o.OfferingTeam)
            .FirstOrDefault(o => o.Id == offerId && o.ManagerId == managerId && o.Status == "Pending");
        if (offer == null) return NotFound();

        var player = offer.Player!;
        var buyingTeam = offer.OfferingTeam!;
        var myTeam = _db.Teams.FirstOrDefault(t => t.Id == manager.ManagedTeamId);
        if (myTeam == null) return NotFound();

        var gameSetting = _db.GameSettings.FirstOrDefault();
        var currentDate = gameSetting?.CurrentDate ?? DateTime.Now;

        // AI accepts if counter-demand <= 1.3x player's base Value
        if (counterAmount <= player.Value * 1.3m)
        {
            // AI accepts the counter-offer
            myTeam.Budget += counterAmount;
            buyingTeam.Budget -= counterAmount;

            player.TeamId = buyingTeam.Id;
            player.IsTransferListed = false;
            player.IsLoanListed = false;
            player.IsStarter = false;
            player.IsSubstitute = false;
            player.PitchPosition = null;
            player.FormationRow = -1;

            offer.Status = "Accepted";
            offer.OfferedFee = counterAmount;

            _db.Messages.Add(new Message
            {
                ManagerId = manager.Id,
                Sender = buyingTeam.Name,
                Subject = "Pazarlık Kabul Edildi!",
                Content = $"{buyingTeam.Name}, {player.Name} için pazarlık talebinizi kabul etti. Transfer {counterAmount:N0} € karşılığında tamamlandı.",
                DateSent = currentDate,
                IsRead = false
            });

            _db.SaveChanges();
            TempData["TransferSuccess"] = $"Pazarlık başarılı! {player.Name} → {buyingTeam.Name} ({counterAmount:N0} €)";
        }
        else
        {
            // AI rejects and walks away
            offer.Status = "Rejected";

            _db.Messages.Add(new Message
            {
                ManagerId = manager.Id,
                Sender = buyingTeam.Name,
                Subject = "Pazarlık Reddedildi",
                Content = $"{buyingTeam.Name}, {player.Name} için {counterAmount:N0} € talebinizi çok yüksek buldu ve masadan kalktı.",
                DateSent = currentDate,
                IsRead = false
            });

            _db.SaveChanges();
            TempData["TransferError"] = $"{buyingTeam.Name} pazarlık talebinizi reddetti ve masadan kalktı.";
        }

        return RedirectToAction("Offers");
    }
}

public class SquadController : Controller
{
    private readonly AltLigMenajer.Data.ApplicationDbContext _db;

    public SquadController(AltLigMenajer.Data.ApplicationDbContext db) => _db = db;

    public IActionResult Index(string sortOrder)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        var teamId = manager?.ManagedTeamId;

        var players = _db.Players.Where(p => p.TeamId == teamId).ToList();

        // Logical position order: GK → DEF → MID → ATT
        var positionOrder = new Dictionary<string, int>
        {
            ["KL"]  = 0,  // Kaleci (GK)
            ["SAB"] = 1,  // Sağ Bek (RB)
            ["SLB"] = 2,  // Sol Bek (LB)
            ["STP"] = 3,  // Stoper (CB)
            ["DOS"] = 4,  // Defansif Orta Saha (CDM)
            ["MO"]  = 5,  // Merkez Orta Saha (CM)
            ["OOS"] = 6,  // Ofansif Orta Saha (CAM)
            ["SLO"] = 7,  // Sol Orta Saha / Sol Kanat (LM/LW)
            ["SAO"] = 8,  // Sağ Orta Saha / Sağ Kanat (RM/RW)
            ["SNT"] = 9,  // Santrfor (ST)
        };

        switch (sortOrder)
        {
            case "position":
                players = players
                    .OrderBy(p => positionOrder.GetValueOrDefault(p.Position, 99))
                    .ThenByDescending(p => p.Ovr)
                    .ToList();
                break;
            case "fatigue":
                players = players.OrderByDescending(p => p.Fatigue).ToList();
                break;
            default:
                players = players.OrderByDescending(p => p.Ovr).ToList();
                break;
        }

        return View(players);
    }

    public IActionResult Details(int id)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        var teamId = manager?.ManagedTeamId;

        var player = _db.Players.FirstOrDefault(p => p.Id == id && p.TeamId == teamId);
        if (player == null) return NotFound();

        return View("PlayerDetail", player);
    }

    [HttpPost]
    public IActionResult ListForTransfer(int id, decimal askingPrice)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        var teamId = manager?.ManagedTeamId;

        var player = _db.Players.FirstOrDefault(p => p.Id == id && p.TeamId == teamId);
        if (player == null) return NotFound();

        player.IsTransferListed = true;
        player.AskingPrice = askingPrice;

        var gameSetting = _db.GameSettings.FirstOrDefault();
        var currentDate = gameSetting?.CurrentDate ?? DateTime.Now;

        _db.Messages.Add(new AltLigMenajer.Models.Message
        {
            ManagerId = managerId.Value,
            Sender = "Yönetim Kurulu",
            Subject = "Transfer Listesi",
            Content = $"{player.Name} isimli oyuncu {askingPrice:N0} € bedelle transfer listesine eklendi.",
            DateSent = currentDate,
            IsRead = false
        });

        _db.SaveChanges();

        TempData["Notification"] = "Oyuncu transfer listesine eklendi.";
        return RedirectToAction("Details", new { id = id });
    }

    [HttpPost]
    public IActionResult RemoveFromTransferList(int id)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        var teamId = manager?.ManagedTeamId;

        var player = _db.Players.FirstOrDefault(p => p.Id == id && p.TeamId == teamId);
        if (player == null) return NotFound();

        player.IsTransferListed = false;
        _db.SaveChanges();

        TempData["Notification"] = "Oyuncu transfer listesinden kaldırıldı.";
        return RedirectToAction("Details", new { id = id });
    }

    [HttpPost]
    public IActionResult ListForLoan(int id)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        var teamId = manager?.ManagedTeamId;

        var player = _db.Players.FirstOrDefault(p => p.Id == id && p.TeamId == teamId);
        if (player == null) return NotFound();

        player.IsLoanListed = true;
        _db.SaveChanges();

        TempData["Notification"] = "Oyuncu kiralık listesine eklendi.";
        return RedirectToAction("Details", new { id = id });
    }

    [HttpGet]
    public IActionResult RenewContract(int id)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        var teamId = manager?.ManagedTeamId;

        var player = _db.Players.FirstOrDefault(p => p.Id == id && p.TeamId == teamId);
        if (player == null) return NotFound();

        return View("ContractRenewal", player);
    }

    [HttpPost]
    public IActionResult RenewContractPost(int playerId, int offeredWage, int offeredYears)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        var teamId = manager?.ManagedTeamId;

        var player = _db.Players.FirstOrDefault(p => p.Id == playerId && p.TeamId == teamId);
        if (player == null) return NotFound();

        var rand = new Random();
        var pendingOffer = new AltLigMenajer.Models.PendingContractOffer
        {
            PlayerId = player.Id,
            ManagerId = managerId.Value,
            OfferedWage = offeredWage,
            OfferedYears = offeredYears,
            DaysUntilResponse = rand.Next(2, 5) // 2 to 4 days
        };

        _db.PendingContractOffers.Add(pendingOffer);
        _db.SaveChanges();

        TempData["Notification"] = "Teklif iletildi, oyuncunun kararı bekleniyor.";
        return RedirectToAction("Details", new { id = playerId });
    }
}

public class OfficeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Home");
}
