using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

/// <summary>
/// Extension methods for <see cref="AgvStatus"/>
/// </summary>
public static class AgvStatusExtensions
{
    /// <summary>
    /// Returns the <see cref="AgvStatus.LastNodeId"/>, throws if null
    /// </summary>
    /// <param name="agvStatus">The AgvStatus</param>
    /// <returns>The LastNodeId of the AgvStatus</returns>
    /// <exception cref="AgvStatusNotFoundException"></exception>
    /// <exception cref="AgvLocationUnknownException"></exception>
    public static string GetRequiredLastNodeId(this AgvStatus agvStatus)
    {
        RequireLastNodeId(agvStatus);
        return agvStatus.LastNodeId!;
    }

    /// <summary>
    /// Throws if <see cref="AgvStatus.LastNodeId"/> is null
    /// </summary>
    /// <param name="agvStatus">The AgvStatus</param>
    /// <exception cref="AgvStatusNotFoundException"></exception>
    public static void RequireLastNodeId(this AgvStatus agvStatus)
    {
        string? lastNodeId = agvStatus.LastNodeId;
        if (lastNodeId == null)
        {
            throw new AgvLocationUnknownException(agvStatus.CorrelationId);
        }
    }
}