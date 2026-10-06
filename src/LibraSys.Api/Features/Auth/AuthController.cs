using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraSys.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Username and password are required." });
        }

        var response = await authService.LoginAsync(request, cancellationToken);
        return response is null
            ? Unauthorized(new { error = "Invalid username or password." })
            : Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<RegisteredMember>> Register(
        RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length is < 3 or > 80 ||
            string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12 ||
            string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Length > 80 ||
            string.IsNullOrWhiteSpace(request.LastName) || request.LastName.Length > 80 ||
            string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 255 ||
            request.ContactInfo?.Length > 100)
        {
            return BadRequest(new { error = "Username, name, email, and a password of at least 12 characters are required." });
        }

        try
        {
            var result = await authService.RegisterAsync(request with
            {
                Username = request.Username.Trim(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email.Trim(),
                ContactInfo = string.IsNullOrWhiteSpace(request.ContactInfo) ? null : request.ContactInfo.Trim()
            }, cancellationToken);
            return result is null
                ? Problem("The Member application role is not configured.", statusCode: StatusCodes.Status500InternalServerError)
                : Created("/api/member/profile", result);
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == 1062)
        {
            return Conflict(new { error = "That username or email address is already registered." });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        userId = User.FindFirst("sub")?.Value,
        username = User.Identity?.Name,
        role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
    });
}

