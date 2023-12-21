using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

/// <summary>
/// This record contain all important states of the  the agv, such as status, agv action and agv graph 
/// </summary>
/// <value></value>
public record AgvInformation
{
    /// <summary>
    /// AGV Action
    /// </summary>
    public AgvAction? AgvAction { get; set; }

    /// <summary>
    /// AgvStatusReceived
    /// </summary>
    public AgvStatusReceived? AgvStatusReceived { get; set; }

    /// <summary>
    /// AGV Graph
    /// </summary>
    public AgvGraph? AgvGraph { get; set; }
}