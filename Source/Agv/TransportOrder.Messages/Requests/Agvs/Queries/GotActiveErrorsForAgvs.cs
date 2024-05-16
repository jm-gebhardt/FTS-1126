using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Defines a result for the <see cref="GetActiveErrorsForAgvs"/> request.
/// </summary>
public record GotActiveErrorsForAgvs(List<AgvErrors> AgvsErrors);

/// <summary>
/// Represents a list of agv errors with the agv correlation id.
/// </summary>
public record AgvErrors
{
    /// <summary>
    /// Correlation id of the agv.
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// AgvErrors of the agv.
    /// </summary>
    public List<AgvError> AgvErrorList { get; } = new ();

    /// <inheritdoc cref="AgvErrors" />
    public AgvErrors(Guid correlationId, List<AgvError> agvErrors)
    {
        CorrelationId = correlationId;
        AgvErrorList = agvErrors;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    [UsedImplicitly]
    public AgvErrors()
    {
    }
}