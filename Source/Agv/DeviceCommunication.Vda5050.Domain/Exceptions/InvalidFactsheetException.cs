using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Represents the exception raised when an invalid fact sheet has been uploaded.
/// </summary>
public class InvalidFactsheetException : FileUploadException
{
    /// <inheritdoc/>
    public InvalidFactsheetException(Guid agvCorrelationId, string traceBack) : base($"Invalid fact sheet for agv {agvCorrelationId}: {traceBack}"){}
}