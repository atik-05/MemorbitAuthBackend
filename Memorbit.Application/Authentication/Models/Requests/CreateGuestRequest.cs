using System.ComponentModel.DataAnnotations;

namespace Memorbit.Application.Authentication.Models.Requests
{
    public record CreateGuestRequest(
        [MaxLength(200)] string? DeviceName);
}
