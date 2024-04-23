using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

/// <summary>
/// Cargo loaded on an Agv
/// </summary>
public record Cargo : CorrelatedBy<Guid>
{
    /// <summary>
    /// CorrelationId of the cargo
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Agv Correlation Id
    /// </summary>
    public Guid AgvCorrelationId { get; init; }

    /// <summary>
    /// Id of loaded cargo
    /// </summary>
    public string LoadId { get; init; }

    /// <summary>
    /// Type of load
    /// </summary>
    public string? LoadType { get; init; }

    /// <summary>
    /// This point describes the loads position on the AGV in the vehicle coordinates. The boundingBoxReference point is in the middle of the footprint of the load, so length/2 and width/2. [m]
    /// </summary>
    public Point3D? CenterPointPosition { get; init; }

    /// <summary>
    /// Dimensions of the load's bounding box [m]
    /// </summary>
    public Cuboid? Dimension { get; init; }

    /// <summary>
    /// Load position of cargo
    /// </summary>
    public string? LoadPosition { get; init; }

    /// <summary>
    /// Weight of cargo [kg]
    /// </summary>
    public double? Weight { get; init; }

    /// <inheritdoc cref="Cargo"/>
    /// <param name="correlationId">CorrelationId of the cargo</param>
    /// <param name="agvCorrelationId">The CorrelationId of the Agv</param>
    /// <param name="loadId">Id of loaded cargo</param>
    /// <param name="loadType">Type of load</param>
    /// <param name="centerPointPosition">This point describes the loads position on the AGV in the vehicle coordinates. The boundingBoxReference point is in the middle of the footprint of the load, so length/2 and width/2. [m]</param>
    /// <param name="dimension">Dimensions of the load's bounding box [m]</param>
    /// <param name="loadPosition">Load position of cargo</param>
    /// <param name="weight">Weight of cargo [kg]</param>
    public Cargo(Guid correlationId, Guid agvCorrelationId, string loadId, string? loadType, Point3D centerPointPosition, Cuboid dimension, string? loadPosition, double? weight)
    {
        CorrelationId = correlationId;
        AgvCorrelationId = agvCorrelationId;
        LoadId = loadId;
        LoadType = loadType;
        CenterPointPosition = centerPointPosition;
        Dimension = dimension;
        LoadPosition = loadPosition;
        Weight = weight;
    }

    /// <summary>
    /// Empty constructor for serialization
    /// </summary>
    [UsedImplicitly]
    public Cargo()
    {
        LoadId = null!;
    }
}