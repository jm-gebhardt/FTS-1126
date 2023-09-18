using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Defines a result for GetAgvErrors request
/// </summary>
/// <param name="AgvErrors"></param>
public record GetActiveAgvErrorsResult(List<AgvError> AgvErrors);

/// <summary>
/// Represents a
/// </summary>
public record AgvError
{
    /// <summary>
    /// CorrelationId of the AgvError
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Agv CorrelationId
    /// </summary>
    public Guid AgvCorrelationId { get; init; }

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
    public DateTime? ErrorResolved { get; set; }

    /// <param name="correlationId"></param>
    /// <param name="agvCorrelationId"></param>
    /// <param name="errorDescription"></param>
    /// <param name="errorLevel"></param>
    /// <param name="errorType"></param>
    /// <param name="errorReferences"></param>
    /// <param name="errorReceived"></param>
    public AgvError(Guid correlationId, Guid agvCorrelationId, string? errorDescription, ErrorLevel errorLevel, string? errorType, List<ErrorReference>? errorReferences, DateTime errorReceived)
    {
        CorrelationId = correlationId;
        AgvCorrelationId = agvCorrelationId;
        ErrorDescription = errorDescription;
        ErrorLevel = errorLevel;
        ErrorType = errorType;
        ErrorReferences = errorReferences;
        ErrorReceived = errorReceived;
    }

    /// <summary>
    /// Automapper ctor
    /// </summary>
    public AgvError()
    {

    }
}