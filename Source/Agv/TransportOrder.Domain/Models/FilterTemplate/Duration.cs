using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;

/// <summary>
/// The order duration range.
/// </summary>
/// <param name="From">Minimum duration [s]</param>
/// <param name="To">Maximum duration [s]</param>
[PublicAPI]
public record Duration(float? From, float? To);