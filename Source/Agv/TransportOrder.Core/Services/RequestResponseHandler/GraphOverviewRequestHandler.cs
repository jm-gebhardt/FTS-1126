using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles requests for the global graph together with the load states of all AGV types.
/// </summary>
[UsedImplicitly]
public class GraphOverviewRequestHandler(INewGraphRepository graphRepository, IAgvDb agvDb) : IConsumer<GetGraphOverview>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetGraphOverview> context)
    {
        var graph = await graphRepository.GetGraphAsync();
        var agvTypes = await agvDb.AgvTypes.GetAllAgvTypesAsync(context.CancellationToken);
        var loadStates = agvTypes.SelectMany(t => t.LoadStates).ToList();

        await context.RespondAsync(new GotGraphOverview(graph, loadStates));
    }
}