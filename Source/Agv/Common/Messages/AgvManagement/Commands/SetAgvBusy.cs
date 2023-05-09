using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands
{
    /// <summary>
    /// Request an AGV to transition to busy state
    /// </summary>
    /// <param name="CorrelationId">The Correlation of the Agv</param>
    [JetBrains.Annotations.PublicAPI]
    public record SetAgvBusy(Guid CorrelationId) : CorrelatedBy<Guid>;
}