using EventManagement.Api.Data;
using EventManagement.Api.Dtos;
using EventManagement.Api.Filters;
using EventManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _context;
    public EventsController(AppDbContext context) => _context = context;

    /// <summary>Lists all events. Any logged-in user.</summary>
    /// <response code="200">Events returned.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    [RequireLogin]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult GetEvents()
    {
        var events = _context.Events
            .Select(e => new
            {
                e.EventId,
                e.Name,
                e.Description,
                e.EventDate,
                e.Location,
                e.Distance,
                e.EventType,
                e.OrganiserId,
                Organiser = e.Organiser!.FullName
            })
            .ToList();

        return Ok(events);
    }

    /// <summary>Returns one event by id.</summary>
    /// <response code="200">Event returned.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id}")]
    [RequireLogin]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public IActionResult GetEvent(int id)
    {
        var item = _context.Events
            .Include(e => e.Categories)
            .Where(e => e.EventId == id)
            .Select(e => new
            {
                e.EventId,
                e.Name,
                e.Description,
                e.EventDate,
                e.Location,
                e.Distance,
                e.EventType,
                e.OrganiserId,
                Categories = e.Categories.Select(c => new { c.CategoryId, c.Name })
            })
            .SingleOrDefault();

        if (item is null) return NotFound("Event not found.");

        return Ok(item);
    }

    /// <summary>Creates a new event. Organiser role required.</summary>
    /// <response code="201">Event created successfully.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged-in user is not an Organiser.</response>
    [HttpPost]
    [RequireRole("Organiser")]
    [ProducesResponseType(201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult CreateEvent(EventDto request)
    {
        if (request.EventType is not ("Run" or "Walk" or "Cycle"))
            return BadRequest("EventType must be Run, Walk or Cycle.");

        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var item = new Event
        {
            Name = request.Name,
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location,
            Distance = request.Distance,
            EventType = request.EventType,
            OrganiserId = organiserId
        };

        _context.Events.Add(item);
        _context.SaveChanges();

        return CreatedAtAction(nameof(GetEvent), new { id = item.EventId }, new
        {
            item.EventId,
            item.Name,
            item.EventDate,
            item.Location,
            item.Distance,
            item.EventType,
            item.OrganiserId
        });
    }

    /// <summary>Updates an event the logged-in Organiser owns.</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id}")]
    [RequireRole("Organiser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult UpdateEvent(int id, EventDto request)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var item = _context.Events.Find(id);
        if (item is null) return NotFound("Event not found.");
        if (item.OrganiserId != organiserId)
            return StatusCode(403, "You can only edit your own events.");

        item.Name = request.Name;
        item.Description = request.Description;
        item.EventDate = request.EventDate;
        item.Location = request.Location;
        item.Distance = request.Distance;
        item.EventType = request.EventType;
        _context.SaveChanges();

        return Ok("Event updated.");
    }

    /// <summary>Deletes an event the logged-in Organiser owns.</summary>
    /// <response code="200">Event deleted.</response>
    /// <response code="400">Event still has enrolments.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    [HttpDelete("{id}")]
    [RequireRole("Organiser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public IActionResult DeleteEvent(int id)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var item = _context.Events.Find(id);
        if (item is null) return NotFound("Event not found.");
        if (item.OrganiserId != organiserId)
            return StatusCode(403, "You can only delete your own events.");

        // Deletes are Restrict, so clear children first.
        if (_context.Enrolments.Any(e => e.EventId == id))
            return BadRequest("Cannot delete an event that has enrolments.");

        var categories = _context.Categories.Where(c => c.EventId == id);
        _context.Categories.RemoveRange(categories);
        _context.Events.Remove(item);
        _context.SaveChanges();

        return Ok("Event deleted.");
    }
}