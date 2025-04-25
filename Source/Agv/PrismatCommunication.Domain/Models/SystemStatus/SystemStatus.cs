using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Models.SystemStatus;

/// <summary>
/// Represents the system status.
/// </summary>
public class SystemStatus
{
    /// <summary>
    /// The id of the system.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// True, if the system is ready for transport orders from prismat.
    /// </summary>
    public bool IsReady { get; private set; }

    /// <summary>
    /// The error code, if the system is not ready.
    /// </summary>
    public int ErrorCode { get; private set; }

    /// <summary>
    /// Information on the error, if the system is not ready.
    /// </summary>
    public string ErrorInfo { get; private set; }

    /// <summary>
    /// True, if the agv limit at akl stations should be enforced.
    /// </summary>
    public bool UseAklAgvLimit { get; private set; } = true;

    /// <inheritdoc cref="SystemStatus"/>>
    public SystemStatus(Guid id, bool isReady, int errorCode, string errorInfo)
    {
        Id = id;
        IsReady = isReady;
        ErrorCode = errorCode;
        ErrorInfo = errorInfo;
    }

    /// <inheritdoc cref="SystemStatus"/>>
    public SystemStatus(bool isReady, int errorCode, string errorInfo)
    {
        Id = GetId();
        IsReady = isReady;
        ErrorCode = errorCode;
        ErrorInfo = errorInfo;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>The system id to be used.</returns>
    public static Guid GetId()
    {
        return "FTS001".ToGuid();
    }

    /// <summary>
    /// Set the status to ready.
    /// </summary>
    public void SetReady()
    {
        IsReady = true;
        ErrorInfo = string.Empty;
        ErrorCode = 0;
    }

    /// <summary>
    /// Set the status to not ready.
    /// </summary>
    public void SetNotReady(int errorCode, string errorInfo)
    {
        IsReady = false;
        ErrorInfo = errorInfo;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Decides, if the agv limit at akl stations should be used.
    /// </summary>
    /// <param name="useLimit">Flag indicating if the limit is used.</param>
    public void SetUseAklAgvLimit(bool useLimit)
    {
        UseAklAgvLimit = useLimit;
    }
}