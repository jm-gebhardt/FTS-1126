using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Filter.Api.Swagger;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterResults;

/// <summary>
/// Data transfer object for a filtered agv
/// </summary>
[PublicAPI]
public record FilteredAgvDto
{
    /// <inheritdoc cref="AgvStatus.LastNodeId"/>
    public string LastNodeId { get; init; } = "";

    /// <inheritdoc cref="AgvStatus.Disturbed"/>
    public bool IsDisturbed { get; init; }

    /// <inheritdoc cref="AgvStatus.Automatic"/>
    public bool IsAutomatic { get; init; }

    /// <inheritdoc cref="AgvStatus.OperatingMode"/>
    public AgvOperatingMode OperatingMode { get; init; }

    /// <inheritdoc cref="AgvStatus.Charging"/>
    public bool IsCharging { get; init; }

    /// <inheritdoc cref="AgvStatus.ZoneIds"/>
    public ICollection<string> Zones { get; init; } = [];

    /// <inheritdoc cref="AgvStatus.StationIds"/>
    public ICollection<string> Stations { get; init; } = [];

    /// <inheritdoc cref="Domain.Models.Agv.Agv.CorrelationId"/>
    [SwaggerFilterableProperty]
    public Guid CorrelationId { get; init; }

    /// <inheritdoc cref="Domain.Models.Agv.Agv.SerialNumber"/>
    [SwaggerFilterableProperty]
    public string SerialNumber { get; init; } = null!;

    /// <inheritdoc cref="Domain.Models.Agv.Agv.Name"/>
    [SwaggerFilterableProperty]
    public string Name { get; private set; } = null!;

    /// <inheritdoc cref="Domain.Models.Agv.Agv.Mode"/>
    [SwaggerFilterableProperty]
    public AgvModeDto Mode { get; init; } = null!;
}