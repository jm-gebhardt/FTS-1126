using Gebhardt.StoreWare.Agv.Common.Contracts;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Messages;

/// <summary>
/// Command to start the status send scheduler
/// </summary>
public class RequestStatusSendSchedulerStart : CorrelatedBy<Guid>, IAgvId
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string AgvId { get; }

    /// <summary>
    /// Build message
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="agvId"></param>
    public RequestStatusSendSchedulerStart(Guid correlationId, string agvId)
    {
        CorrelationId = correlationId;
        AgvId = agvId;
    }
}