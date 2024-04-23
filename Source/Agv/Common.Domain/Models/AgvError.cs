using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Error of an agv.
/// </summary>
public record AgvError : CorrelatedBy<Guid>
{
    /// <summary>
    /// The correlation id of the agv error.
    /// </summary>
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The correlation id of the agv.
    /// </summary>
    public Guid AgvCorrelationId { get; init; }

    /// <summary>
    /// The error title.
    /// </summary>
    public string? ErrorTitle { get; init; }

    /// <summary>
    /// The error description.
    /// </summary>
    public string? ErrorDescription { get; init; }

    /// <summary>
    /// warning: AGV is ready to drive without human intervention
    /// fatal: Agv is not in running condition
    /// </summary>
    public ErrorLevel ErrorLevel { get; init; }

    /// <summary>
    /// The error code.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// The type of error.
    /// </summary>
    public string ErrorType { get; init; }

    /// <summary>
    /// The error category.
    /// </summary>
    public ErrorCategory ErrorCategory { get; init; } = ErrorCategory.General;

    /// <summary>
    ///  List of error references.
    /// </summary>
    public List<ErrorReference>? ErrorReferences { get; init; }

    /// <summary>
    /// Date when error was received.
    /// </summary>
    public DateTime ErrorReceived { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Date when error was resolved.
    /// </summary>
    public DateTime? ErrorResolved { get; set; }

    /// <inheritdoc cref="AgvError"/>>
    public AgvError(Guid agvCorrelationId, string?  errorTitle, string? errorDescription, string? errorCode, ErrorLevel errorLevel, string errorType, ErrorCategory errorCategory, List<ErrorReference>? errorReferences, DateTime errorReceived)
    {
        AgvCorrelationId = agvCorrelationId;
        ErrorTitle = errorTitle;
        ErrorDescription = errorDescription;
        ErrorCode = errorCode;
        ErrorLevel = errorLevel;
        ErrorType = errorType;
        ErrorCategory = errorCategory;
        ErrorReferences = errorReferences;
        if (errorReceived != default)
        {
            ErrorReceived = errorReceived;
        }
    }

    /// <summary>
    /// Empty constructor for serialization
    /// </summary>
    [UsedImplicitly]
    private AgvError()
    {
        ErrorType = null!;
    }

    /// <summary>
    /// Compares two errors according to their type, category, and level.
    /// </summary>
    /// <param name="error2">The error to compare to.</param>
    /// <returns>True if the error type, category, and level is the same.</returns>
    public bool IsSameError(AgvError error2)
    {
        return ErrorType == error2.ErrorType && ErrorLevel == error2.ErrorLevel && ErrorCategory == error2.ErrorCategory;
    }
}
