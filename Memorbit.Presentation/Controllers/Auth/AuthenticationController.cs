using Memorbit.Application.Authentication.Common;
using Memorbit.Application.Authentication.Interfaces;
using Memorbit.Application.Authentication.Models.Requests;
using Memorbit.Presentation.Authentication;
using Memorbit.Presentation.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Memorbit.Presentation.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ICurrentUser _currentUser;
    private readonly RefreshTokenCookieService _refreshTokenCookieService;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        ICurrentUser currentUser,
        RefreshTokenCookieService refreshTokenCookieService)
    {
        _authenticationService = authenticationService;
        _currentUser = currentUser;
        _refreshTokenCookieService = refreshTokenCookieService;
    }

    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpPost("guest")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-guest")]
    public async Task<IActionResult> Guest(
        CreateGuestRequest? request,
        CancellationToken ct)
    {
        var result = await _authenticationService.CreateGuestAsync(request ?? new CreateGuestRequest(null), ClientIp, ct);
        return result.ToAuthActionResult(Response, _refreshTokenCookieService);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken ct)
    {
        var result = await _authenticationService.RegisterAsync(request, ClientIp, ct);
        return result.ToAuthActionResult(Response, _refreshTokenCookieService);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken ct)
    {
        var result = await _authenticationService.LoginAsync(request, ClientIp, ct);
        return result.ToAuthActionResult(Response, _refreshTokenCookieService);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!_refreshTokenCookieService.TryGet(Request, out var refreshToken))
        {
            return AuthError.InvalidRefreshToken.ToProblemResult();
        }

        var result = await _authenticationService.RefreshAsync(refreshToken, ClientIp, ct);
        return result.ToAuthActionResult(Response, _refreshTokenCookieService);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (_refreshTokenCookieService.TryGet(Request, out var refreshToken))
        {
            await _authenticationService.LogoutAsync(_currentUser.UserId, refreshToken, ct);
        }

        _refreshTokenCookieService.Clear(Response);
        return NoContent();
    }

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var result = await _authenticationService.LogoutAllAsync(_currentUser.UserId, ct);
        if (!result.IsSuccess)
        {
            return result.ToActionResult();
        }

        _refreshTokenCookieService.Clear(Response);
        return NoContent();
    }

    [HttpPost("guest/upgrade")]
    [Authorize(Policy = AuthorizationPolicyNames.GuestUser)]
    public async Task<IActionResult> UpgradeGuest(
        GuestUpgradeRequest request,
        CancellationToken ct)
    {
        var result = await _authenticationService.UpgradeGuestAsync(_currentUser.UserId, request, ClientIp, ct);
        return result.ToAuthActionResult(Response, _refreshTokenCookieService);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await _authenticationService.GetProfileAsync(_currentUser.UserId, ct);
        return result.ToActionResult();
    }

    [HttpPatch("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe(
        UpdateProfileRequest request,
        CancellationToken ct)
    {
        var result = await _authenticationService.UpdateProfileAsync(_currentUser.UserId, request, ct);
        return result.ToActionResult();
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> Sessions(CancellationToken ct)
    {
        var result = await _authenticationService.GetSessionsAsync(_currentUser.UserId, ct);
        return result.ToActionResult();
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    [Authorize]
    public async Task<IActionResult> RevokeSession(
        Guid sessionId,
        CancellationToken ct)
    {
        var result = await _authenticationService.RevokeSessionAsync(_currentUser.UserId, sessionId, ct);
        return result.ToActionResult();
    }
}



