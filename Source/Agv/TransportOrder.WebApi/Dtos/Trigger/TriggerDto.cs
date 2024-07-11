using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;

/// <summary>
/// A dto for triggers
/// </summary>
[PublicAPI]
public record TriggerDto : DatedMessage
{
    /// <inheritdoc cref="TriggerDto"/>>
    public TriggerDto()
    {
        Id = Guid.Empty;
        Name = string.Empty;
        Description = string.Empty;
        InstanceMaximum = null;
        ConfigurationParameters = [];
        Locations = [];
    }

    /// <summary>
    /// The id of the trigger.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The name of the trigger.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The description of the trigger functionality.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// If set, the maximum allowed number of usages in the graph 
    /// </summary>
    public int? InstanceMaximum { get; init; }

    /// <summary>
    /// The configuration parameters of the trigger.
    /// </summary>
    public ICollection<ConfigurationParameterDto> ConfigurationParameters { get; private set; }

    /// <summary>
    /// The locations of the trigger.
    /// </summary>
    public ICollection<TriggerLocationDto> Locations { get; private set; }
}