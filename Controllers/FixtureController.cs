using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AltLigMenajer.Data;

namespace AltLigMenajer.Controllers;

public class FixtureController : Controller
{
    private readonly ApplicationDbContext _db;

    public FixtureController(ApplicationDbContext db) => _db = db;

    public IActionResult Index()
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var manager = _db.Managers.FirstOrDefault(m => m.Id == managerId);
        var teamId = manager?.ManagedTeamId;

        var fixtures = _db.Fixtures
            .Include(f => f.HomeTeam)
            .Include(f => f.AwayTeam)
            .Where(f => f.HomeTeamId == teamId || f.AwayTeamId == teamId)
            .OrderBy(f => f.MatchDate)
            .ToList();

        ViewBag.TeamId = teamId;
        return View(fixtures);
    }
}
