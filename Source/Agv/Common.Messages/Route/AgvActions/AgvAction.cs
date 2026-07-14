using System.Text.Json.Serialization;

namespace Gebhardt.Acc.Common.Messages.Route.AgvActions;

/// <summary>
/// Abstract base for an action to be performed by an AGV.
/// </summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(PickAction), nameof(PickAction))]
[JsonDerivedType(typeof(DropAction), nameof(DropAction))]
[JsonDerivedType(typeof(ChargeAction), nameof(ChargeAction))]
public abstract record AgvAction;