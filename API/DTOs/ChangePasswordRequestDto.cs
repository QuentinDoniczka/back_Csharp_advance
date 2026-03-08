namespace BackBase.API.DTOs;

public record ChangePasswordRequestDto(string CurrentPassword, string NewPassword);
