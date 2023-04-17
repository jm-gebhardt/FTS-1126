using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Exceptions;

/// <summary>
/// Agv Information was not found
/// </summary>
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