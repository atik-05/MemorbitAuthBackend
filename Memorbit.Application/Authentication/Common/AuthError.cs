namespace Memorbit.Application.Authentication.Common
{
    public enum AuthErrorType
    {
        Validation,
        Unauthorized,
        Forbidden,
        NotFound,
        Conflict,
        Locked
    }

    public sealed record AuthError(string Code, string Message, AuthErrorType Type)
    {
        public static readonly AuthError InvalidCredentials = new(
            "invalid_credentials",
            "The provided credentials are invalid.",
            AuthErrorType.Unauthorized);

        public static readonly AuthError AccountLocked = new(
            "account_locked",
            "This account is temporarily locked due to multiple failed sign-in attempts.",
            AuthErrorType.Locked);

        public static readonly AuthError EmailInUse = new(
            "email_in_use",
            "This email address is already registered.",
            AuthErrorType.Conflict);

        public static readonly AuthError UsernameInUse = new(
            "username_in_use",
            "This username is already taken.",
            AuthErrorType.Conflict);

        public static readonly AuthError ReservedEmailDomain = new(
            "reserved_email_domain",
            "This email domain is reserved for internal use.",
            AuthErrorType.Validation);

        public static readonly AuthError UserNotFound = new(
            "user_not_found",
            "User not found.",
            AuthErrorType.NotFound);

        public static readonly AuthError InvalidRefreshToken = new(
            "invalid_refresh_token",
            "The refresh token is invalid, expired, or has already been used.",
            AuthErrorType.Unauthorized);

        public static readonly AuthError RefreshTokenReused = new(
            "refresh_token_reused",
            "This refresh token has already been used. All sessions on this device have been revoked for your safety.",
            AuthErrorType.Unauthorized);

        public static readonly AuthError NotGuestAccount = new(
            "not_guest_account",
            "This operation is only valid for guest accounts.",
            AuthErrorType.Forbidden);

        public static readonly AuthError AlreadyUpgraded = new(
            "already_upgraded",
            "This account has already been upgraded to a full account.",
            AuthErrorType.Conflict);

        public static readonly AuthError SessionNotFound = new(
            "session_not_found",
            "Session not found.",
            AuthErrorType.NotFound);

        public static AuthError IdentityFailure(string description) =>
            new("identity_error", description, AuthErrorType.Validation);
    }
}
