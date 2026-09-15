using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;
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
    public Task Consume(ConsumeContext<AddNewGraphSegment> context)
    {
        return repository.AddOrUpdateGraphAsync(context.Message.GraphSegment);
    }
}