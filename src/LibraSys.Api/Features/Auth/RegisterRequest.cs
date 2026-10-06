namespace LibraSys.Api.Features.Auth;

public sealed record RegisterRequest(
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string Email,
    string? ContactInfo);

public sealed record RegisteredMember(long MemberId, string Username);
