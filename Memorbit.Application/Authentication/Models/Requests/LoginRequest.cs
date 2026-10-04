using System.ComponentModel.DataAnnotations;

namespace Memorbit.Application.Authentication.Models.Requests
{
    public record LoginRequest(
        [Required] string EmailOrUsername,
        [Required] string Password,
        [MaxLength(200)] string? DeviceName);
}
