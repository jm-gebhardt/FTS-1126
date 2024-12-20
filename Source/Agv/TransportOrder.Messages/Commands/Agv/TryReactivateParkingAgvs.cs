namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to try to reactivate parking agvs.
/// </summary>
/// <param name="NextActionTargetNode">The target node of the next action</param>
public record TryReactivateParkingAgvs(string? NextActionTargetNode=null);