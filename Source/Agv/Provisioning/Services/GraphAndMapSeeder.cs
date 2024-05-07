using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
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
internal class GraphAndMapSeeder : JsonSeeder<GraphAndMap>
{
    /// <inheritdoc />
    public GraphAndMapSeeder(IBus bus, IOptions<ConfigOptions> configOptions, ILogger<GraphAndMapSeeder> logger) : base(bus, configOptions.Value.BasePath, logger)
    {
    }

    protected override void Validate(GraphAndMap data)
    {
        EnsureNotNull(data.Graph, nameof(data.Graph));
        EnsureNotNull(data.Maps, nameof(data.Maps));

        if (data.Maps.Count == 0)
        {
            throw new InvalidOperationException("No maps defined.");
        }

        List<Models.Map> notSetIds = data.Maps.Where(m => m.CorrelationId == default).ToList();
        if (notSetIds.Any())
        {
            throw new InvalidOperationException($"CorrelationId of map(s): {string.Join(", ", notSetIds.Select(m => m.Name))} is not set.");
        }

        List<Models.Map> notSetNames = data.Maps.Where(m => string.IsNullOrEmpty(m.Name)).ToList();
        if (notSetNames.Any())
        {
            throw new InvalidOperationException($"Name of map(s): {string.Join(", ", notSetNames.Select(m => m.CorrelationId))} is not set.");
        }
    }

    /// <inheritdoc />
    protected override async Task<object> GetMessageAsync(GraphAndMap data)
    {
        List<Map> maps = await ReadBackgroundImagesAsync(data);

        var graphChanged = new GraphChanged(Guid.NewGuid(), data.Graph, maps);

        return graphChanged;
    }

    private async Task<List<Map>> ReadBackgroundImagesAsync(GraphAndMap graphAndMap)
    {
        var maps = new ConcurrentBag<Map>();
        IEnumerable<Task> tasks = graphAndMap.Maps.Select(map => Task.Run(() =>
        {
            var generator = new MapFromFileGenerator(BasePath);

            maps.Add(generator.Generate(map.CorrelationId, map.Name, graphAndMap.Graph));
        }));

        await Task.WhenAll(tasks);

        return maps.ToList();
    }
}