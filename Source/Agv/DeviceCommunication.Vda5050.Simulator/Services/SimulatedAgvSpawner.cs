using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.NewGraph.Requests;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services;

/// <inheritdoc/>
public class SimulatedAgvSpawner(ILogger<SimulatedAgvSpawner> logger, IBus bus) : ISimulatedAgvSpawner
{
    private const string FallbackMapId = "map";

    private static HashSet<string> SerialNumbersOf(SimulatedAgvTypeConfiguration? configuration)
    {
        return configuration?.SerialNumbers().ToHashSet() ?? [];
    }

    private static bool DiffersInTunables(SimulatedAgvTypeConfiguration? previous, SimulatedAgvTypeConfiguration? current)
    {
        if (previous is null || current is null)
        {
            return false;
        }

        return previous with { NoOfAgvs = current.NoOfAgvs } != current;
    }

    private async Task<IReadOnlyCollection<Node>?> TryGetExistingGraphNodesAsync(CancellationToken cancellationToken)
    {
        var getGraphClient = bus.CreateRequestClient<GetGraph>();
        try
        {
            var response = await getGraphClient.GetResponse<GotGraph>(new GetGraph(Guid.NewGuid()), cancellationToken);
            var nodes = response.Message.Graph.Nodes;
            if (nodes.Count == 0)
            {
                logger.LogInformation("Existing graph is empty; AGVs will wait for GraphUpdated to initialize position");
                return null;
            }
            logger.LogInformation("Retrieved existing graph with {NodeCount} nodes for AGV initialization", nodes.Count);
            return nodes;
        }
        catch (RequestException ex)
        {
            logger.LogWarning(ex, "Could not retrieve existing graph; AGVs will wait for GraphUpdated to initialize position");
            return null;
        }
    }

    private Task CreateAgvAsync(SimulatedAgvTypeConfiguration configuration,
        string serialNumber,
        string mapId,
        IReadOnlyCollection<Node>? graphNodes,
        CancellationToken cancellationToken)
    {
        return bus.Publish(new CreateAgv(configuration.ToAgvConfiguration(serialNumber, mapId), graphNodes), cancellationToken);
    }

    private Task UpdateAgvAsync(SimulatedAgvTypeConfiguration configuration,
        string serialNumber,
        string mapId,
        CancellationToken cancellationToken)
    {
        return bus.Publish(new UpdateAgvConfiguration(configuration.ToAgvConfiguration(serialNumber, mapId)), cancellationToken);
    }

    private Task RemoveAgvAsync(string serialNumber, CancellationToken cancellationToken)
    {
        return bus.Publish(new RemoveAgv(serialNumber.ToGuid()), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ApplyAsync(SimulatedAgvTypeConfiguration? currentConfiguration, SimulatedAgvTypeConfiguration? newConfiguration, CancellationToken cancellationToken = default)
    {
        var currentSerialNumbers = SerialNumbersOf(currentConfiguration);
        var newSerialNumbers = SerialNumbersOf(newConfiguration);

        string[] added = newSerialNumbers.Except(currentSerialNumbers).ToArray();
        string[] removed = currentSerialNumbers.Except(newSerialNumbers).ToArray();
        var updated = new List<string>();
        if (DiffersInTunables(currentConfiguration, newConfiguration))
        {
            updated.AddRange(newSerialNumbers.Intersect(currentSerialNumbers));
        }

        var tasks = new List<Task>();
        if (newConfiguration is not null && (added.Length > 0 || updated.Count > 0))
        {
            logger.LogInformation("Applying configuration of AGV type {AgvTypeIdentifier}: {Added} added, {Updated} updated",
                newConfiguration.AgvTypeIdentifier, added.Length, updated.Count);

            const string mapId = FallbackMapId;
            if (added.Length > 0)
            {
                var nodes = await TryGetExistingGraphNodesAsync(cancellationToken);
                tasks.Add(Task.WhenAll(added.Select(serialNumber => CreateAgvAsync(newConfiguration, serialNumber, mapId, nodes, cancellationToken))));
            }
            tasks.Add(Task.WhenAll(updated.Select(serialNumber => UpdateAgvAsync(newConfiguration, serialNumber, mapId, cancellationToken))));
        }

        if (removed.Length > 0)
        {
            logger.LogInformation("Removing {Removed} simulated AGVs of AGV type {AgvTypeIdentifier}", removed.Length, currentConfiguration?.AgvTypeIdentifier);
            tasks.Add(Task.WhenAll(removed.Select(serialNumber => RemoveAgvAsync(serialNumber, cancellationToken))));
        }
        await Task.WhenAll(tasks);
    }
}