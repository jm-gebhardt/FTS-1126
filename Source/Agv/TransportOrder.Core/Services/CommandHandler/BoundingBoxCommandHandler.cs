using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;
using Gebhardt.Acc.TransportOrder.Messages.Commands.BoundingBox;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Calculates and stores bounding box polygons when triggered by graph or AGV fleet changes.
/// </summary>
[UsedImplicitly]
public class BoundingBoxCommandHandler : IConsumer<CalculateBoundingBoxes>, IConsumer<CalculateBoundingBoxesForAgvType>
{
    private readonly IBoundingBoxRepository _boundingBoxRepository;
    private readonly IAgvDb _agvDb;
    private readonly IReadOnlyList<IEdgePolygonProvider> _edgeProviders;
    private readonly IReadOnlyList<IMarginStrategy> _margins;
    private readonly ILogger<BoundingBoxCommandHandler> _logger;

    /// <inheritdoc cref="BoundingBoxCommandHandler"/>
    public BoundingBoxCommandHandler(
        IBoundingBoxRepository boundingBoxRepository,
        IAgvDb agvDb,
        IEnumerable<IEdgePolygonProvider> edgeProviders,
        IEnumerable<IMarginStrategy> margins,
        ILogger<BoundingBoxCommandHandler> logger)
    {
        _boundingBoxRepository = boundingBoxRepository;
        _agvDb = agvDb;
        _edgeProviders = edgeProviders.ToList();
        _margins = margins.ToList();
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<CalculateBoundingBoxes> context)
    {
        _logger.LogInformation("Recalculating bounding boxes for all AGV types");

        ICollection<Domain.Models.Agv.Agv> allAgvs = await _agvDb.Agvs.GetAllAgvsAsync();

        if (!allAgvs.Any())
        {
            _logger.LogInformation("No AGVs found. Skipping bounding box calculation.");
            return;
        }

        List<Guid> correlationIds = allAgvs.Select(a => a.CorrelationId).ToList();
        ICollection<AgvData> agvDatas = await _agvDb.Data.GetSelectedAgvDataAsync(correlationIds);

        List<AgvData> agvDataByType = agvDatas
            .GroupBy(d => d.Type)
            .Select(g => g.First())
            .ToList();

        var calculator = new GraphBoundingBoxCalculator(context.Message.Graph, _edgeProviders);
        var allResults = new List<ResourceBoundingBox>();

        foreach (var agvData in agvDataByType)
        {
            IReadOnlyList<ResourceBoundingBox> boxes = calculator.CalculateAll(agvData, _margins);
            allResults.AddRange(boxes);
        }

        await _boundingBoxRepository.SaveAllAsync(allResults);
        _logger.LogInformation("Saved {Count} bounding boxes for {TypeCount} AGV type(s)", allResults.Count, agvDataByType.Count);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<CalculateBoundingBoxesForAgvType> context)
    {
        var agvData = context.Message.AgvData;
        IReadOnlyCollection<string> existingTypes = await _boundingBoxRepository.GetAgvTypesAsync();
        if (existingTypes.Contains(agvData.Type))
        {
            return;
        }

        _logger.LogInformation("New AGV type \"{AgvType}\". Calculating bounding boxes", agvData.Type);

        var graph = await _agvDb.Graphs.GetGraphAsync();
        var calculator = new GraphBoundingBoxCalculator(graph, _edgeProviders);
        IReadOnlyList<ResourceBoundingBox> results = calculator.CalculateAll(agvData, _margins);

        await _boundingBoxRepository.AddForAgvTypeAsync(agvData.Type, results);
        _logger.LogInformation("Saved {Count} bounding boxes for AGV type \"{AgvType}\"", results.Count, agvData.Type);
    }
}