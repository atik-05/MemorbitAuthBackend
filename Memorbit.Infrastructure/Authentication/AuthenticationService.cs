using System.Data;
using Memorbit.Application.Authentication.Common;
using Memorbit.Application.Authentication.Interfaces;
using Memorbit.Application.Authentication.Models;
using Memorbit.Application.Authentication.Models.Requests;
using Memorbit.Application.Authentication.Models.Responses;
using Memorbit.Domain.Authentication;
using Memorbit.Infrastructure.Identity;
using Memorbit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Memorbit.Infrastructure.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db,
        ITokenService tokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<Result<IssuedAuthResult>> CreateGuestAsync(
        CreateGuestRequest request,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = GuestDefaults.GenerateGuestUsername(userId),
            Email = GuestDefaults.GenerateGuestEmail(userId),
            IsGuest = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            return Result<IssuedAuthResult>.Failure(ToAuthError(createResult));
        }

        return await IssueTokensAsync(user, request.DeviceName, ipAddress, ct);
    }

    public async Task<Result<IssuedAuthResult>> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        CancellationToken ct = default)
    {
        if (GuestDefaults.IsReservedGuestEmail(request.Email))
        {
            return Result<IssuedAuthResult>.Failure(AuthError.ReservedEmailDomain);
        }

        if (await _userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Result<IssuedAuthResult>.Failure(AuthError.EmailInUse);
        }

        if (await _userManager.FindByNameAsync(request.Username) is not null)
        {
            return Result<IssuedAuthResult>.Failure(AuthError.UsernameInUse);
        }

        var now = DateTime.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Username,
            Email = request.Email,
            IsGuest = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return Result<IssuedAuthResult>.Failure(ToAuthError(createResult));
        }

        return await IssueTokensAsync(user, request.DeviceName, ipAddress, ct);
    }

    public async Task<Result<IssuedAuthResult>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByNameAsync(request.EmailOrUsername)
            ?? await _userManager.FindByEmailAsync(request.EmailOrUsername);

        // Guests have no password; treat them the same as "no such user" to avoid
        // leaking account existence and to force them through the upgrade flow.
        if (user is null || user.IsGuest)
        {
            return Result<IssuedAuthResult>.Failure(AuthError.InvalidCredentials);
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            return Result<IssuedAuthResult>.Failure(AuthError.AccountLocked);
        }

        if (!signInResult.Succeeded)
        {
            return Result<IssuedAuthResult>.Failure(AuthError.InvalidCredentials);
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await IssueTokensAsync(user, request.DeviceName, ipAddress, ct);
    }

    public async Task<Result<IssuedAuthResult>> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var existing = await _db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == tokenHash, ct);
        if (existing is null)
        {
            return Result<IssuedAuthResult>.Failure(AuthError.InvalidRefreshToken);
        }

        var now = DateTime.UtcNow;

        if (existing.IsRevoked)
        {
            // A revoked token being presented again means it was stolen and replayed:
            // burn the whole device family so the legitimate holder is forced to re-authenticate.
            await RevokeFamilyAsync(existing.FamilyId, now, ct);
            await transaction.CommitAsync(ct);
            return Result<IssuedAuthResult>.Failure(AuthError.RefreshTokenReused);
        }

        if (existing.IsExpiredAt(now))
        {
            return Result<IssuedAuthResult>.Failure(AuthError.InvalidRefreshToken);
        }

        var user = await _userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null)
        {
            return Result<IssuedAuthResult>.Failure(AuthError.InvalidRefreshToken);
        }

        if (!string.Equals(existing.SecurityStamp, user.SecurityStamp, StringComparison.Ordinal))
        {
            // Security stamp changed (logout-all, upgrade, password change) since this token was issued.
            return Result<IssuedAuthResult>.Failure(AuthError.InvalidRefreshToken);
        }

        existing.RevokedAtUtc = now;

        var newRefreshTokenPlain = _tokenService.CreateRefreshToken();
        var refreshTokenDays = user.IsGuest ? _jwtOptions.GuestRefreshTokenDays : _jwtOptions.RefreshTokenDays;

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = existing.FamilyId,
            TokenHash = _tokenService.HashToken(newRefreshTokenPlain),
            SecurityStamp = user.SecurityStamp!,
            DeviceName = existing.DeviceName,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(refreshTokenDays),
            CreatedByIp = ipAddress,
            LastUsedAtUtc = now
        };

        existing.ReplacedByTokenId = newRefreshToken.Id;

        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.CreateAccessToken(user.Id, user.IsGuest, user.SecurityStamp!, roles.ToList());

        return Result<IssuedAuthResult>.Success(new IssuedAuthResult(
            new AuthResponse(
                user.Id,
                user.IsGuest,
                accessToken.Token,
                accessToken.ExpiresAtUtc,
                newRefreshToken.ExpiresAtUtc),
            newRefreshTokenPlain));
    }

    public async Task<Result> LogoutAsync(
        Guid userId,
        string refreshToken,
        CancellationToken ct = default)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);

        var token = await _db.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash && x.UserId == userId, ct);

        // Idempotent: an already-gone or already-revoked token is still a successful logout.
        if (token is not null && !token.IsRevoked)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    public async Task<Result> LogoutAllAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(AuthError.UserNotFound);
        }

        var now = DateTime.UtcNow;

        var activeTokens = await _db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
        }

        await _userManager.UpdateSecurityStampAsync(user);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<IssuedAuthResult>> UpgradeGuestAsync(
        Guid userId,
        GuestUpgradeRequest request,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<IssuedAuthResult>.Failure(AuthError.UserNotFound);
        }

        if (!user.IsGuest)
        {
            return Result<IssuedAuthResult>.Failure(AuthError.AlreadyUpgraded);
        }

        if (GuestDefaults.IsReservedGuestEmail(request.Email))
        {
            return Result<IssuedAuthResult>.Failure(AuthError.ReservedEmailDomain);
        }

        var existingEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingEmail is not null && existingEmail.Id != user.Id)
        {
            return Result<IssuedAuthResult>.Failure(AuthError.EmailInUse);
        }

        var existingUsername = await _userManager.FindByNameAsync(request.Username);
        if (existingUsername is not null && existingUsername.Id != user.Id)
        {
            return Result<IssuedAuthResult>.Failure(AuthError.UsernameInUse);
        }

        var setEmailResult = await _userManager.SetEmailAsync(user, request.Email);
        if (!setEmailResult.Succeeded)
        {
            return Result<IssuedAuthResult>.Failure(ToAuthError(setEmailResult));
        }

        var setUsernameResult = await _userManager.SetUserNameAsync(user, request.Username);
        if (!setUsernameResult.Succeeded)
        {
            return Result<IssuedAuthResult>.Failure(ToAuthError(setUsernameResult));
        }

        var addPasswordResult = await _userManager.AddPasswordAsync(user, request.Password);
        if (!addPasswordResult.Succeeded)
        {
            return Result<IssuedAuthResult>.Failure(ToAuthError(addPasswordResult));
        }

        user.IsGuest = false;
        user.EmailConfirmed = false;
        user.UpgradedAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Revokes every session created while this account was a guest and rotates the
        // security stamp, so any leaked guest refresh token stops working immediately.
        await LogoutAllAsync(user.Id, ct);

        var upgradedUser = await _userManager.FindByIdAsync(user.Id.ToString()) ?? user;

        return await IssueTokensAsync(upgradedUser, deviceName: null, ipAddress, ct);
    }

    public async Task<Result<UserProfileResponse>> GetProfileAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<UserProfileResponse>.Failure(AuthError.UserNotFound);
        }

        return Result<UserProfileResponse>.Success(ToProfileResponse(user));
    }

    public async Task<Result<UserProfileResponse>> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<UserProfileResponse>.Failure(AuthError.UserNotFound);
        }

        if (request.DisplayName is not null)
        {
            user.DisplayName = request.DisplayName;
        }

        if (request.PhoneNumber is not null)
        {
            var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
            if (!setPhoneResult.Succeeded)
            {
                return Result<UserProfileResponse>.Failure(ToAuthError(setPhoneResult));
            }
        }

        user.UpdatedAtUtc = DateTime.UtcNow;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result<UserProfileResponse>.Failure(ToAuthError(updateResult));
        }

        return Result<UserProfileResponse>.Success(ToProfileResponse(user));
    }

    public async Task<Result<IReadOnlyCollection<SessionResponse>>> GetSessionsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var sessions = await _db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new SessionResponse(
                x.Id,
                x.DeviceName,
                x.CreatedAtUtc,
                x.ExpiresAtUtc,
                x.LastUsedAtUtc,
                x.CreatedByIp))
            .ToListAsync(ct);

        return Result<IReadOnlyCollection<SessionResponse>>.Success(sessions);
    }

    public async Task<Result> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken ct = default)
    {
        var token = await _db.RefreshTokens
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, ct);

        if (token is null)
        {
            return Result.Failure(AuthError.SessionNotFound);
        }

        if (!token.IsRevoked)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken ct)
    {
        var tokens = await _db.RefreshTokens
            .Where(x => x.FamilyId == familyId && x.RevokedAtUtc == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.RevokedAtUtc = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<Result<IssuedAuthResult>> IssueTokensAsync(
        ApplicationUser user,
        string? deviceName,
        string? ipAddress,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.CreateAccessToken(user.Id, user.IsGuest, user.SecurityStamp!, roles.ToList());

        var refreshTokenPlain = _tokenService.CreateRefreshToken();
        var refreshTokenDays = user.IsGuest ? _jwtOptions.GuestRefreshTokenDays : _jwtOptions.RefreshTokenDays;

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(refreshTokenPlain),
            SecurityStamp = user.SecurityStamp!,
            DeviceName = deviceName,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(refreshTokenDays),
            CreatedByIp = ipAddress,
            LastUsedAtUtc = now
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);

        return Result<IssuedAuthResult>.Success(new IssuedAuthResult(
            new AuthResponse(
                user.Id,
                user.IsGuest,
                accessToken.Token,
                accessToken.ExpiresAtUtc,
                refreshToken.ExpiresAtUtc),
            refreshTokenPlain));
    }

    private static AuthError ToAuthError(IdentityResult result)
    {
        var description = string.Join(" ", result.Errors.Select(e => e.Description));
        return AuthError.IdentityFailure(string.IsNullOrWhiteSpace(description) ? "The operation failed." : description);
    }

    private static UserProfileResponse ToProfileResponse(ApplicationUser user) => new(
        user.Id,
        user.IsGuest,
        user.IsGuest ? null : user.Email,
        user.IsGuest ? null : user.UserName,
        user.DisplayName,
        user.PhoneNumber,
        user.CreatedAtUtc,
        user.UpgradedAtUtc);
}
