using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// AgvGraph was not found.
/// </summary>
public class AgvGraphNotFoundException : NotFoundException
{
    /// <summary>
    /// AgvGraph was not found for the given AGV correlation Id.
    /// </summary>
    /// <param name="agvCorrelationId">AGV correlation Id.</param>
    public AgvGraphNotFoundException(Guid agvCorrelationId)
        : base(nameof(Models.Graphs.AgvGraph), nameof(agvCorrelationId), agvCorrelationId)
    {
    }
}