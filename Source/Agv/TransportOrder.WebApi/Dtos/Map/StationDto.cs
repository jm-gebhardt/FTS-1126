using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;

/// <summary>
/// The station dto.
/// </summary>
[UsedImplicitly]
public record StationDto
{
    /// <summary>The Id of the node.</summary>
    public string NodeId { get; init; }

    /// <summary>Length of the station [m].</summary>
    public double Length { get; init; }

    /// <summary>Width of the station [m].</summary>
    public double Width { get; init; }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    public StationDto()
    {
        NodeId = null!;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nodeId">The Id of the node.</param>
    /// <param name="length">Length of the station [m].</param>
    /// <param name="width">Width of the station [m].</param>
    public StationDto(string nodeId, double length, double width)
    {
        NodeId = nodeId;
        Length = length;
        Width = width;
    }
}