using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterResults;

/// <summary>
/// Data transfer object for a filtered agv
/// </summary>
[PublicAPI]
public record FilteredAgvDto
{
    /// <summary>
    /// 
    /// </summary>
    public string LastNodeId = "";

    /// <summary>
    /// 
    /// </summary>
    public bool IsDisturbed;

    /// <summary>
    /// 
    /// </summary>
    public bool IsAutomatic;

    /// <summary>
    /// 
    /// </summary>
    public bool IsCharging;

    /// <summary>
    /// 
    /// </summary>
    public ICollection<string> Zones = [];

    /// <summary>
    /// 
    /// </summary>
    public ICollection<string> Stations = [];

    /// <summary>
    /// Correlation id of the agv.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Serial number of the agv.
    /// </summary>
    public string SerialNumber { get; init; } = null!;

    /// <summary>
    /// The name of the agv.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// The mode of the agv.
    /// </summary>
    public AgvModeDto Mode { get; init; } = null!;

    /// <summary>
    /// 
    /// </summary>
    public FilteredAgvDto()
    {
    }
}