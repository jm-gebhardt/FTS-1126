using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Agv Error model
/// </summary>
public record AgvErrorStatusDto : DatedMessage
{
    /// <summary>The CorrelationId of the Agv</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>The list of errors. If there is no error it is empty or null.</summary>
    public List<AgvErrorDto> Errors { get; init; }

    /// <inheritdoc cref="AgvErrorStatusDto" />
    /// <param name="correlationId">The CorrelationId of the Agv</param>
    /// <param name="errors">The list of errors. If there is no error it is empty or null.</param>
    public AgvErrorStatusDto(Guid correlationId, List<AgvErrorDto> errors)
    {
        CorrelationId = correlationId;
        Errors = errors;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    [UsedImplicitly]
    private AgvErrorStatusDto()
    {
        Errors = null!;
    }
}