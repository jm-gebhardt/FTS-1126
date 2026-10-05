using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.FilterTemplate;
using Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles filter template related requests
/// </summary>
[UsedImplicitly]
public class FilterTemplateRequestHandler(IOwnedFilterTemplateRepository filterTemplateRepository) :
    IConsumer<AddFilterTemplate>,
    IConsumer<UpdateFilterTemplate>,
    IConsumer<DeleteFilterTemplate>,
    IConsumer<GetFilterTemplates>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AddFilterTemplate> context)
    {
        (string owner, var filter, string name) = context.Message;

        if (name.Length > DbStringLimits.Description)
        {
            throw new StringLengthExceededException(name.Length, DbStringLimits.Description, nameof(name), nameof(FilterTemplate));
        }

        var ownedFilterCorrelationId = await filterTemplateRepository.AddOwnedResource(filter, owner, name);

        await context.RespondAsync(new AddedFilterTemplate(ownedFilterCorrelationId));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UpdateFilterTemplate> context)
    {
        (string owner, var filter, var ownedFilterCorrelationId, string name) = context.Message;

        if (name.Length > DbStringLimits.Description)
        {
            throw new StringLengthExceededException(name.Length, DbStringLimits.Description, nameof(name), nameof(FilterTemplate));
        }

        bool result = await filterTemplateRepository.UpdateOwnedResource(filter, owner, ownedFilterCorrelationId, name);

        await context.RespondAsync(new OwnedFilterModificationResult(result));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DeleteFilterTemplate> context)
    {
        var (ownedFilterCorrelationId, _) = context.Message;

        bool result = await filterTemplateRepository.DeleteResource(ownedFilterCorrelationId);

        await context.RespondAsync(new OwnedFilterModificationResult(result));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetFilterTemplates> context)
    {
        // Do not only get filter templates for this user, but all of them
        // Storing them as an owned property is only done to ease later migrations for systems with multiple users
        var filters = await filterTemplateRepository.GetAllResources();

        await context.RespondAsync(new GotFilterTemplates(filters));
    }
}