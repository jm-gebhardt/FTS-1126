using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.NewGraph.Events;
using Gebhardt.Acc.Common.Messages.NewGraph.Requests;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;
using Gebhardt.Acc.TransportOrder.Messages.Events.Graph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Handles new graph related commands.
/// </summary>
[UsedImplicitly]
public class NewGraphCommandHandler(INewGraphRepository repository, ILogger<GraphCommandHandler> logger) : IConsumer<AddNewGraphSegment>, IConsumer<DeleteGraphSegment>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AddNewGraphSegment> context)
    {
        await repository.AddOrUpdateGraphSegmentAsync(context.Message.GraphSegment);
        await context.Publish(new GraphSegmentAdded(context.Message.GraphSegment));
        
        var graph = await repository.GetGraphAsync();
        await context.Publish(new GraphUpdated(context.Message.CorrelationId, graph));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DeleteGraphSegment> context)
    {
        bool isDeleted = await repository.DeleteGraphSegmentAsync(context.Message.GraphSegmentId);
        if (!isDeleted)
        {
            logger.LogError("Deleting graph segment {guid} failed", context.Message.GraphSegmentId);
            await context.RespondAsync(new Failed());
            return;
        }
        
        await context.RespondAsync(new Success());
    }
}