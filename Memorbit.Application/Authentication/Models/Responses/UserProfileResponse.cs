namespace Memorbit.Application.Authentication.Models.Responses
{
    public record UserProfileResponse(
        Guid UserId,
        bool IsGuest,
        string? Email,
        string? Username,
        string? DisplayName,
        string? PhoneNumber,
        DateTime CreatedAtUtc,
        DateTime? UpgradedAtUtc);
}
