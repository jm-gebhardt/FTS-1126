using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to request to refresh agv information.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="RefreshType">The information to refresh (data, zones, graph)</param>
public record RefreshAgvInformation(Guid CorrelationId, RefreshType RefreshType) : CorrelatedBy<Guid>;

/// <summary>
/// The type of agv information to refresh.
/// </summary>
public enum RefreshType
{
    /// <summary>
    ///  Agv data.
    /// </summary>
    Data,

    /// <summary>
    ///  Agv graph.
    /// </summary>
    Graph,

    /// <summary>
    ///  Agv zones.
    /// </summary>
    Zones
}