#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;

/// <summary>
/// The incident type.
/// </summary>
[PublicAPI]
public enum IncidentType
{
    Error,
    Warning,
    ManualInteraction
}