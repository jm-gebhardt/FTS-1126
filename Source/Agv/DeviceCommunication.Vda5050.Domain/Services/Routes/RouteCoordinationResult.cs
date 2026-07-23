using Gebhardt.Acc.Common.Domain.Models.Errors;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;

/// <summary>
/// The outcome of handling a Route message via <see cref="IRouteExecutionCoordinator"/>: what, if anything, the
/// caller should do against the VDA5050 order-execution machinery.
/// </summary>
[PublicAPI]
public abstract record RouteCoordinationResult;

/// <summary>
/// Nothing needs to be sent to the AGV.
/// </summary>
[PublicAPI]
public sealed record NoRouteAction : RouteCoordinationResult;

/// <summary>
/// The requested operation was rejected.
/// </summary>
/// <param name="Error">The reason for the rejection.</param>
[PublicAPI]
public sealed record RouteCoordinationFailed(Error Error) : RouteCoordinationResult;

/// <summary>
/// A fresh order should be started for the given, previously-unused orderId.
/// </summary>
/// <param name="RouteId">The route id, used as the stable across orderId rotations.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
/// <param name="OrderId">The newly minted orderId.</param>
/// <param name="Order">The order to start.</param>
[PublicAPI]
public sealed record StartRouteOrder(Guid RouteId, Guid AgvCorrelationId, Guid OrderId, Models.Orders.Order Order) : RouteCoordinationResult;

/// <summary>
/// An update should be sent for the currently active order. The receiving <c>OrderExecutionSaga</c> is
/// responsible for splicing this against its own live stitching node.
/// </summary>
/// <param name="RouteId">The route id, used as the stable <c>Agv.CurrentFleetManagerActionId</c> across orderId rotations.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
/// <param name="OrderId">The current, unchanged orderId.</param>
/// <param name="Order">The full, currently-known candidate order to splice against the live order.</param>
[PublicAPI]
public sealed record UpdateRouteOrder(Guid RouteId, Guid AgvCorrelationId, Guid OrderId, Models.Orders.Order Order) : RouteCoordinationResult;

/// <summary>
/// The currently active order should be cancelled.
/// </summary>
/// <param name="RouteId">The route id.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
/// <param name="OrderId">The orderId to cancel.</param>
[PublicAPI]
public sealed record CancelRouteOrder(Guid RouteId, Guid AgvCorrelationId, Guid OrderId) : RouteCoordinationResult;
