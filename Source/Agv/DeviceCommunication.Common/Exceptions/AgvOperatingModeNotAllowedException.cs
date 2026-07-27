using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.DeviceCommunication.Common.Exceptions;

/// <summary>
/// The requested operation is not allowed while the AGV is in its current operating mode.
/// </summary>
public class AgvOperatingModeNotAllowedException : ValidationException
{
    /// <inheritdoc cref="AgvOperatingModeNotAllowedException"/>
    public AgvOperatingModeNotAllowedException(Guid agvCorrelationId, AgvOperatingMode currentMode)
        : base($"Operation not allowed while agv with CorrelationId {agvCorrelationId} is in {currentMode} mode.")
    {
    }

    /// <inheritdoc cref="AgvOperatingModeNotAllowedException"/>
    public AgvOperatingModeNotAllowedException(Guid agvCorrelationId, AgvOperatingMode currentMode, AgvOperatingMode requiredMode)
        : base($"Operation not allowed while agv with CorrelationId {agvCorrelationId} is in {currentMode} mode but needs to be in {requiredMode}.")
    {
    }
}