using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Common.Utilities;

/// <summary>
/// Contains helper functions to generate errors and warnings.
/// </summary>
public static class ErrorGenerator
{
    /// <summary>
    /// Creates a random warning.
    /// </summary>
    /// <param name="correlationId">The agv correlation id to generate the warning for.</param>
    public static AgvError CreateWarning(Guid correlationId)
    {
        int warningPos = Random.Shared.Next(0, 5);
        return warningPos switch
        {
            0 => new AgvError(
                correlationId,
                "Distance sensor dirty",
                "Distance sensor reports dirt. Please clean the sensor.",
                "7120",
                ErrorLevel.Warning,
                "Distance sensor dirty",
                ErrorCategory.Dirt,
                null,
                DateTime.UtcNow,
                ErrorFlag.Agv,
                AgvErrorSource.Agv
            ),
            1 => new AgvError(
                correlationId,
                "Battery temperature",
                "Battery temperature is above 56\u00b0C",
                "8672",
                ErrorLevel.Warning,
                "Battery temperature",
                ErrorCategory.Temperature,
                null,
                DateTime.UtcNow,
                ErrorFlag.Battery,
                AgvErrorSource.Agv
            ),
            2 => new AgvError(
                correlationId,
                "Predictive Maintenance",
                "Operation hours exceeded the maintenance interval of 3000h. Please schedule maintenance.",
                "8232",
                ErrorLevel.Warning,
                "Predictive Maintenance",
                ErrorCategory.General,
                null,
                DateTime.UtcNow,
                ErrorFlag.Agv,
                AgvErrorSource.Agv
            ),
            3 => new FieldViolation(
                correlationId,
                DateTime.UtcNow
            ),
            _ => new AgvError(
                correlationId,
                "Motor temperature",
                "Motor[Front left] temperature is above 56\u00b0C. Inspection recommended.",
                "6522",
                ErrorLevel.Warning,
                "Motor temperature",
                ErrorCategory.Temperature,
                null,
                DateTime.UtcNow,
                ErrorFlag.Agv,
                AgvErrorSource.Agv
            )
        };
    }

    /// <summary>
    /// Creates a random error.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv to generate the error for.</param>
    public static AgvError CreateError(Guid correlationId)
    {
        int errorPos = Random.Shared.Next(0, 6);
        return errorPos switch
        {
            0 => new AgvError(
                correlationId,
                "Distance sensor offline",
                "No connection to distance sensor anymore.",
                "7120",
                ErrorLevel.Fatal,
                "Distance sensor offline",
                ErrorCategory.General,
                null,
                DateTime.UtcNow,
                ErrorFlag.Agv,
                AgvErrorSource.Agv
            ),
            1 => new AgvError(
                correlationId,
                "Camera offline",
                "No connection to the camera for detecting local obstacles anymore.",
                "8672",
                ErrorLevel.Fatal,
                "Camera offline",
                ErrorCategory.General,
                null,
                DateTime.UtcNow,
                ErrorFlag.Agv,
                AgvErrorSource.Agv
            ),
            2 => new AgvError(
                correlationId,
                "Emergency stop",
                "Collision detected. Emergency stop activated.",
                "9287",
                ErrorLevel.Fatal,
                "Emergency stop",
                ErrorCategory.General,
                null,
                DateTime.UtcNow,
                ErrorFlag.Agv,
                AgvErrorSource.Agv
            ),
            3 => new AgvError(
                correlationId,
                "Motor malfunction",
                "Motor[Front left] malfunction. Maintenance required.",
                "6522",
                ErrorLevel.Fatal,
                "Motor malfunction",
                ErrorCategory.General,
                null,
                DateTime.UtcNow,
                ErrorFlag.Agv,
                AgvErrorSource.Agv
            ),
            4 => new AgvError(
                correlationId,
                "Battery temperature limit exceeded",
                "Critical: Battery temperature exceeded safe operating limit.",
                "4242",
                ErrorLevel.Fatal,
                "Battery temperature",
                ErrorCategory.Temperature,
                null,
                DateTime.UtcNow,
                ErrorFlag.Battery,
                AgvErrorSource.Agv
            ),
            _ => new AgvError(
                correlationId,
                "Weight limit exceeded",
                "Weight limit for loads has been exceeded. Emergency stopped activated.",
                "4327",
                ErrorLevel.Fatal,
                "Weight limit exceeded",
                ErrorCategory.General,
                null,
                DateTime.UtcNow,
                ErrorFlag.Agv,
                AgvErrorSource.Agv
            )
        };
    }
}