using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Route.AgvActions;

/// <summary>
/// Pick action to be performed by an AGV.
/// </summary>
/// <param name="PickHeight">The height at which the AGV should pick an item. Can be null if no specific height is required.</param>
[PublicAPI]
public record PickAction(double? PickHeight) : AgvAction;