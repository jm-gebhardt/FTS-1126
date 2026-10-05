using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

namespace Gebhardt.Acc.TransportOrder.Messages.Factories;

/// <summary>
/// Factory for creating commands to update AGV system errors based on state comparison.
/// </summary>
public static class SystemAgvErrorFactory
{
    /// <summary>
    /// Creates an UpdateSystemAgvErrors instance based on the comparison of current errors and updated errors.
    /// </summary>
    /// <param name="agvCorrelationId">The AGV correlation ID.</param>
    /// <param name="currentErrors">Collection of existing AGV errors.</param>
    /// <param name="updatedErrors">Collection of new or updated AGV errors.</param>
    /// <param name="position">Current position of the AGV.</param>
    /// <returns>An UpdateSystemAgvErrors instance or null if no changes are required.</returns>
    public static UpdateSystemAgvErrors? CreateUpdateSystemAgvErrors(Guid agvCorrelationId, IReadOnlyCollection<AgvError> currentErrors, ICollection<AgvError> updatedErrors, AgvPosition position)
    {
        var errorsToAdd = updatedErrors.Where(e => !currentErrors.Any(c => c.IsSameError(e))).ToList();
        var errorsToResolve = currentErrors.Where(c => !updatedErrors.Any(u => u.IsSameError(c))).ToList();

        if (errorsToAdd.Count == 0 && errorsToResolve.Count == 0)
        {
            return null;
        }

        return new UpdateSystemAgvErrors(agvCorrelationId, errorsToAdd, errorsToResolve, position);
    }
}