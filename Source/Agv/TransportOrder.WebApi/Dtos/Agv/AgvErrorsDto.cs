
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Agv error list model.
/// </summary>
public record AgvErrorsDto
{
    /// <summary>
    /// Correlation id of the agv.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// AgvErrors of the agv.
    /// </summary>
    public List<AgvErrorDto> AgvErrors { [UsedImplicitly]get; init; } = new ();

    /// <inheritdoc cref="AgvErrorsDto" />
    [UsedImplicitly]
    public AgvErrorsDto(Guid correlationId, List<AgvErrorDto> agvErrors)
    {
        CorrelationId = correlationId;
        AgvErrors = agvErrors;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    public AgvErrorsDto()
    {
    }
}