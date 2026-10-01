using AutoMapper;
using Gebhardt.Acc.Common.Messages.NewGraph.Requests;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for graph requests.
/// </summary>
[UsedImplicitly]
public class GetNewGraphRequestHandler : IConsumer<GetGraph>
{
    private readonly INewGraphRepository _db;
    
    /// <summary>
    /// Initializes a new  <see cref="GetGraphRequestHandler"/> instance.
    /// </summary>
    public GetNewGraphRequestHandler(INewGraphRepository db, IMapper mapper)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetGraph> context)
    {
        var graph = await _db.GetGraphAsync();
        await context.RespondAsync(new GotGraph(graph));
    }
}