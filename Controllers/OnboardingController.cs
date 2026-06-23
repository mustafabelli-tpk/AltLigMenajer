using Microsoft.AspNetCore.Mvc;
using AltLigMenajer.Data;
using AltLigMenajer.Models;

namespace AltLigMenajer.Controllers;

public class OnboardingController : Controller
{
    private readonly ApplicationDbContext _db;

    public OnboardingController(ApplicationDbContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult CreateProfile()
    {
        return View();
    }

    [HttpPost]
    public IActionResult CreateProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            ModelState.AddModelError("", "First name and last name are required.");
            return View();
        }

        HttpContext.Session.SetString("ManagerFirstName", firstName);
        HttpContext.Session.SetString("ManagerLastName", lastName);

        return RedirectToAction("SelectCoach");
    }

    [HttpGet]
    public IActionResult SelectCoach()
    {
        var firstName = HttpContext.Session.GetString("ManagerFirstName");
        if (string.IsNullOrEmpty(firstName))
            return RedirectToAction("CreateProfile");

        return View();
    }

    [HttpPost]
    public IActionResult SelectCoach(string coachTrait)
    {
        if (string.IsNullOrWhiteSpace(coachTrait))
        {
            return View();
        }

        HttpContext.Session.SetString("CoachTrait", coachTrait);
        return RedirectToAction("SelectTeam");
    }

    [HttpGet]
    public IActionResult SelectTeam()
    {
        var firstName = HttpContext.Session.GetString("ManagerFirstName");
        var coachTrait = HttpContext.Session.GetString("CoachTrait");

        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(coachTrait))
            return RedirectToAction("CreateProfile");

        // Default manager star rating is 1 for new managers
        ViewBag.ManagerStarRating = 1;

        var teams = _db.Teams.OrderBy(t => t.RequiredStarRating).ThenBy(t => t.Name).ToList();
        return View(teams);
    }

    [HttpPost]
    public IActionResult SelectTeam(int teamId)
    {
        var firstName = HttpContext.Session.GetString("ManagerFirstName");
        var lastName = HttpContext.Session.GetString("ManagerLastName");
        var coachTrait = HttpContext.Session.GetString("CoachTrait");

        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
        {
            return RedirectToAction("CreateProfile");
        }

        if (string.IsNullOrEmpty(coachTrait))
        {
            return RedirectToAction("SelectCoach");
        }

        var team = _db.Teams.FirstOrDefault(t => t.Id == teamId);
        if (team == null) return NotFound();

        // Server-side star rating check — new managers have star rating 1
        int managerStarRating = 1;
        if (team.RequiredStarRating > managerStarRating)
        {
            TempData["Error"] = "Bu takımı yönetmek için yeterli yıldız puanınız yok!";
            return RedirectToAction("SelectTeam");
        }

        // FIX: Remove the old manager if they are already assigned to this team to prevent SQLite Error 19 (UNIQUE constraint)
        var existingManager = _db.Managers.FirstOrDefault(m => m.ManagedTeamId == teamId);
        if (existingManager != null)
        {
            existingManager.ManagedTeamId = null;
        }

        var manager = new Manager
        {
            FirstName = firstName,
            LastName = lastName,
            CoachTrait = coachTrait,
            StarRating = managerStarRating,
            ManagedTeamId = teamId
        };

        _db.Managers.Add(manager);
        _db.SaveChanges();

        HttpContext.Session.SetInt32("ManagedTeamId", team.Id);
        HttpContext.Session.SetInt32("ManagerId", manager.Id);

        return RedirectToAction("Index", "Office");
    }
}
