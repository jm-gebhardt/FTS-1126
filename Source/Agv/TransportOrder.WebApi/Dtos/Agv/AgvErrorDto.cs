using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Agv Error model
/// </summary>
public record AgvErrorDto
{
    /// <summary>
    /// CorrelationId of the AgvError
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Verbose description of error.
    /// </summary>
    public string? ErrorDescription { get; init; }

    /// <summary>
    /// warning: AGV is ready to drive without human intervention
    /// fatal: AGV is not in running condition.
    /// </summary>
    public ErrorLevel ErrorLevel { get; init; }

    /// <summary>
    /// Type of error
    /// </summary>
    public string? ErrorType { get; init; }

    /// <summary>
    ///  List of above mentioned ErrorReferences
    /// </summary>
    public List<ErrorReference>? ErrorReferences { get; init; }

    /// <summary>
    /// Date of error received
    /// </summary>
    public DateTime ErrorReceived { get; init; }

    /// <summary>
    /// Date or error resolved
    /// </summary>
    public DateTime? ErrorResolved { get; init; }

    /// <inheritdoc cref="AgvErrorDto" />
    /// <param name="correlationId"></param>
    /// <param name="errorDescription"></param>
    /// <param name="errorLevel"></param>
    /// <param name="errorType"></param>
    /// <param name="errorReferences"></param>
    /// <param name="errorReceived"></param>
    public AgvErrorDto(Guid correlationId, string? errorDescription, ErrorLevel errorLevel, string? errorType, List<ErrorReference>? errorReferences, DateTime errorReceived)
    {
        CorrelationId = correlationId;
        ErrorDescription = errorDescription;
        ErrorLevel = errorLevel;
        ErrorType = errorType;
        ErrorReferences = errorReferences;
        ErrorReceived = errorReceived;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    public AgvErrorDto()
    {
    }
}