using Memorbit.Application.Authentication.Common;
using Memorbit.Application.Authentication.Models;
using Memorbit.Application.Authentication.Models.Requests;
using Memorbit.Application.Authentication.Models.Responses;

namespace Memorbit.Application.Authentication.Interfaces
{
    public interface IAuthenticationService
    {
        Task<Result<IssuedAuthResult>> CreateGuestAsync(
            CreateGuestRequest request,
            string? ipAddress,
            CancellationToken ct = default);

        Task<Result<IssuedAuthResult>> RegisterAsync(
            RegisterRequest request,
            string? ipAddress,
            CancellationToken ct = default);

        Task<Result<IssuedAuthResult>> LoginAsync(
            LoginRequest request,
            string? ipAddress,
            CancellationToken ct = default);

        Task<Result<IssuedAuthResult>> RefreshAsync(
            string refreshToken,
            string? ipAddress,
            CancellationToken ct = default);

        Task<Result> LogoutAsync(
            Guid userId,
            string refreshToken,
            CancellationToken ct = default);

        Task<Result> LogoutAllAsync(
            Guid userId,
            CancellationToken ct = default);

        Task<Result<IssuedAuthResult>> UpgradeGuestAsync(
            Guid userId,
            GuestUpgradeRequest request,
            string? ipAddress,
            CancellationToken ct = default);

        Task<Result<UserProfileResponse>> GetProfileAsync(
            Guid userId,
            CancellationToken ct = default);

        Task<Result<UserProfileResponse>> UpdateProfileAsync(
            Guid userId,
            UpdateProfileRequest request,
            CancellationToken ct = default);

        Task<Result<IReadOnlyCollection<SessionResponse>>> GetSessionsAsync(
            Guid userId,
            CancellationToken ct = default);

        Task<Result> RevokeSessionAsync(
            Guid userId,
            Guid sessionId,
            CancellationToken ct = default);
    }
}
