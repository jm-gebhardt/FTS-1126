using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Services.Filters;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Filters;

/// <summary>
/// Provides publish filters for action events.
/// </summary>
[UsedImplicitly]
public class ActionEventPublishFilter(
    ILogger<ActionEventPublishFilter> logger,
    UnTimedInMemoryFilter<ActionCompleted> actionCompletedFilter,
    UnTimedInMemoryFilter<ActionStarted> actionStartedFilter,
    UnTimedInMemoryFilter<ActionUpdated> actionUpdatedFilter,
    UnTimedInMemoryFilter<ActionCanceled> actionCanceledFilter) :
    IFilter<PublishContext<ActionCompleted>>,
    IFilter<PublishContext<ActionStarted>>,
    IFilter<PublishContext<ActionUpdated>>,
    IFilter<PublishContext<ActionCanceled>>
{
    /// <inheritdoc cref="IFilter{ActionCompleted}"/>>
    public async Task Send(PublishContext<ActionCompleted> context, IPipe<PublishContext<ActionCompleted>> next)
    {
        if (await actionCompletedFilter.ShouldActAsync(context.Message))
        {
            await next.Send(context);
            return;
        }
        logger.LogInformation("Not sending duplication of {Message}", nameof(ActionCompleted));
    }

    /// <inheritdoc cref="IFilter{ActionStarted}"/>>
    public async Task Send(PublishContext<ActionStarted> context, IPipe<PublishContext<ActionStarted>> next)
    {
        if (await actionStartedFilter.ShouldActAsync(context.Message))
        {
            await next.Send(context);
            return;
        }
        logger.LogInformation("Not sending duplication of {Message}", nameof(ActionStarted));
    }

    /// <inheritdoc cref="IFilter{ActionUpdated}"/>>
    public async Task Send(PublishContext<ActionUpdated> context, IPipe<PublishContext<ActionUpdated>> next)
    {
        if (await actionUpdatedFilter.ShouldActAsync(context.Message))
        {
            await next.Send(context);
            return;
        }
        logger.LogInformation("Not sending duplication of {Message}", nameof(ActionUpdated));
    }

    /// <inheritdoc cref="IFilter{ActionCanceled}"/>>
    public async Task Send(PublishContext<ActionCanceled> context, IPipe<PublishContext<ActionCanceled>> next)
    {
        if (await actionCanceledFilter.ShouldActAsync(context.Message))
        {
            await next.Send(context);
            return;
        }
        logger.LogInformation("Not sending duplication of {Message}", nameof(ActionCanceled));
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public void Probe(ProbeContext context)
    {
        context.CreateScope(nameof(ActionEventPublishFilter));
    }
}