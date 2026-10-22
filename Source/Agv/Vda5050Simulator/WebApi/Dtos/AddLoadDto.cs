namespace Gebhardt.Acc.Vda5050Simulator.WebApi.Dtos;

/// <summary>
/// Provides a dto for adding a load to an agv.
/// </summary>
public record AddLoadDto
{
    /// <summary>
    /// Unique identification of the load (e.g., barcode or RFID).
    /// </summary>
    public string LoadId { get; init; } = string.Empty;

    /// <summary>
    /// Type of load.
    /// </summary>
    public string LoadType { get; init; } = string.Empty;

    /// <summary>
    /// Load handling position carrying the load. Optional for vehicles with only one load position.
    /// </summary>
    public string? LoadPosition { get; init; }
}
