using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Cargos loaded on an agv.
/// </summary>
public record CargosDto
{
    /// <summary>
    /// Correlation id of the agv.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// List of the agv cargos.
    /// </summary>
    public IEnumerable<CargoDto> Cargos { get; init; } = new List<CargoDto>();

    /// <inheritdoc cref="CargosDto"/> />
    public CargosDto(Guid correlationId, IEnumerable<CargoDto> cargos)
    {
        CorrelationId = correlationId;
        Cargos = cargos;
    }

    /// <summary>
    /// Empty constructor for serialization
    /// </summary>
    [UsedImplicitly]
    public CargosDto()
    {
    }
}