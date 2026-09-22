using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;
using Gebhardt.Acc.TransportOrder.Messages.Events.Graph;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Handles new graph related commands.
/// </summary>
[UsedImplicitly]
public class NewGraphCommandHandler(INewGraphRepository repository) : IConsumer<AddNewGraphSegment>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AddNewGraphSegment> context)
    {
        await repository.AddOrUpdateGraphAsync(context.Message.GraphSegment);
        await context.Publish(new NewGraphUpdated(context.Message.GraphSegment));
    }
}