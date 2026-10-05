using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

/// <inheritdoc cref="IBoundingBoxService"/>
public class BoundingBoxService : IBoundingBoxService
{
    private static CancellationToken CreateTimeoutToken() => new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

    private readonly IBoundingBoxCacheProvider _cache;
    private readonly IKeyedSemaphoreProvider<string, IBoundingBoxCalculationSemaphore> _calculationSemaphores;
    private readonly IAgvDb _agvDb;
    private readonly IReadOnlyList<IEdgePolygonProvider> _edgeProviders;
    private readonly IReadOnlyList<IMarginStrategy> _margins;
    private readonly ILogger<BoundingBoxService> _logger;

    /// <inheritdoc cref="BoundingBoxService"/>
    public BoundingBoxService(
        IBoundingBoxCacheProvider cache,
        IKeyedSemaphoreProvider<string, IBoundingBoxCalculationSemaphore> calculationSemaphores,
        IAgvDb agvDb,
        IEnumerable<IEdgePolygonProvider> edgeProviders,
        IEnumerable<IMarginStrategy> margins,
        ILogger<BoundingBoxService> logger)
    {
        _cache = cache;
        _calculationSemaphores = calculationSemaphores;
        _agvDb = agvDb;
        _edgeProviders = edgeProviders.ToList();
        _margins = margins.ToList();
        _logger = logger;
    }

    private async Task<GraphBoundingBoxCalculator> CreateCalculatorAsync()
    {
        var graph = await _agvDb.NewGraphs.GetGraphAsync();

        return new GraphBoundingBoxCalculator(graph, _edgeProviders);
    }

    private Task<IReadOnlyList<ResourceBoundingBox>> CalculateAndCacheAsync(string agvTypeIdentifier)
    {
        return _calculationSemaphores.Get(agvTypeIdentifier).ExecuteLockedAsync(async () =>
        {
            if (_cache.ConcurrentDict.TryGetValue(agvTypeIdentifier, out var cached))
            {
                return cached;
            }

            var agvTypes = await _agvDb.AgvTypes.GetAllAgvTypesAsync();
            var agvType = agvTypes.FirstOrDefault(t => t.AgvTypeIdentifier == agvTypeIdentifier);
            if (agvType is null)
            {
                _logger.LogWarning("Unknown AGV type \"{AgvType}\". Skipping bounding box calculation", agvTypeIdentifier);
                return [];
            }

            var calculator = await CreateCalculatorAsync();
            var boxes = calculator.CalculateAll(agvType, _margins);
            _cache.ConcurrentDict[agvTypeIdentifier] = boxes;

            _logger.LogInformation("Calculated {Count} bounding boxes for AGV type \"{AgvType}\"", boxes.Count, agvTypeIdentifier);
            return boxes;
        }, CreateTimeoutToken());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ResourceBoundingBox>> GetByAgvTypeAsync(string agvTypeIdentifier)
    {
        if (_cache.ConcurrentDict.TryGetValue(agvTypeIdentifier, out var cached))
        {
            return Task.FromResult(cached);
        }

        return CalculateAndCacheAsync(agvTypeIdentifier);
    }

    /// <inheritdoc />
    public async Task<ResourceBoundingBox?> GetAsync(Guid resourceId, string agvTypeIdentifier)
    {
        var boxes = await GetByAgvTypeAsync(agvTypeIdentifier);
        return boxes.FirstOrDefault(b => b.ResourceId == resourceId);
    }

    /// <inheritdoc />
    public async Task RecalculateAllAsync()
    {
        var sw = Stopwatch.StartNew();
        var agvTypes = await _agvDb.AgvTypes.GetAllAgvTypesAsync();
        var calculator = await CreateCalculatorAsync();

        await Task.WhenAll(agvTypes.Select(agvType => Task.Run(async () =>
            {
                var boxes = calculator.CalculateAll(agvType, _margins);

                await _calculationSemaphores.Get(agvType.AgvTypeIdentifier)
                    .ExecuteLockedAsync(() => _cache.ConcurrentDict[agvType.AgvTypeIdentifier] = boxes, CreateTimeoutToken());
            })
        ));

        var knownIdentifiers = agvTypes.Select(t => t.AgvTypeIdentifier).ToList();
        var staleIdentifiers = _cache.ConcurrentDict.Keys.Except(knownIdentifiers).ToList();
        foreach (string staleIdentifier in staleIdentifiers)
        {
            await _calculationSemaphores.Get(staleIdentifier)
                .ExecuteLockedAsync(() => _cache.ConcurrentDict.TryRemove(staleIdentifier, out _), CreateTimeoutToken());
        }

        _logger.LogInformation("Recalculated bounding boxes for {TypeCount} AGV type(s) in {ElapsedMs:F0} ms", agvTypes.Count, sw.ElapsedMilliseconds);
    }
}