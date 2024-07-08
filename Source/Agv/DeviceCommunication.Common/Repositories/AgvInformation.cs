using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;

namespace Gebhardt.Acc.DeviceCommunication.Common.Repositories;

/// <summary>
/// This record contain all important states of the agv, such as status, agv action and agv graph 
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