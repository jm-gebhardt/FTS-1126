using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Route.AgvActions;

/// <summary>
/// Charge action to be performed by an AGV.
/// </summary>
[PublicAPI]
public record ChargeAction : AgvAction;