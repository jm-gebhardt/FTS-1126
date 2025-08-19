using System.Diagnostics.CodeAnalysis;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Services;

/// <summary>
/// Defines an interface for validating AGV dispatch limitations in AKL station groups.
/// </summary>
public interface IAklAgvLimitGuard
{
    /// <summary>
    /// Determines if the specified HostOrder fits inside the AGV dispatch limitations for AKL station groups.
    /// </summary>
    /// <param name="hostOrder">The host order which should be checked.</param>
    /// <returns>True if it's inside the AKL limitations, otherwise false.</returns>
    Task<bool> IsWithinAklAgvLimitAsync([NotNullWhen(true)] HostOrder? hostOrder);
}