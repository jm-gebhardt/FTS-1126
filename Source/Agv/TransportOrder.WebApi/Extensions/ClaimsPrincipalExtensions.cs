using System.Security.Claims;

namespace Gebhardt.Acc.TransportOrder.WebApi.Extensions;

/// <summary>
/// Extensions for <see cref="ClaimsPrincipal"/>.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Get the email address of the principal.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal.</param>
    /// <returns></returns>
    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        var emailClaim = principal.FindFirst(ClaimTypes.Email);
        return emailClaim?.Value;
    }
}