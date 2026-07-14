using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Route.AgvActions;

/// <summary>
/// Drop action to be performed by an AGV.
/// </summary>
/// <param name="DropHeight">The height at which the AGV should drop an item. Can be null if no specific height is required.</param>
[PublicAPI]
public record DropAction(double? DropHeight) : AgvAction;