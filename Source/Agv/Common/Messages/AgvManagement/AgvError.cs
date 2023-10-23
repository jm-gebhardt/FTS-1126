using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

/// <summary>
/// Error of an Agv
/// </summary>
public record AgvError : CorrelatedBy<Guid>
{
    /// <summary>
    /// CorrelationId of the AgvError
    /// </summary>
    public Guid CorrelationId { get; } = Guid.NewGuid();

    /// <summary>
    /// AgvCorrelationId
    /// </summary>
    public Guid AgvCorrelationId { get; init; }

    /// <summary>
    /// Verbose description of error
    /// </summary>
    public string? ErrorDescription { get; init; }

    /// <summary>
    /// warning: AGV is ready to drive without human intervention
    /// fatal: AGV is not in running condition
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
    public DateTime ErrorReceived { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Date or error resolved
    /// </summary>
    public DateTime? ErrorResolved { get; set; }

    /// <inheritdoc cref="AgvError"/>>
    /// <param name="agvCorrelationId"></param>
    /// <param name="errorDescription"></param>
    /// <param name="errorLevel"></param>
    /// <param name="errorType"></param>
    /// <param name="errorReferences"></param>
    /// <param name="errorReceived"></param>
    public AgvError(Guid agvCorrelationId, string? errorDescription, ErrorLevel errorLevel, string? errorType, List<ErrorReference>? errorReferences, DateTime errorReceived)
    {
        AgvCorrelationId = agvCorrelationId;
        ErrorDescription = errorDescription;
        ErrorLevel = errorLevel;
        ErrorType = errorType;
        ErrorReferences = errorReferences;
        ErrorReceived = errorReceived;
    }

    /// <summary>
    /// Empty constructor for serialization
    /// </summary>
    [UsedImplicitly]
    private AgvError()
    {
    }
}