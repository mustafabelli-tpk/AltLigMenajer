using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AltLigMenajer.Data;

namespace AltLigMenajer.Controllers;

public class InboxController : Controller
{
    private readonly ApplicationDbContext _db;

    public InboxController(ApplicationDbContext db) => _db = db;

    public IActionResult Index()
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var messages = _db.Messages
            .Where(m => m.ManagerId == managerId)
            .OrderByDescending(m => m.DateSent)
            .ToList();

        return View(messages);
    }

    [HttpGet]
    public IActionResult Read(int id)
    {
        var managerId = HttpContext.Session.GetInt32("ManagerId");
        if (managerId == null) return RedirectToAction("Index", "Onboarding");

        var message = _db.Messages.FirstOrDefault(m => m.Id == id && m.ManagerId == managerId);
        if (message == null) return NotFound();

        if (!message.IsRead)
        {
            message.IsRead = true;
            _db.SaveChanges();
        }

        return View(message);
    }
}
