namespace SyrianStudyBot.Features.Auth.Dtos;

public record RegisterRequest(string FirstName, string LastName, string PhoneNumber, string Email, string Password);
