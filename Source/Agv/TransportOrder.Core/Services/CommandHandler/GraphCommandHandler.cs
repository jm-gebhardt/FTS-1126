using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Graphs;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Consumer for <see cref="UpdateGraph"/>.
/// </summary>
[UsedImplicitly]
public class GraphCommandHandler : IConsumer<UpdateGraph>
{
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<GraphCommandHandler> _logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="graphRepository"></param>
    /// <param name="logger"></param>
    public GraphCommandHandler(IGraphRepository graphRepository, ILogger<GraphCommandHandler> logger)
    {
        _graphRepository = graphRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UpdateGraph> context)
    {
        _logger.LogInformation("Update Graph");

        var graph = context.Message.Graph;
        graph.Validate();

        await _graphRepository.UpdateGraphAsync(graph);

        if (context.Message.Maps != null)
        {
            await context.Publish(new SaveMaps(context.Message.Maps));
        }
    }
}