namespace EventManagement.Api.Dtos;

// Auth
public record RegisterDto(string FullName, string Email, string Password, string Role, string? PhoneNumber);
public record LoginDto(string Email, string Password);

// Profile
public record UpdateProfileDto(string FullName, string? PhoneNumber);

// Events
public record EventDto(
    string Name,
    string? Description,
    DateTime EventDate,
    string Location,
    decimal Distance,
    string EventType);

// Categories
public record CategoryDto(string Name);

// Enrolments
public record EnrolmentDto(int EventId, int CategoryId);

// Results
public record ResultDto(int EnrolmentId, TimeSpan FinishTime, int Position);