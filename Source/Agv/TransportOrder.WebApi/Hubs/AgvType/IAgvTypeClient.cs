using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvType;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.AgvType;

/// <summary>
/// Provides client methods for agv-type-related events.
/// </summary>
[SignalRHub(AgvHub.UrlPath, documentNames: new[] { HubApiGroup.GroupName })]
public interface IAgvTypeClient
{
    /// <summary>
    /// Agv type has been acknowledged.
    /// </summary>
    /// <param name="agvTypeAcknowledgement">The agv type acknowledgement.</param>
    [SignalRMethod(nameof(AgvTypeAcknowledged))]
    Task AgvTypeAcknowledged(AgvTypeAcknowledgedDto agvTypeAcknowledgement);

    /// <summary>
    /// A new agv type has been created.
    /// </summary>
    /// <param name="agvType">The created agv type.</param>
    [SignalRMethod(nameof(AgvTypeCreated))]
    Task AgvTypeCreated(AgvTypeDto agvType);

    /// <summary>
    /// Agv type has been updated.
    /// </summary>
    /// <param name="agvTypeUpdatedMessage">The agv type updated message.</param>
    [SignalRMethod(nameof(AgvTypeUpdated))]
    Task AgvTypeUpdated(AgvTypeUpdatedDto agvTypeUpdatedMessage);
}