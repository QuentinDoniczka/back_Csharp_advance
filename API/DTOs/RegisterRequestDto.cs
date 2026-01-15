namespace BackBase.API.DTOs;

public record RegisterRequestDto(
    string Email,
    string Password,
    string FirstName,
    string LastName
);
