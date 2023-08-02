using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Extensions;

/// <summary>
/// Extension methods for <see cref="IAgv"/>
/// </summary>
public static class AgvExtensions
{
    /// <summary>
    /// Returns the <see cref="IAgv.AgvStatus"/>, throws if null
    /// </summary>
    /// <param name="agv"></param>
    /// <returns>The AgvStatus</returns>
    /// <exception cref="AgvStatusNotFoundException"></exception>
    public static AgvStatus GetRequiredAgvStatus(this IAgv agv)
    {
        var agvStatus = agv.AgvStatus;
        RequireAgvStatus(agv);
        return agvStatus!;
    }

    /// <summary>
    /// Get AgvStatus
    /// </summary>
    /// <param name="agv"></param>
    /// <exception cref="AgvStatusNotFoundException"></exception>
    private static void RequireAgvStatus(this IAgv agv)
    {
        var agvStatus = agv.AgvStatus;
        if (agvStatus == null)
        {
            throw new AgvStatusNotFoundException(agv);
        }
    }

    /// <summary>
    /// Get a bool if the LastNodeId of the AgvStatus does exist
    /// </summary>
    /// <param name="agv">The Agv</param>
    /// <param name="lastNodeId">The lastNodeId, only set if the return value was true</param>
    /// <returns>True if the lastNodeId exists</returns>
    [PublicAPI]    
    public static bool TryGetLastNodeId(this IAgv agv, out string lastNodeId)
    {
        string? statusLastNodeId = agv.AgvStatus?.LastNodeId;
        // if it is null, the return value was false and the caller will thus not use it
        lastNodeId = statusLastNodeId!;
        return !string.IsNullOrEmpty(statusLastNodeId);
    }
}