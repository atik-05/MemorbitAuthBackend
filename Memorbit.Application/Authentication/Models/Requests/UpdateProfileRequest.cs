using System.ComponentModel.DataAnnotations;

namespace Memorbit.Application.Authentication.Models.Requests
{
    public record UpdateProfileRequest(
        [MaxLength(100)] string? DisplayName,
        [Phone, MaxLength(30)] string? PhoneNumber);
}
