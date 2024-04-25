using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Services;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Exceptions;

/// <summary>
/// Agv Information was not found
/// </summary>
[PublicAPI]
public class AgvInformationNotFoundException : NotFoundException
{
    /// <summary>
    /// Agv Information was not found for Agv with Agv correlationId
    /// </summary>
    /// <param name="agvCorrelationId">Correlation Id of Agv</param>
    public AgvInformationNotFoundException(Guid agvCorrelationId)
        : base(nameof(AgvInformation), nameof(agvCorrelationId), agvCorrelationId)
    {
    }
}