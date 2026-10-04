namespace Memorbit.Application.Authentication.Interfaces
{
    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }

        Guid UserId { get; }

        bool IsGuest { get; }
    }
}
