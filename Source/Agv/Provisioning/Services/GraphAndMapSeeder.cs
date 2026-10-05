using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using Gebhardt.Acc.Provisioning.Configuration;
using Gebhardt.Acc.Provisioning.Models;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Map = Gebhardt.Acc.Common.Domain.Models.Graphs.Map;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
public class GraphAndMapSeeder : JsonSeeder<GraphAndMap>
{
    /// <inheritdoc />
    public GraphAndMapSeeder(IBus bus, IOptions<ConfigOptions> configOptions, ILogger<GraphAndMapSeeder> logger) : base(bus, configOptions.Value.BasePath, logger)
    {
    }

    /// <inheritdoc />
    protected override void Validate(GraphAndMap data)
    {
        EnsureNotNull(data.Graph, nameof(data.Graph));
        EnsureNotNull(data.Maps, nameof(data.Maps));

        if (data.Maps.Count == 0)
        {
            throw new InvalidOperationException("No maps defined.");
        }

        var notSetIds = data.Maps.Where(m => m.CorrelationId == Guid.Empty).ToList();
        if (notSetIds.Count != 0)
        {
            throw new InvalidOperationException($"CorrelationId of map(s): {string.Join(", ", notSetIds.Select(m => m.Name))} is not set.");
        }

        var notSetNames = data.Maps.Where(m => string.IsNullOrEmpty(m.Name)).ToList();
        if (notSetNames.Count != 0)
        {
            throw new InvalidOperationException($"Name of map(s): {string.Join(", ", notSetNames.Select(m => m.CorrelationId))} is not set.");
        }
    }

    /// <inheritdoc />
    protected override async Task<object> GetMessageAsync(GraphAndMap data)
    {
        var maps = await ReadBackgroundImagesAsync(data);

        var updateGraph = new UpdateGraphRequest(Guid.NewGuid(), data.Graph, maps);

        return updateGraph;
    }

    private async Task<List<Map>> ReadBackgroundImagesAsync(GraphAndMap graphAndMap)
    {
        var maps = new ConcurrentBag<Map>();
        var tasks = graphAndMap.Maps.Select(map => Task.Run(() =>
        {
            var generator = new MapFromFileGenerator(BasePath);

            maps.Add(generator.Generate(map.CorrelationId, map.Name, graphAndMap.Graph));
        }));

        await Task.WhenAll(tasks);

        return maps.ToList();
    }
}