using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Represents a resource that belongs to an owner.
/// </summary>
/// <typeparam name="T">The type of resource</typeparam>
[PublicAPI]
public interface IOwnedResource<T>
{
    /// <summary>
    /// The owner of the resource.
    /// </summary>
    string Owner { get; set; }

    /// <summary>
    /// The correlation id of the owned resource.
    /// </summary>
    Guid ResourceCorrelationId { get; set; }

    /// <summary>
    /// The resource.
    /// </summary>
    T Resource { get; set; }

    /// <summary>
    /// The name of the specific resource instance.
    /// </summary>
    string ResourceName { get; set; }
}