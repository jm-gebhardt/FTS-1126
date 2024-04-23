using Gebhardt.Acc.TransportOrder.Messages.MessageContent;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Routing;

/// <summary>
/// Defines a result for the <see cref="GetRoute"/>
/// </summary>
/// <param name="Route">The returned route. Null if no route was found.</param>
public record GotRoute(Route? Route);