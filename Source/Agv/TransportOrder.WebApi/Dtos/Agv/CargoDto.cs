using Gebhardt.StoreWare.Agv.Common.Models;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Cargo loaded on an agv.
/// </summary>
public record CargoDto
{
    /// <summary>
    /// The correlationId of the agv.
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Id of loaded cargo.
    /// </summary>
    public string CargoId { get; }

    /// <summary>
    /// Type of loaded cargo.
    /// </summary>
    public string? CargoType { get; }

    /// <summary>
    /// This point describes the loads position on the AGV in the vehicle coordinates.
    /// The boundingBoxReference point is in the middle of the footprint of the load, so length/2 and width/2 [m].
    /// </summary>
    public Point3D? CenterPointPosition { get; }

    /// <summary>
    /// Dimensions of the cargo's bounding box [m].
    /// </summary>
    public Cuboid? Dimension { get; }

    /// <summary>
    /// Load position of cargo.
    /// </summary>
    public string? CargoPosition { get; }

    /// <summary>
    /// Weight of cargo [kg].
    /// </summary>
    public double? Weight { get; }

    /// <inheritdoc cref="CargoDto"/>
    /// <param name="correlationId">The CorrelationId of the Agv.</param>
    /// <param name="cargoId">Id of loaded cargo</param>
    /// <param name="cargoType">Type of load</param>
    /// <param name="centerPointPosition">This point describes the loads position on the AGV in the vehicle coordinates. The boundingBoxReference point is in the middle of the footprint of the load, so length/2 and width/2. [m]</param>
    /// <param name="dimension">Dimensions of the load's bounding box [m]</param>
    /// <param name="cargoPosition">Load position of cargo</param>
    /// <param name="weight">Weight of cargo [kg]</param>
    [PublicAPI]
    public CargoDto(Guid correlationId, string cargoId, string? cargoType, Point3D centerPointPosition, Cuboid dimension, string? cargoPosition, double? weight)
    {
        CorrelationId = correlationId;
        CargoId = cargoId;
        CargoType = cargoType;
        CenterPointPosition = centerPointPosition;
        Dimension = dimension;
        CargoPosition = cargoPosition;
        Weight = weight;
    }

    /// <summary>
    /// Empty constructor for serialization
    /// </summary>
    [UsedImplicitly]
    public CargoDto()
    {
        CargoId = null!;
    }
}