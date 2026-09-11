using EventManagement.Api.Data;
using EventManagement.Api.Dtos;
using EventManagement.Api.Filters;
using EventManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/events/{eventId}/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;
    public CategoriesController(AppDbContext context) => _context = context;

    /// <summary>Lists the categories of one event. Any logged-in user.</summary>
    /// <response code="200">Categories returned.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet]
    [RequireLogin]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public IActionResult GetCategories(int eventId)
    {
        if (!_context.Events.Any(e => e.EventId == eventId))
            return NotFound("Event not found.");

        var categories = _context.Categories
            .Where(c => c.EventId == eventId)
            .Select(c => new { c.CategoryId, c.Name, c.EventId })
            .ToList();

        return Ok(categories);
    }

    /// <summary>Adds a category to an event. Organiser role required.</summary>
    /// <response code="201">Category created.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost]
    [RequireRole("Organiser")]
    [ProducesResponseType(201)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult CreateCategory(int eventId, CategoryDto request)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var ev = _context.Events.Find(eventId);
        if (ev is null) return NotFound("Event not found.");
        if (ev.OrganiserId != organiserId)
            return StatusCode(403, "You can only add categories to your own events.");

        var category = new Category { Name = request.Name, EventId = eventId };
        _context.Categories.Add(category);
        _context.SaveChanges();

        return CreatedAtAction(nameof(GetCategories), new { eventId },
            new { category.CategoryId, category.Name, category.EventId });
    }
}