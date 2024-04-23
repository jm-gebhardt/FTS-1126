using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Defines a result for GetAgvErrors request
/// </summary>
/// <param name="AgvErrors"></param>
public record GotActiveAgvErrors(List<AgvError> AgvErrors);

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
    /// The error title.
    /// </summary>
    [PublicAPI]
    public string? ErrorTitle { get; init; }

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
    /// The error code.
    /// </summary>
    [PublicAPI]
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Type of error
    /// </summary>
    public string? ErrorType { get; init; }

    /// <summary>
    /// The error category.
    /// </summary>
    [PublicAPI]
    public ErrorCategory ErrorCategory { get; init; }

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

    /// <inheritdoc cref="AgvError" />
    public AgvError(Guid correlationId, Guid agvCorrelationId, string? errorTitle, string? errorDescription, string? errorCode, ErrorLevel errorLevel, string? errorType, ErrorCategory errorCategory, List<ErrorReference>? errorReferences, DateTime errorReceived)
    {
        CorrelationId = correlationId;
        AgvCorrelationId = agvCorrelationId;
        ErrorTitle = errorTitle;
        ErrorDescription = errorDescription;
        ErrorCode = errorCode;
        ErrorLevel = errorLevel;
        ErrorType = errorType;
        ErrorCategory = errorCategory;
        ErrorReferences = errorReferences;
        ErrorReceived = errorReceived;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    public AgvError()
    {
    }
}