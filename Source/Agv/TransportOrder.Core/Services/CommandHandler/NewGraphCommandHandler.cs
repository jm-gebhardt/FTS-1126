using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Handles new graph related commands.
/// </summary>
[UsedImplicitly]
public class NewGraphCommandHandler(INewGraphRepository repository) : IConsumer<AddNewGraph>
{
    /// <inheritdoc/>
    public Task Consume(ConsumeContext<AddNewGraph> context)
    {
        return repository.AddOrUpdateGraphAsync(Graph.FromCommon(context.Message.Graph));
    }
}