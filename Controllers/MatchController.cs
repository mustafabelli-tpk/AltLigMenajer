using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AltLigMenajer.Data;
using AltLigMenajer.Models;

namespace AltLigMenajer.Controllers;

public class MatchController : Controller
{
    private readonly ApplicationDbContext _db;

    public MatchController(ApplicationDbContext db) => _db = db;

    // ── Position categories for weighted selection ──
    // Forwards: SNT (ST)
    // Wingers/Attacking Mids: SLO (LW), SAO (RW), OOS (CAM)
    // Midfielders: DOS (CDM), MO (CM)
    // Defenders: STP (CB), SLB (LB), SAB (RB)
    // Goalkeeper: KL (GK)

    private static readonly HashSet<string> ForwardPositions = new() { "SNT" };
    private static readonly HashSet<string> WingerAttPositions = new() { "SLO", "SAO", "OOS" };
    private static readonly HashSet<string> MidPositions = new() { "DOS", "MO" };
    private static readonly HashSet<string> DefPositions = new() { "STP", "SLB", "SAB" };
    private static readonly HashSet<string> GkPositions = new() { "KL" };

    /// <summary>Load the match engine UI for a given fixture.</summary>
    public IActionResult Play(int id)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        if (manager == null) return RedirectToAction("Index", "Onboarding");

        var fixture = _db.Fixtures
            .Include(f => f.HomeTeam)
            .Include(f => f.AwayTeam)
            .FirstOrDefault(f => f.Id == id);

        if (fixture == null || fixture.IsPlayed) return RedirectToAction("Index", "Home");

        var teamId = manager.ManagedTeamId;

        // Verify this fixture involves the manager's team
        if (fixture.HomeTeamId != teamId && fixture.AwayTeamId != teamId)
            return RedirectToAction("Index", "Home");

        // Load home team players (exclude injured)
        var homePlayers = _db.Players.Where(p => p.TeamId == fixture.HomeTeamId && !p.IsInjured).ToList();
        var homeStarters = homePlayers.Where(p => p.IsStarter).OrderBy(p => p.PitchPosition ?? 999).ToList();
        var homeBench = homePlayers.Where(p => !p.IsStarter).OrderByDescending(p => p.Ovr).ToList();

        // If home team doesn't have 11 starters, auto-fill
        if (homeStarters.Count < 11)
        {
            var needed = 11 - homeStarters.Count;
            var toAdd = homeBench.Take(needed).ToList();
            homeStarters.AddRange(toAdd);
            homeBench = homeBench.Skip(needed).ToList();
        }

        // Load away team players (exclude injured)
        var awayPlayers = _db.Players.Where(p => p.TeamId == fixture.AwayTeamId && !p.IsInjured).ToList();
        var awayStarters = awayPlayers.OrderByDescending(p => p.Ovr).Take(11).ToList();
        var awayBench = awayPlayers.Except(awayStarters).OrderByDescending(p => p.Ovr).Take(7).ToList();

        var vm = new MatchViewModel
        {
            Fixture = fixture,
            HomeTeam = fixture.HomeTeam!,
            AwayTeam = fixture.AwayTeam!,
            HomeStarters = homeStarters,
            HomeBench = homeBench,
            AwayStarters = awayStarters,
            AwayBench = awayBench,
            HomeOvr = homeStarters.Any() ? (int)Math.Round(homeStarters.Average(p => p.Ovr)) : 50,
            AwayOvr = awayStarters.Any() ? (int)Math.Round(awayStarters.Average(p => p.Ovr)) : 50
        };

