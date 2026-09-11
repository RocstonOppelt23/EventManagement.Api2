using EventManagement.Api.Data;
using EventManagement.Api.Dtos;
using EventManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    public AuthController(AppDbContext context) => _context = context;

    /// <summary>Registers a new Organiser or Participant.</summary>
    /// <response code="200">Registration successful.</response>
    /// <response code="400">Invalid role or email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public IActionResult Register(RegisterDto request)
    {
        if (request.Role is not ("Organiser" or "Participant"))
            return BadRequest("Role must be Organiser or Participant.");

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email and password are required.");

        if (_context.Users.Any(u => u.Email == request.Email))
            return BadRequest("Email already registered.");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            PhoneNumber = request.PhoneNumber
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return Ok("Registration successful.");
    }

    /// <summary>Logs in and starts a session holding UserId and Role.</summary>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult Login(LoginDto request)
    {
        var user = _context.Users.SingleOrDefault(u => u.Email == request.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);

        return Ok(new { user.UserId, user.FullName, user.Role });
    }

    /// <summary>Clears the current session.</summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok("Logged out.");
    }
}