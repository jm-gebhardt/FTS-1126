using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Response to get the Cargos for multiple Agvs
/// </summary>
/// <param name="AgvCargosList"></param>
[PublicAPI]
public record GotCargosOfAgvs(IEnumerable<AgvCargos> AgvCargosList);

/// <summary>
/// Represents a list of cargos with the agv correlation id.
/// </summary>
public record AgvCargos
{
    /// <summary>
    /// Correlation id of the agv.
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// List of the agv cargos.
    /// </summary>
    public IEnumerable<Cargo> Cargos { get; } = new List<Cargo>();

    /// <inheritdoc cref="AgvCargos"/> />
    public AgvCargos(Guid correlationId, IEnumerable<Cargo> cargos)
    {
        CorrelationId = correlationId;
        Cargos = cargos;
    }

    /// <summary>
    /// Automapper ctor.
    /// </summary>
    [UsedImplicitly]
    public AgvCargos()
    {
    }
}