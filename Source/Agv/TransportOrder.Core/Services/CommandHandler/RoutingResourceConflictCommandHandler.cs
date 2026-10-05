using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;
using Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Detects the conflicts between the routing resources of the persisted graph and stores them.
/// </summary>
[UsedImplicitly]
public class RoutingResourceConflictCommandHandler : IConsumer<DetectGraphConflicts>
{
    private readonly IBoundingBoxService _boundingBoxService;
    private readonly IRoutingResourceConflictRepository _conflictRepository;
    private readonly IAgvDb _agvDb;
    private readonly ILogger<RoutingResourceConflictCommandHandler> _logger;

    /// <inheritdoc cref="RoutingResourceConflictCommandHandler"/>
    public RoutingResourceConflictCommandHandler(
        IBoundingBoxService boundingBoxService,
        IRoutingResourceConflictRepository conflictRepository,
        IAgvDb agvDb,
        ILogger<RoutingResourceConflictCommandHandler> logger)
    {
        _boundingBoxService = boundingBoxService;
        _conflictRepository = conflictRepository;
        _agvDb = agvDb;
        _logger = logger;
    }

    private async Task<IReadOnlyList<ResourceBoundingBox>> GetAllBoundingBoxesAsync()
    {
        var agvTypes = await _agvDb.AgvTypes.GetAllAgvTypesAsync();
        var boxes = new List<ResourceBoundingBox>();

        foreach (var agvType in agvTypes)
        {
            boxes.AddRange(await _boundingBoxService.GetByAgvTypeAsync(agvType.AgvTypeIdentifier));
        }

        return boxes;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<DetectGraphConflicts> context)
    {
        var sw = Stopwatch.StartNew();

        var graph = await _agvDb.NewGraphs.GetGraphAsync();
        var boxes = await GetAllBoundingBoxesAsync();

        var loadDuration = sw.Elapsed;
        sw.Restart();

        var detector = new BoundingBoxConflictDetector(boxes, graph);
        var conflicts = detector.Detect();

        var pairs = conflicts
            .Select(c => new ResourceConflict(c.ResourceA.ResourceId, c.ResourceB.ResourceId))
            .ToList();

        var detectionDuration = sw.Elapsed;
        sw.Restart();

        await _conflictRepository.SetConflictsAsync(pairs);
        var persistenceDuration = sw.Elapsed;

        _logger.LogInformation(
            "Stored {Count} routing resource conflict(s) from {BoxCount} bounding box(es). "
            + "Load took {LoadMs:F0} ms, detection {DetectionMs:F0} ms, persistence {PersistenceMs:F0} ms",
            pairs.Count,
            boxes.Count,
            loadDuration.TotalMilliseconds,
            detectionDuration.TotalMilliseconds,
            persistenceDuration.TotalMilliseconds);
    }
}