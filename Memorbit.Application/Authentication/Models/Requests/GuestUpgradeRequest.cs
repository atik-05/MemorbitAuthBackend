using System.ComponentModel.DataAnnotations;

namespace Memorbit.Application.Authentication.Models.Requests
{
    public record GuestUpgradeRequest(
        [Required, EmailAddress, MaxLength(256)] string Email,
        [Required, MinLength(3), MaxLength(50)] string Username,
        [Required, MinLength(8), MaxLength(100)] string Password);
}
