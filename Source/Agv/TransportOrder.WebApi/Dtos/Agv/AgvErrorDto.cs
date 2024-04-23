using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

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
    /// The error title.
    /// </summary>
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
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Type of error
    /// </summary>
    public string? ErrorType { get; init; }

    /// <summary>
    /// The error category.
    /// </summary>
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

    /// <inheritdoc cref="AgvErrorDto" />
    public AgvErrorDto(Guid correlationId, string? errorTitle, string? errorDescription, string? errorCode, ErrorLevel errorLevel, string? errorType, ErrorCategory errorCategory, List<ErrorReference>? errorReferences, DateTime errorReceived)
    {
        CorrelationId = correlationId;
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
    public AgvErrorDto()
    {
    }
}