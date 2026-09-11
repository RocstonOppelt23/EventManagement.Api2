using EventManagement.Api.Data;
using EventManagement.Api.Dtos;
using EventManagement.Api.Filters;
using EventManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
public class EnrolmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    public EnrolmentsController(AppDbContext context) => _context = context;

    /// <summary>Enrols the logged-in Participant in an event and category.</summary>
    /// <response code="200">Enrolment stored.</response>
    /// <response code="400">Already enrolled, or invalid event/category.</response>
    /// <response code="403">Logged-in user is not a Participant.</response>
    [HttpPost("api/enrolments")]
    [RequireRole("Participant")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public IActionResult Enrol(EnrolmentDto request)
    {
        int participantId = HttpContext.Session.GetInt32("UserId")!.Value;

        if (!_context.Events.Any(e => e.EventId == request.EventId))
            return BadRequest("Event not found.");

        // The category must belong to the event being entered.
        bool categoryValid = _context.Categories
            .Any(c => c.CategoryId == request.CategoryId && c.EventId == request.EventId);
        if (!categoryValid)
            return BadRequest("That category does not belong to this event.");

        bool duplicate = _context.Enrolments
            .Any(e => e.ParticipantId == participantId && e.EventId == request.EventId);
        if (duplicate)
            return BadRequest("You are already enrolled in this event.");

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId = request.EventId,
            CategoryId = request.CategoryId,
            EnrolmentDate = DateTime.Now
        };

        _context.Enrolments.Add(enrolment);
        _context.SaveChanges();

        return Ok(new
        {
            enrolment.EnrolmentId,
            enrolment.ParticipantId,
            enrolment.EventId,
            enrolment.CategoryId,
            enrolment.EnrolmentDate
        });
    }

    /// <summary>Lists everyone enrolled in one event. Organiser role required.</summary>
    /// <response code="200">Enrolments returned.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{eventId}/enrolments")]
    [RequireRole("Organiser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult GetEventEnrolments(int eventId)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var ev = _context.Events.Find(eventId);
        if (ev is null) return NotFound("Event not found.");
        if (ev.OrganiserId != organiserId)
            return StatusCode(403, "You can only view enrolments for your own events.");

        var enrolments = _context.Enrolments
            .Where(e => e.EventId == eventId)
            .Select(e => new
            {
                e.EnrolmentId,
                Participant = e.Participant!.FullName,
                e.ParticipantId,
                Category = e.Category!.Name,
                e.EnrolmentDate
            })
            .ToList();

        return Ok(enrolments);
    }

    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    [HttpGet("api/enrolments/mine")]
    [RequireRole("Participant")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public IActionResult GetMyEnrolments()
    {
        int participantId = HttpContext.Session.GetInt32("UserId")!.Value;

        var enrolments = _context.Enrolments
            .Where(e => e.ParticipantId == participantId)
            .Select(e => new
            {
                e.EnrolmentId,
                Event = e.Event!.Name,
                e.EventId,
                Category = e.Category!.Name,
                e.EnrolmentDate
            })
            .ToList();

        return Ok(enrolments);
    }
}