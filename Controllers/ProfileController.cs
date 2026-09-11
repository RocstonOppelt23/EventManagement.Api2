using EventManagement.Api.Data;
using EventManagement.Api.Dtos;
using EventManagement.Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;
    public ProfileController(AppDbContext context) => _context = context;

    /// <summary>Returns the logged-in user's own profile.</summary>
    /// <response code="200">Profile returned.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    [RequireLogin]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult GetProfile()
    {
        // The user id comes from the session, never from the client.
        int userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var user = _context.Users
            .Where(u => u.UserId == userId)
            .Select(u => new { u.UserId, u.FullName, u.Email, u.Role, u.PhoneNumber })
            .SingleOrDefault();

        if (user is null) return NotFound("Profile not found.");

        return Ok(user);
    }

    /// <summary>Updates the logged-in user's own profile.</summary>
    /// <response code="200">Profile updated.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut]
    [RequireLogin]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult UpdateProfile(UpdateProfileDto request)
    {
        int userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var user = _context.Users.Find(userId);
        if (user is null) return NotFound("Profile not found.");

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        _context.SaveChanges();

        return Ok(new { user.UserId, user.FullName, user.Email, user.Role, user.PhoneNumber });
    }
}