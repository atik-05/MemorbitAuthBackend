# Authentication in ASP.NET Core Web API with Identity

**Memorbit** is an all-in-one personal workspace for capturing, organizing, and managing everyday life. Here, we will be focusing on Authentication part.

We've followed Clean Architecture + Vertical Slice layout, and the authentication system designed around one specific product decision: **let people use the app before they trust it with an email address.**


### How a guest identity works

A guest is a real row in the `AspNetUsers` table, created the moment someone hits `POST /api/auth/guest`:

```csharp
var userId = Guid.NewGuid();

var user = new ApplicationUser
{
    Id = userId,
    UserName = GuestDefaults.GenerateGuestUsername(userId), // guest-f47ac10b...
    Email    = GuestDefaults.GenerateGuestEmail(userId),    // f47ac10b...@guest.memorbit.local
    IsGuest  = true,
    CreatedAtUtc = now,
    UpdatedAtUtc = now
};

await _userManager.CreateAsync(user); // no password — Identity allows this
```

The user gets back a real access/refresh token pair immediately and starts creating data that's tied to a real, permanent `userId`

### Upgrading without losing data

When a guest decides to "become" a real user, the upgrade endpoint reuses the exact same `Guid`. The moment an account is upgraded, every refresh token issued during the guest phase is revoked and the user's security stamp is rotated.

Abandoned guest accounts don't linger forever either. A background `RefreshTokenCleanupService` runs every 24 hours and removes guest users who never came back and have no active session, so the `Users` table doesn't fill up with one-time visitors.

---

## Refresh tokens: rotation, replay detection, and fixing race condition

Refresh tokens are opaque, cryptographically random strings (64 bytes from `RandomNumberGenerator`), stored server-side as a SHA-256 hash — never the raw value. Each token belongs to a **family** (`FamilyId`), which is what makes replay detection possible.

**The rotation chain:** every time a refresh token is used, it's revoked and a new one is issued in the same family. If a *revoked* token is ever presented again, that's a signal the token was stolen and already used by someone else — so the entire family is burned, killing every session descended from it:

```csharp
if (existing.IsRevoked)
{
    await RevokeFamilyAsync(existing.FamilyId, now, ct);
    return AuthError.RefreshTokenReused;
}
```

**The race condition:** early on, refresh was a plain read-check-write. Fire two refresh requests with the same token at nearly the same instant and both could pass the "is this token valid" check before either one wrote its revocation — two valid new token pairs from one old one.

The fix is a `Serializable` transaction around the whole read-validate-rotate sequence. Under `Serializable`, PostgreSQL detects the conflicting concurrent transactions and forces one to fail and retry rather than let both commit.

Stored the refresh token in an **HttpOnly, Secure, SameSite cookie**. JavaScript can't read the token, which mitigates XSS theft, and frontend never touches the refresh token

---

## Rate limiting on the auth surface

Login, register, and refresh are the endpoints someone would actually script against — password spraying, credential stuffing, token-guessing. They all share one strict policy using ASP.NET Core's built-in fixed-window limiter, partitioned per client IP:

```csharp
options.AddPolicy("auth-sensitive", httpContext =>
    RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 5
        }));
```

---

## User Lockout after failed login attempt

If user try to login with incorrect credentials for several times, the account is locked out (for 15 minutes). It prevents brute-force attacks.

```csharp
services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

        // ...
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
```

---


## Tech stack

- **.NET 10** / ASP.NET Core Web API
- **PostgreSQL** via Npgsql + EF Core
- **ASP.NET Core Identity** (`IdentityCore<ApplicationUser>` with `Guid` keys) for password hashing, lockout, and user storage
- **JWT** access tokens (`JsonWebTokenHandler`, HMAC-SHA256) + hashed refresh tokens with HttpOnly cookie
- Built-in **`RateLimiter`** middleware, no external dependency
- **Swashbuckle** for OpenAPI documentation


## Auth endpoints

| Method | Route | Auth | Notes |
|---|---|---|---|
| POST | `/api/auth/guest` | anonymous | creates a real, upgradeable user with no credentials |
| POST | `/api/auth/register` | anonymous | rate-limited |
| POST | `/api/auth/login` | anonymous | rate-limited |
| POST | `/api/auth/refresh` | anonymous | rate-limited, serializable transaction |
| POST | `/api/auth/guest/upgrade` | guest only | preserves `userId`, revokes guest sessions |
| POST | `/api/auth/logout` | required | revokes one session |
| POST | `/api/auth/logout-all` | required | revokes everything + rotates security stamp |
| GET | `/api/auth/me` | required | current profile |
| PATCH | `/api/auth/me` | required | update profile |
| GET | `/api/auth/sessions` | required | list active devices/sessions |
| DELETE | `/api/auth/sessions/{id}` | required | revoke one session by id |
