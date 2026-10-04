namespace Memorbit.Application.Authentication.Models.Responses
{
    public record SessionResponse(
        Guid Id,
        string? DeviceName,
        DateTime CreatedAtUtc,
        DateTime ExpiresAtUtc,
        DateTime? LastUsedAtUtc,
        string? CreatedByIp);
}
