using Gebhardt.Acc.TransportOrder.Domain.Models.Filter;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles owned filter related requests
/// </summary>
[UsedImplicitly]
public class OwnedFilterRequestHandler(IOwnedFilterRepository ownedFilterRepository) : 
    IConsumer<GetOwnedFilters>,
    IConsumer<AddOwnedFilter>,
    IConsumer<UpdateOwnedFilter>,
    IConsumer<DeleteOwnedFilter>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetOwnedFilters> context)
    {
        string owner = context.Message.Owner;

        ICollection<OwnedFilter> filters = await ownedFilterRepository.GetResourcesForOwner(owner);

        await context.RespondAsync(new GotOwnedFilters(filters));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AddOwnedFilter> context)
    {
        (string owner, var filter, string name) = context.Message;

        var ownedFilterCorrelationId = await ownedFilterRepository.AddOwnedResource(filter, owner, name);

        await context.RespondAsync(new AddedOwnedFilter(ownedFilterCorrelationId));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UpdateOwnedFilter> context)
    {
        (string owner, var filter, var ownedFilterCorrelationId, string name) = context.Message;

        bool result = await ownedFilterRepository.UpdateOwnedResource(filter, owner, ownedFilterCorrelationId, name);

        await context.RespondAsync(new OwnedFilterModificationResult(result));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DeleteOwnedFilter> context)
    {
        (string owner, var ownedFilterCorrelationId) = context.Message;

        bool result = await ownedFilterRepository.DeleteOwnedResource(owner, ownedFilterCorrelationId);

        await context.RespondAsync(new OwnedFilterModificationResult(result));
    }
}