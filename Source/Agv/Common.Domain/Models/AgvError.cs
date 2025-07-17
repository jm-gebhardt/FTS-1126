using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Error of an agv.
/// </summary>
public record AgvError : CorrelatedBy<Guid>
{
    private const string JoystickMode = "Joystick";
    private const string PushMode = "Schiebemodus";

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
    // ReSharper disable once TypeWithSuspiciousEqualityIsUsedInRecord.Global
    public List<ErrorReference>? ErrorReferences { get; init; }

    /// <summary>
    /// Date when error was received.
    /// </summary>
    public DateTime ErrorReceived { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Date when error was resolved.
    /// </summary>
    public DateTime? ErrorResolved { get; set; }

    /// <summary>
    /// The flag of the error.
    /// </summary>
    public ErrorFlag ErrorFlag { get; init; } = ErrorFlag.Agv;

    /// <inheritdoc cref="AgvError"/>>
    public AgvError(Guid agvCorrelationId, string?  errorTitle, string? errorDescription, string? errorCode, ErrorLevel errorLevel, string errorType, ErrorCategory errorCategory, List<ErrorReference>? errorReferences, DateTime errorReceived, ErrorFlag errorFlag)
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
        ErrorFlag = errorFlag;
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
    /// Compares two errors according to their type, category, flag, and level.
    /// </summary>
    /// <param name="error2">The error to compare to.</param>
    /// <returns>True if the error type, category, and level is the same.</returns>
    public bool IsSameError(AgvError error2)
    {
        return ErrorType == error2.ErrorType && ErrorLevel == error2.ErrorLevel && ErrorCategory == error2.ErrorCategory && error2.ErrorFlag == ErrorFlag;
    }

    /// <summary>
    /// True if the error is a field violation.
    /// </summary>
    /// <returns></returns>
    public bool IsFieldViolation()
    {
        var fieldViolation = new FieldViolation(CorrelationId, ErrorReceived);
        return ErrorType == fieldViolation.ErrorType 
               && ErrorLevel == fieldViolation.ErrorLevel 
               && ErrorCategory == fieldViolation.ErrorCategory 
               && fieldViolation.ErrorFlag == ErrorFlag 
               && fieldViolation.ErrorCode == ErrorCode 
               && fieldViolation.ErrorDescription == ErrorDescription 
               && fieldViolation.ErrorTitle == ErrorTitle;
    }

    /// <summary>
    /// Check if the error reports that an agv was pushed manually via joystick or in pushmode.
    /// </summary>
    /// <returns>True, if description reports a joystick or pushmode move of agv.</returns>
    public bool IsPushedManually()
    {
        return (ErrorDescription?.Contains(JoystickMode)?? false) || (ErrorDescription?.Contains(PushMode) ?? false);
    }

    /// <summary>
    /// True, if the error is a zone error.
    /// </summary>
    public bool IsZoneError()
    {
        var zoneError = new ZoneError(CorrelationId, ErrorReceived, "any");
        return ErrorType == zoneError.ErrorType &&
               ErrorCategory == zoneError.ErrorCategory &&
               ErrorFlag == zoneError.ErrorFlag &&
               ErrorLevel == zoneError.ErrorLevel;
    }

    /// <summary>
    /// True, if the error is a cargo mismatch error.
    /// </summary>
    public bool IsCargoMismatchError()
    {
        var cargoMismatchError = new CargoMismatchError(CorrelationId, ErrorReceived, false);
        return ErrorType == cargoMismatchError.ErrorType &&
               ErrorCategory == cargoMismatchError.ErrorCategory &&
               ErrorFlag == cargoMismatchError.ErrorFlag &&
               ErrorLevel == cargoMismatchError.ErrorLevel;
    }

    /// <summary>
    /// True, if the error is a queue blocking error.
    /// </summary>
    public bool IsQueueBlockingError()
    {
        var queueBlockingError = new QueueBlockingError(CorrelationId, ErrorReceived);
        return ErrorType == queueBlockingError.ErrorType &&
               ErrorCategory == queueBlockingError.ErrorCategory &&
               ErrorFlag == queueBlockingError.ErrorFlag &&
               ErrorLevel == queueBlockingError.ErrorLevel;
    }

    /// <summary>
    /// True, if the error is a ressource blocking error.
    /// </summary>
    public bool IsRessourceError()
    {
        var ressourceError = new RessourceBlockingError(CorrelationId, ErrorReceived, "any");
        return ErrorType == ressourceError.ErrorType &&
               ErrorCategory == ressourceError.ErrorCategory &&
               ErrorFlag == ressourceError.ErrorFlag &&
               ErrorLevel == ressourceError.ErrorLevel;
    }
}

/// <summary>
/// Provides a flag to distinguish agv and battery errors.
/// </summary>
public enum ErrorFlag
{
    /// <summary>
    /// Agv error.
    /// </summary>
    Agv,

    /// <summary>
    /// Battery error.
    /// </summary>
    Battery
}