        return View(vm);
    }

    /// <summary>Persist the match result after the JS engine finishes.</summary>
    [HttpPost]
    public IActionResult SaveResult([FromBody] SaveMatchResultRequest req)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return Unauthorized();

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        if (manager == null) return Unauthorized();
        var userTeamId = manager.ManagedTeamId;

        var fixture = _db.Fixtures
            .Include(f => f.HomeTeam)
            .Include(f => f.AwayTeam)
            .FirstOrDefault(f => f.Id == req.FixtureId);

        if (fixture == null || fixture.IsPlayed)
            return BadRequest(new { error = "Fixture not found or already played." });

        // Save scores
        fixture.HomeScore = req.HomeScore;
        fixture.AwayScore = req.AwayScore;
        fixture.IsPlayed = true;

        // Update league points
        var homeTeam = fixture.HomeTeam!;
        var awayTeam = fixture.AwayTeam!;

        if (req.HomeScore > req.AwayScore)
            homeTeam.Points += 3;
        else if (req.HomeScore < req.AwayScore)
            awayTeam.Points += 3;
        else
        {
            homeTeam.Points += 1;
            awayTeam.Points += 1;
        }

        // Update player goal stats from JS engine
        foreach (var playerId in req.HomeGoalScorerIds)
        {
            var player = _db.Players.FirstOrDefault(p => p.Id == playerId);
            if (player != null) player.Goals++;
        }
        foreach (var playerId in req.AwayGoalScorerIds)
        {
            var player = _db.Players.FirstOrDefault(p => p.Id == playerId);
            if (player != null) player.Goals++;
        }

        // Update player assist stats from JS engine
        foreach (var playerId in req.HomeAssistIds)
        {
            var player = _db.Players.FirstOrDefault(p => p.Id == playerId);
            if (player != null) player.Assists++;
        }
        foreach (var playerId in req.AwayAssistIds)
        {
            var player = _db.Players.FirstOrDefault(p => p.Id == playerId);
            if (player != null) player.Assists++;
        }

        // ── Injury check for user's live match participants ──
        var rand = new Random();
        var gameSetting = _db.GameSettings.FirstOrDefault();
        var currentDate = gameSetting?.CurrentDate ?? DateTime.Now;

        var userMatchPlayers = _db.Players
            .Where(p => p.TeamId == fixture.HomeTeamId || p.TeamId == fixture.AwayTeamId)
            .Where(p => p.IsStarter && !p.IsInjured)
            .ToList();

        foreach (var p in userMatchPlayers)
        {
            if (rand.Next(100) < 4) // 4% injury chance per starter
            {
                int injuryDays = rand.Next(3, 21); // 3-20 days
                p.IsInjured = true;
                p.InjuryEndDate = currentDate.AddDays(injuryDays);

                // Send inbox notification only for user's team
                if (p.TeamId == userTeamId)
                {
                    _db.Messages.Add(new Message
                    {
                        ManagerId = manager.Id,
                        Sender = "Sağlık Ekibi",
                        Subject = "Oyuncu Sakatlığı!",
                        Content = $"{p.Name} maç sırasında sakatlandı. Tahmini iyileşme süresi: {injuryDays} gün. {p.InjuryEndDate:dd/MM/yyyy} tarihine kadar forma giyemeyecek.",
                        DateSent = currentDate,
                        IsRead = false
                    });
                }
            }
        }

        // ── Background Simulation: simulate all OTHER fixtures in this matchweek ──
        var matchweek = fixture.Matchweek;
        var otherFixtures = _db.Fixtures
            .Include(f => f.HomeTeam)
            .Include(f => f.AwayTeam)
            .Where(f => f.Matchweek == matchweek && !f.IsPlayed && f.Id != fixture.Id)
            .ToList();

        foreach (var otherMatch in otherFixtures)
        {
            SimulateBackgroundMatch(otherMatch, rand, currentDate, manager.Id, userTeamId);
        }

        // ── Award XP for match result ──
        {
            bool isHome = fixture.HomeTeamId == userTeamId;
            int myScore = isHome ? req.HomeScore : req.AwayScore;
            int oppScore = isHome ? req.AwayScore : req.HomeScore;

            int xpEarned = 0;
            if (myScore > oppScore) xpEarned = 10;
            else if (myScore == oppScore) xpEarned = 3;
            // Loss = 0 XP

            manager.ExperiencePoints += xpEarned;

            // ── Milestone bonuses: check if user's team leads at mid-season or end-of-season ──
            if (fixture.Matchweek == 17 || fixture.Matchweek == 34)
            {
                // Get user's team tier
                var userTeam = _db.Teams.FirstOrDefault(t => t.Id == userTeamId);
                if (userTeam != null)
                {
                    var tierFixtures = _db.Fixtures
                        .Where(f => f.IsPlayed && f.LeagueTier == userTeam.LeagueTier)
                        .ToList();
                    var tierTeams = _db.Teams.Where(t => t.LeagueTier == userTeam.LeagueTier).ToList();

                    // Compute standings to find leader
                    var standings = tierTeams.Select(t =>
                    {
                        var hm = tierFixtures.Where(f => f.HomeTeamId == t.Id);
                        var am = tierFixtures.Where(f => f.AwayTeamId == t.Id);
                        int pts = hm.Count(f => f.HomeScore > f.AwayScore) * 3 + hm.Count(f => f.HomeScore == f.AwayScore)
                                + am.Count(f => f.AwayScore > f.HomeScore) * 3 + am.Count(f => f.AwayScore == f.HomeScore);
                        int gd = hm.Sum(f => (f.HomeScore ?? 0) - (f.AwayScore ?? 0)) + am.Sum(f => (f.AwayScore ?? 0) - (f.HomeScore ?? 0));
                        return new { TeamId = t.Id, Points = pts, GD = gd };
                    })
                    .OrderByDescending(s => s.Points)
                    .ThenByDescending(s => s.GD)
                    .ToList();

                    if (standings.Any() && standings[0].TeamId == userTeamId)
                    {
                        int milestoneXp = fixture.Matchweek == 34 ? 500 : 250;
                        manager.ExperiencePoints += milestoneXp;

                        string milestoneMsg = fixture.Matchweek == 34
                            ? $"Şampiyonluk! +{milestoneXp} XP kazandınız!"
                            : $"Devre liderliği! +{milestoneXp} XP kazandınız!";

                        _db.Messages.Add(new Message
                        {
                            ManagerId = manager.Id,
                            Sender = "Yönetim Kurulu",
                            Subject = "Başarı Ödülü!",
                            Content = milestoneMsg,
                            DateSent = currentDate,
                            IsRead = false
                        });
                    }
                }
            }

            // ── Level up check ──
            if (Helpers.CurrencyHelper.TryLevelUp(manager))
            {
                var newLabel = Helpers.CurrencyHelper.GetLicenseLabel(manager.LicenseLevel);
                _db.Messages.Add(new Message
                {
                    ManagerId = manager.Id,
                    Sender = "TFF",
                    Subject = "Lisans Yükseltme!",
                    Content = $"Tebrikler! Lisans seviyeniz yükseldi: {newLabel}. Yeni fırsatlar sizi bekliyor!",
                    DateSent = currentDate,
                    IsRead = false
                });
            }
        }

        // Advance date by 1 day
        if (gameSetting != null)
        {
            gameSetting.CurrentDate = gameSetting.CurrentDate.AddDays(1);
        }

        _db.SaveChanges();

        return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
    }

    /// <summary>Fully simulate a background match: scores, goals, assists, injuries, points.</summary>
    private void SimulateBackgroundMatch(Fixture match, Random rand, DateTime currentDate, int managerId, int? userTeamId)
    {
        var hTeam = match.HomeTeam!;
        var aTeam = match.AwayTeam!;

        // Get top-11 players per team (exclude injured)
        var homePlayers = _db.Players
            .Where(p => p.TeamId == hTeam.Id && !p.IsInjured)
            .OrderByDescending(p => p.Ovr)
            .Take(11).ToList();
        var awayPlayers = _db.Players
            .Where(p => p.TeamId == aTeam.Id && !p.IsInjured)
            .OrderByDescending(p => p.Ovr)
            .Take(11).ToList();

        var hOvr = homePlayers.Any() ? homePlayers.Average(p => p.Ovr) : 50.0;
        var aOvr = awayPlayers.Any() ? awayPlayers.Average(p => p.Ovr) : 50.0;

        // Generate scores
        var hExpected = (hOvr / 50.0) * 1.2 + 0.3; // home advantage
        var aExpected = (aOvr / 50.0) * 1.0;

        int hScore = SimGoals(rand, hExpected);
        int aScore = SimGoals(rand, aExpected);

        match.HomeScore = hScore;
        match.AwayScore = aScore;
        match.IsPlayed = true;

        // Update points
        if (hScore > aScore)
            hTeam.Points += 3;
        else if (hScore < aScore)
            aTeam.Points += 3;
        else
        {
            hTeam.Points += 1;
            aTeam.Points += 1;
        }

        // ── Distribute goals & assists for home team ──
        DistributeGoalsAndAssists(homePlayers, hScore, rand);

        // ── Distribute goals & assists for away team ──
        DistributeGoalsAndAssists(awayPlayers, aScore, rand);

        // ── Injury check for background matches ──
        var allPlayers = homePlayers.Concat(awayPlayers).ToList();
        foreach (var p in allPlayers)
        {
            if (rand.Next(100) < 3) // 3% per player
            {
                int injuryDays = rand.Next(3, 21);
                p.IsInjured = true;
                p.InjuryEndDate = currentDate.AddDays(injuryDays);
            }
        }
    }

    /// <summary>Distribute goals and assists to players based on position weighting.</summary>
    private void DistributeGoalsAndAssists(List<Player> players, int goals, Random rand)
    {
        if (players.Count == 0 || goals == 0) return;

        for (int g = 0; g < goals; g++)
        {
            // ── Pick goal scorer (weighted by position) ──
            var scorer = PickWeightedPlayer(players, rand, isGoalScorer: true);
            if (scorer != null)
            {
                scorer.Goals++;

                // ── 70% chance of an assist ──
                if (rand.Next(100) < 70)
                {
                    var possibleAssisters = players.Where(p => p.Id != scorer.Id).ToList();
                    var assister = PickWeightedPlayer(possibleAssisters, rand, isGoalScorer: false);
                    if (assister != null)
                    {
                        assister.Assists++;
                    }
                }
            }
        }
    }

    /// <summary>Pick a player using position-based weighting.</summary>
    private Player? PickWeightedPlayer(List<Player> players, Random rand, bool isGoalScorer)
    {
        if (players.Count == 0) return null;

        // Build weighted list
        var weighted = new List<(Player player, double weight)>();
        foreach (var p in players)
        {
            double w;
            if (isGoalScorer)
            {
                // Goal scorer weights: FW 65%, MID+Wing 25%, DEF+GK 10%
                if (ForwardPositions.Contains(p.Position))
                    w = 65;
                else if (WingerAttPositions.Contains(p.Position) || MidPositions.Contains(p.Position))
                    w = 25;
                else
                    w = 10;
            }
            else
            {
                // Assist weights: MID+Wing 70%, FW 20%, DEF+GK 10%
                if (WingerAttPositions.Contains(p.Position) || MidPositions.Contains(p.Position))
                    w = 70;
                else if (ForwardPositions.Contains(p.Position))
                    w = 20;
                else
                    w = 10;
            }
            weighted.Add((p, w));
        }

        double totalWeight = weighted.Sum(x => x.weight);
        double roll = rand.NextDouble() * totalWeight;
        double cumulative = 0;

        foreach (var (player, weight) in weighted)
        {
            cumulative += weight;
            if (roll <= cumulative)
                return player;
        }

        return weighted.Last().player;
    }

    /// <summary>Generate a random goal count using a Poisson distribution based on expected goals.</summary>
    private static int SimGoals(Random rand, double expected)
    {
        double L = Math.Exp(-expected);
        int k = 0;
        double p = 1.0;
        do
        {
            k++;
            p *= rand.NextDouble();
        } while (p > L);
        return Math.Min(k - 1, 6); // Cap at 6 goals max
    }
}
