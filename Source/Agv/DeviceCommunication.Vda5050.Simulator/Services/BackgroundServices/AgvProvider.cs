using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.BackgroundServices;

/// <summary>
/// Provides a service to generate simulated agvs.
/// </summary>
public class AgvProvider(Options options, ILogger<AgvProvider> logger, IBus bus) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ICollection<GraphNode>? graphNodes = await TryGetExistingGraphNodesAsync(stoppingToken);

        logger.LogInformation("Creating {Agvs} with base serial number {Base}", options.Agvs, options.SerialNumberBase);
        IEnumerable<Task> tasks = options.SimulatedSerialNumbers().Select(serialNumber => bus.Publish(
            new CreateAgv(new AgvConfiguration(serialNumber, options.MapId, options.Manufacturer), graphNodes),
            stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task<ICollection<GraphNode>?> TryGetExistingGraphNodesAsync(CancellationToken cancellationToken)
    {
        IRequestClient<GetGraph> getGraphClient = bus.CreateRequestClient<GetGraph>();
        try
        {
            Response<GotGraph> response = await getGraphClient.GetResponse<GotGraph>(new GetGraph(Guid.NewGuid()), cancellationToken);
            ICollection<GraphNode> nodes = response.Message.Graph.Nodes;
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
}