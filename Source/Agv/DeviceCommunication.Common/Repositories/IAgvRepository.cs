using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;

namespace Gebhardt.Acc.DeviceCommunication.Common.Repositories;

/// <summary>
/// Repository of agvs important states, such as status, agv action and agv graph, to retrieve or modify last agvStatus and agvMap of Agv
/// </summary>
public interface IAgvRepository
{
    /// <summary>
    /// Retrieve last state information of Agv for given correlationId
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the Agv</param>
    /// <returns>Null if there is no information yet for the given Agv</returns>
    public AgvInformation? GetAgvInformation(Guid correlationId);

    /// <summary>
    /// Updates the repository with agv status, agv graph and agv action
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="agvInformation"></param>
    public void AddOrUpdate(Guid correlationId, AgvInformation agvInformation);

    /// <summary>
    /// Updates the repository record's agv graph
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="agvGraph"></param>
    public void AddOrUpdateAgvGraph(Guid correlationId, AgvGraph agvGraph);

    /// <summary>
    /// Update the repository record's AgvStatusReceived
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="agvStatusReceived"></param>
    public void AddOrUpdateAgvStatus(Guid correlationId, AgvStatusReceived agvStatusReceived);

    /// <summary>
    /// Update the repository record's agv action
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="agvAction"></param>
    public void AddOrUpdateAgvAction(Guid correlationId, AgvAction agvAction);
}