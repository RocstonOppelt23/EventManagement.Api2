using EventManagement.Api.Data;
using EventManagement.Api.Dtos;
using EventManagement.Api.Filters;
using EventManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _context;
    public ResultsController(AppDbContext context) => _context = context;

    /// <summary>Captures a result for an enrolment. Organiser role required.</summary>
    /// <response code="201">Result captured.</response>
    /// <response code="400">Enrolment not found or already has a result.</response>
    /// <response code="403">Not an Organiser, or not the owner of that event.</response>
    [HttpPost]
    [RequireRole("Organiser")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public IActionResult CaptureResult(ResultDto request)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var enrolment = _context.Enrolments
            .Where(e => e.EnrolmentId == request.EnrolmentId)
            .Select(e => new { e.EnrolmentId, e.Event!.OrganiserId })
            .SingleOrDefault();

        if (enrolment is null) return BadRequest("Enrolment not found.");
        if (enrolment.OrganiserId != organiserId)
            return StatusCode(403, "You can only capture results for your own events.");

        if (_context.Results.Any(r => r.EnrolmentId == request.EnrolmentId))
            return BadRequest("This enrolment already has a result.");

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position
        };

        _context.Results.Add(result);
        _context.SaveChanges();

        return CreatedAtAction(nameof(GetMyResults), null,
            new { result.ResultId, result.EnrolmentId, result.FinishTime, result.Position });
    }

    /// <summary>Returns the logged-in Participant's own results.</summary>
    /// <response code="200">Results returned.</response>
    /// <response code="403">Logged-in user is not a Participant.</response>
    [HttpGet("mine")]
    [RequireRole("Participant")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public IActionResult GetMyResults()
    {
        int participantId = HttpContext.Session.GetInt32("UserId")!.Value;

        // Filtered through the enrolment so a participant only ever sees their own.
        var results = _context.Results
            .Where(r => r.Enrolment!.ParticipantId == participantId)
            .Select(r => new
            {
                r.ResultId,
                Event = r.Enrolment!.Event!.Name,
                Category = r.Enrolment.Category!.Name,
                r.FinishTime,
                r.Position
            })
            .ToList();

        return Ok(results);
    }
}