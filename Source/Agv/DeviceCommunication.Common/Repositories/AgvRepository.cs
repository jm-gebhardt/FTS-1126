using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;

namespace Gebhardt.Acc.DeviceCommunication.Common.Repositories;

/// <summary>
/// Agv repository in memory to retrieve or modify last agvStatus and agvMap of an agv.
/// </summary>
public class AgvRepository : IAgvRepository
{
    private readonly ConcurrentDictionary<Guid, AgvInformation> _agvInformationByAgvId = new();

    /// <inheritdoc />
    public AgvInformation? GetAgvInformation(Guid correlationId)
    {
        _agvInformationByAgvId.TryGetValue(correlationId, out var agvInformation);
        return agvInformation;
    }

    /// <inheritdoc />
    public void AddOrUpdate(Guid correlationId, AgvInformation agvInformation)
    {
        _agvInformationByAgvId.AddOrUpdate(correlationId, agvInformation, (_, _) => agvInformation);
    }

    /// <inheritdoc />
    public void AddOrUpdateAgvGraph(Guid correlationId, AgvGraph agvGraph)
    {
        var agvInformation = GetAgvInformation(correlationId) ?? new AgvInformation();

        agvInformation.AgvGraph = agvGraph;
        AddOrUpdate(correlationId, agvInformation);
    }

    /// <inheritdoc />
    public void AddOrUpdateAgvStatus(Guid correlationId, AgvStatusReceived agvStatusReceived)
    {
        var agvInformation = GetAgvInformation(correlationId) ?? new AgvInformation();

        agvInformation.AgvStatusReceived = agvStatusReceived;
        AddOrUpdate(correlationId, agvInformation);
    }

    /// <inheritdoc />
    public void AddOrUpdateAgvAction(Guid correlationId, AgvAction agvAction)
    {
        var agvInformation = GetAgvInformation(correlationId) ?? new AgvInformation();

        agvInformation.AgvAction = agvAction;
        AddOrUpdate(correlationId, agvInformation);
    }
}