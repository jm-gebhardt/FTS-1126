using System.Collections.Concurrent;
using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

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