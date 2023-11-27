using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Messages;

/// <summary>
/// Simulate agv error in simulated agv.
/// </summary>
[PublicAPI]
public class SimulateAgvError : CorrelatedBy<Guid>
{
    /// <summary>
    /// Correlation Id of the agv.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// List of errors. If empty it will remove any existing errors.
    /// </summary>
    public List<AgvError> AgvErrors { get; set; } = new();

    /// <inheritdoc cref="SimulateAgvError"/>
    /// <param name="correlationId">Correlation Id of the agv.</param>
    /// <param name="agvErrors">List of errors. If empty it will remove any existing errors.</param>
    public SimulateAgvError(Guid correlationId, List<AgvError>? agvErrors)
    {
        CorrelationId = correlationId;

        if (agvErrors != null)
        {
            AgvErrors = agvErrors;
        }
    }
}