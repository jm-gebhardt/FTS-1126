using System.Diagnostics.CodeAnalysis;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Station;

/// <summary>
/// Defines an interface for validating AGV dispatch limitations in station groups.
/// </summary>
public interface IStationAgvLimitGuard
{
    /// <summary>
    /// Determines if the specified action group fits inside the AGV dispatch limitations for station groups.
    /// </summary>
    /// <param name="actionGroup">The action group which should be checked.</param>
    /// <returns>True if it's inside the stations limitations, otherwise false.</returns>
    Task<bool> IsWithinLimitAsync([NotNullWhen(true)] Models.ActionGroup.ActionGroup? actionGroup);
}